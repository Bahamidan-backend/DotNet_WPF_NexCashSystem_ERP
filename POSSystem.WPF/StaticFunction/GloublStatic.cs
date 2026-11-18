using POSSystem.WPF.Services;
using POSSystem.WPF.Services.Logging;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static POSSystem.WPF.Models.Category;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data;
using POSSystem.WPF.Shered.Proberty;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace POSSystem.WPF.StaticFunction
{
    public class GloublStatic
    {
        private static ICategoryService _categoryService;

        public GloublStatic(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public static event PropertyChangedEventHandler? PropertyChanged;

        protected static void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(new GloublStatic(_categoryService), new PropertyChangedEventArgs(propertyName));
        }

        public static string ResolveWorkingPrinterName(SettingsProberty? settings = null)
        {
            settings ??= SettingsService.LoadSettings();
            string configured = settings?.PrinterName?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(configured) && RawPrinterHelper.IsPrinterInstalled(configured))
            {
                return configured;
            }

            try
            {
                using var context = new POSDbContext();
                var kitchenPrinters = context.Kitchens
                    .AsNoTracking()
                    .Where(k => k.IsPrinterEnabled && !string.IsNullOrWhiteSpace(k.PrinterName))
                    .Select(k => k.PrinterName!.Trim())
                    .Distinct()
                    .ToList();

                foreach (var kp in kitchenPrinters)
                {
                    if (RawPrinterHelper.IsPrinterInstalled(kp))
                    {
                        return kp;
                    }
                }
            }
            catch { }

            string defaultWinPrinter = RawPrinterHelper.GetDefaultPrinterName();
            if (!string.IsNullOrWhiteSpace(defaultWinPrinter) && RawPrinterHelper.IsPrinterInstalled(defaultWinPrinter))
            {
                return defaultWinPrinter;
            }

            return configured;
        }

        private static bool RunOnStaThread(Func<bool> action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                return dispatcher.Invoke(action);
            }
            return action();
        }

        public static void PrintReturnReceipt(ReturnOrder returnOrder, Order originalOrder)
        {
            try
            {
                var settings = SettingsService.LoadSettings();
                if (!settings.PrintReceipt) return;

                string printerName = ResolveWorkingPrinterName(settings);
                if (string.IsNullOrWhiteSpace(printerName)) return;

                string extraCommands = "\x1B\x69";

                bool printed = RunOnStaThread(() =>
                {
                    var returnVisual = ReceiptTemplate.CreateReturnVisual(returnOrder, originalOrder, settings);
                    return RawPrinterHelper.PrintVisual(printerName, returnVisual, extraCommands);
                });

                if (!printed)
                {
                    ToastHelper.ShowWarning("تعذرت طباعة إيصال المرتجع. تأكد من الطابعة.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Return Printing Error: {ex.Message}", ex);
                ToastHelper.ShowWarning("حدث خطأ في نظام الطباعة أثناء طباعة المرتجع.");
            }
        }

        public static void PrintReceipt(Order order, CustomerDebt debt = null, bool? overridePrintCustomer = null, bool? overridePrintKitchen = null)
        {
            try
            {
                var purgedPrinters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var settings = SettingsService.LoadSettings();

                string printerName = ResolveWorkingPrinterName(settings);
                if (!string.IsNullOrWhiteSpace(printerName))
                {
                    RawPrinterHelper.PurgePrinterQueue(printerName);
                    purgedPrinters.Add(printerName);
                }

                string extraCommands = "\x1B\x69" + "\x1B\x70\x00\x19\xFA";
                bool customerFailed = false;

                // 1. Debt Payment Receipt (سداد دين)
                if (debt != null && debt.IsPaid)
                {
                    if (string.IsNullOrWhiteSpace(printerName))
                    {
                        ToastHelper.ShowWarning("يرجى تحديد اسم الطابعة في الإعدادات.");
                        return;
                    }
                    bool paymentSuccess = RunOnStaThread(() =>
                    {
                        FrameworkElement paymentVisual = ReceiptTemplate.CreateDebtPaymentVisual(debt, settings);
                        return RawPrinterHelper.PrintVisual(printerName, paymentVisual, extraCommands);
                    });
                    if (!paymentSuccess)
                    {
                        ToastHelper.ShowWarning("تعذرت طباعة إيصال السداد. يرجى التحقق من اسم الطابعة والتوصيلات.");
                    }
                    return;
                }

                bool printCustomer = overridePrintCustomer ?? settings.PrintCustomerReceipt;
                bool printKitchen = overridePrintKitchen ?? settings.PrintKitchenReceipt;
                bool printWaiterCopy = settings.PrintWaiterReceipt;

                // 2. Normal Customer Receipt (إذا كان خيار نسخة المباشر مفعلاً يطبع فاتورتين: مرجع الزبون + مرجع المباشر، وإذا أُلغي يطبع فاتورة الزبون فقط)
                if (printCustomer)
                {
                    if (string.IsNullOrWhiteSpace(printerName))
                    {
                        customerFailed = true;
                    }
                    else
                    {
                        try
                        {
                            bool ok = RunOnStaThread(() =>
                            {
                                string badge = printWaiterCopy ? "مرجع الزبون" : string.Empty;
                                FrameworkElement customerVisual = ReceiptTemplate.CreateVisual(
                                    order,
                                    settings,
                                    copyTitle: "فاتورة الزبون",
                                    referenceBadge: badge);
                                bool firstOk = RawPrinterHelper.PrintVisual(printerName, customerVisual, extraCommands);

                                if (printWaiterCopy)
                                {
                                    FrameworkElement waiterVisual = ReceiptTemplate.CreateVisual(
                                        order,
                                        settings,
                                        copyTitle: "فاتورة المباشر",
                                        referenceBadge: "مرجع المباشر");
                                    bool secondOk = RawPrinterHelper.PrintVisual(printerName, waiterVisual, "\x1B\x69");
                                    return firstOk && secondOk;
                                }

                                return firstOk;
                            });
                            if (!ok) customerFailed = true;
                        }
                        catch (Exception custEx)
                        {
                            AppLogger.LogError($"Customer Receipt Print Error: {custEx.Message}", custEx);
                            customerFailed = true;
                        }
                    }
                }

                // 3. Debt Creation Receipt (إيصال مديونية آجل)
                if (debt != null && !debt.IsPaid && !string.IsNullOrWhiteSpace(printerName))
                {
                    try
                    {
                        bool debtOk = RunOnStaThread(() =>
                        {
                            FrameworkElement debtVisual = ReceiptTemplate.CreateDebtVisual(order, debt, settings);
                            return RawPrinterHelper.PrintVisual(printerName, debtVisual, extraCommands);
                        });
                        if (!debtOk) customerFailed = true;
                    }
                    catch (Exception debtEx)
                    {
                        AppLogger.LogError($"Debt Receipt Print Error: {debtEx.Message}", debtEx);
                        customerFailed = true;
                    }
                }

                // 4. Kitchen Receipts (قصات المطابخ — مستقلة لكل مطبخ)
                if (printKitchen)
                {
                    try
                    {
                        PrintKitchenReceipts(order, purgedPrinters, overridePrintKitchen: true, fallbackPrinterName: printerName);
                    }
                    catch (Exception kitEx)
                    {
                        AppLogger.LogError($"Kitchen Receipts Print Error: {kitEx.Message}", kitEx);
                    }
                }

                if (customerFailed)
                {
                    ToastHelper.ShowWarning($"تعذرت طباعة فاتورة الزبون على الطابعة ({printerName}). تأكد من اسم الطابعة في الإعدادات.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Printing Error: {ex.Message}", ex);
                ToastHelper.ShowWarning("حدث خطأ في نظام الطباعة.");
            }
        }

        public static void PrintShiftEndReceipt(ShiftReportDto report, User user)
        {
            try
            {
                var settings = SettingsService.LoadSettings();
                string printerName = ResolveWorkingPrinterName(settings);
                if (string.IsNullOrWhiteSpace(printerName)) return;

                RawPrinterHelper.PurgePrinterQueue(printerName);

                StringBuilder receipt = new StringBuilder();
                receipt.AppendLine($"وقت الطباعة : {DateTime.Now:HH:mm:ss dd-MM-yyyy}");
                receipt.AppendLine($"المستخدم : {user.Email}");
                receipt.AppendLine("=================================");
                receipt.AppendLine("         أرصدة الخزنه الآن");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"الخزنة          | الرصيد الحالي");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"خزينة النقدي     | {report.CurrentCashInBox:N0}");
                string defaultBank = settings.DefaultBankOrWallet;
                string bankSuffix = !string.IsNullOrWhiteSpace(defaultBank) ? $" ({defaultBank})" : "";
                receipt.AppendLine($"خزينة إيداع/شبكة{bankSuffix} | {report.CardSales:N0}");
                receipt.AppendLine("=================================");
                receipt.AppendLine("        المبيعات لكل خزنه");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"الخزنة          | المبلغ المحصل");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"        مبيعات اليوم");
                receipt.AppendLine($"إجمالي المبيعات  | {report.TotalSales:N0}");
                receipt.AppendLine($"المحصل          | {report.CurrentCashInBox:N0}");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"        مرتجع اليوم");
                receipt.AppendLine($"قيمه المرتجع     | {report.ReturnedOrdersAmount:N0}");
                receipt.AppendLine($"المبلغ المدفوع   | {report.ReturnedOrdersAmount:N0}");
                receipt.AppendLine("---------------------------------");
                receipt.AppendLine($"مبيعات دين (عدد الأشخاص) : {report.CustomerDebtsCount}");
                if (!string.IsNullOrWhiteSpace(report.DifferenceStatusText))
                {
                    receipt.AppendLine("---------------------------------");
                    receipt.AppendLine("        مطابقة عهدة الوردية");
                    receipt.AppendLine("---------------------------------");
                    receipt.AppendLine($"النقدية الدفترية : {report.ExpectedEndingCash:N0} ريال");
                    receipt.AppendLine($"النقدية المسلمة  : {report.EndingCash:N0} ريال");
                    receipt.AppendLine($"حالة المطابقة   : {report.DifferenceStatusText}");
                }
                receipt.AppendLine($"إجمالي مبيعات الدين      : {report.TotalCustomerDebts:N0}");
                receipt.AppendLine("=================================");

                var bankDeposits = report.TransactionDetails?.Where(t => t.ExpenseType == "إيداع في البنك" || t.Type == "توريد بنكي").ToList();
                if (bankDeposits != null && bankDeposits.Any())
                {
                    receipt.AppendLine("        الإيداعات والتوريدات البنكية");
                    receipt.AppendLine("---------------------------------");
                    foreach (var bd in bankDeposits)
                    {
                        receipt.AppendLine($"{bd.Description}");
                        receipt.AppendLine($"المبلغ المودع : {bd.Amount:N0} ريال");
                        if (!string.IsNullOrWhiteSpace(bd.Notes))
                        {
                            receipt.AppendLine($"{bd.Notes}");
                        }
                        receipt.AppendLine("---------------------------------");
                    }
                    receipt.AppendLine("=================================");
                }

                receipt.AppendLine("\n\n\n");
                receipt.Append("\x1B\x69");

                bool ok = RunOnStaThread(() => RawPrinterHelper.Print(printerName, receipt.ToString()));
                if (!ok)
                {
                    ToastHelper.ShowWarning("تعذرت طباعة ملخص الوردية. تأكد من الطابعة.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Shift Printing Error: {ex.Message}", ex);
                ToastHelper.ShowWarning("حدث خطأ في نظام الطباعة أثناء طباعة الوردية.");
            }
        }

        public static void PrintKitchenReceipts(Order order, HashSet<string>? alreadyPurged = null, bool? overridePrintKitchen = null, string? fallbackPrinterName = null)
        {
            try
            {
                var settings = SettingsService.LoadSettings();
                bool printKitchen = overridePrintKitchen ?? settings.PrintKitchenReceipt;
                if (!printKitchen) return;

                string defaultPrinter = !string.IsNullOrWhiteSpace(fallbackPrinterName)
                    ? fallbackPrinterName
                    : ResolveWorkingPrinterName(settings);

                using var context = new POSDbContext();
                var kitchens = context.Kitchens.AsNoTracking().ToList();

                var productIds = order.OrderItems
                    .Where(i => i.ProductId.HasValue && (i.Product == null || i.Product.Category == null))
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                if (productIds.Count > 0)
                {
                    var dbProducts = context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Where(p => productIds.Contains(p.Id))
                        .ToDictionary(p => p.Id);

                    foreach (var item in order.OrderItems)
                    {
                        if (item.ProductId.HasValue && dbProducts.TryGetValue(item.ProductId.Value, out var dbProd))
                        {
                            item.Product ??= dbProd;
                            if (item.Product.Category == null && dbProd.Category != null)
                            {
                                item.Product.Category = dbProd.Category;
                            }
                        }
                    }
                }

                var targetedPrinters = kitchens
                    .Where(k => k.IsPrinterEnabled && !string.IsNullOrWhiteSpace(k.PrinterName))
                    .Select(k => k.PrinterName!.Trim())
                    .Distinct();

                foreach (var p in targetedPrinters)
                {
                    if ((alreadyPurged == null || !alreadyPurged.Contains(p)) && RawPrinterHelper.IsPrinterInstalled(p))
                    {
                        RawPrinterHelper.PurgePrinterQueue(p);
                        alreadyPurged?.Add(p);
                    }
                }

                foreach (var kitchen in kitchens)
                {
                    if (!kitchen.IsPrinterEnabled)
                        continue;

                    string kPrinter = !string.IsNullOrWhiteSpace(kitchen.PrinterName) && RawPrinterHelper.IsPrinterInstalled(kitchen.PrinterName)
                        ? kitchen.PrinterName.Trim()
                        : defaultPrinter;

                    if (string.IsNullOrWhiteSpace(kPrinter))
                        continue;

                    var kitchenItems = order.OrderItems.Where(item =>
                    {
                        if (item.Product != null && item.Product.KitchenId == kitchen.Id) return true;
                        if (item.Product != null && item.Product.Category != null && item.Product.Category.KitchenId == kitchen.Id) return true;
                        if (item.Addon != null && item.Addon.CategoryId.HasValue && item.Addon.Category != null && item.Addon.Category.KitchenId == kitchen.Id) return true;
                        return false;
                    }).ToList();

                    if (kitchenItems.Any())
                    {
                        PrintCategorySlip(order, kitchenItems, kitchen.Name, kPrinter);
                    }
                }

                var unassignedItems = order.OrderItems.Where(item =>
                {
                    int? pKid = item.Product?.KitchenId ?? item.Product?.Category?.KitchenId;
                    int? aKid = item.Addon?.Category?.KitchenId;
                    return !pKid.HasValue && !aKid.HasValue;
                }).ToList();

                if (unassignedItems.Any() && !string.IsNullOrWhiteSpace(defaultPrinter))
                {
                    PrintCategorySlip(order, unassignedItems, "عام / مطبخ رئيسي", defaultPrinter);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Kitchen Printing Error: {ex.Message}", ex);
            }
        }

        private static void PrintCategorySlip(Order order, List<OrderItem> items, string categoryName, string printerName)
        {
            if (items == null || items.Count == 0 || string.IsNullOrWhiteSpace(printerName)) return;

            RunOnStaThread(() =>
            {
                try
                {
                    var visual = ReceiptTemplate.CreateKitchenVisual(order, items, categoryName);
                    string extraCmds = "\n\x1B\x69";
                    return RawPrinterHelper.PrintVisual(printerName, visual, extraCmds);
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Kitchen slip print error: {ex.Message}", ex);
                    return false;
                }
            });
        }
    }
}
