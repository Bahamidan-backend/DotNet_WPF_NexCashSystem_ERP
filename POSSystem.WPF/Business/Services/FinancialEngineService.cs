using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Helpers;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.DTOs;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Services.Logging;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.Business.Services
{
    /// <summary>
    /// المحرك المالي والمحاسبي المتخصص في قطاع المطاعم (Domain Financial & P&L Service)
    /// يقوم بحساب الإيرادات، الخصومات، المرتجعات، تكلفة البضاعة المباعة (COGS)، المصروفات، والربح الصافي
    /// بدقة محاسبية 100% بمعزل تام عن طبقة العرض (UI / ViewModels).
    /// </summary>
    public class FinancialEngineService : IFinancialEngineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly IWarehouseService _warehouseService;
        private readonly IExpenseService _expenseService;
        private readonly IPurchaseService _purchaseService;
        private readonly IEmployeeService _employeeService;
        private readonly ICustomerDebtService _customerDebtService;

        public FinancialEngineService(
            IUnitOfWork unitOfWork,
            IOrderService orderService,
            IWarehouseService warehouseService,
            IExpenseService expenseService,
            IPurchaseService purchaseService,
            IEmployeeService employeeService,
            ICustomerDebtService customerDebtService)
        {
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _warehouseService = warehouseService;
            _expenseService = expenseService;
            _purchaseService = purchaseService;
            _employeeService = employeeService;
            _customerDebtService = customerDebtService;
        }

        public async Task<FinancialReportDto> GenerateFinancialStatementAsync(DateTime startDate, DateTime endDate)
        {
            var dto = new FinancialReportDto
            {
                StartDate = startDate,
                EndDate = endDate
            };

            try
            {
                AppLogger.LogInfo($"Generating financial statement for period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var context = _unitOfWork.GetContext();

                // 1. حساب المبيعات والإيرادات والخصومات مباشرة عبر استعلامات تجميع SQL
                var countableOrdersQuery = context.Orders.AsNoTracking()
                    .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate
                        && (o.Status == OrderStatus.Completed 
                            || o.Status == OrderStatus.Returned 
                            || o.Status == OrderStatus.PartiallyReturned));

                dto.SalesCount = await countableOrdersQuery.CountAsync();

                // تفصيل قنوات الدفع (إسقاط الأعمدة الضرورية فقط دون تحميل شجرة الكائنات)
                var ordersSummary = await countableOrdersQuery
                    .Select(o => new
                    {
                        o.Id,
                        o.SubTotal,
                        o.ServiceCharge,
                        o.TaxAmount,
                        o.Discount,
                        o.Total,
                        o.PaymentMethod,
                        o.Notes,
                        o.CreatedAt
                    })
                    .ToListAsync();

                // FIX ISSUE-FIN-003: TotalGrossSales represents true gross sales BEFORE discount (Total + Discount),
                // while NetSales = TotalGrossSales - TotalDiscounts - TotalReturns (= sum of net order totals minus returns).
                decimal totalOrderNetAfterDiscount = Math.Round(ordersSummary.Sum(o => o.Total), 2);
                dto.TotalDiscounts = Math.Round(ordersSummary.Sum(o => o.Discount), 2);
                dto.TotalGrossSales = Math.Round(totalOrderNetAfterDiscount + dto.TotalDiscounts, 2);

                dto.CashSales = ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total)
                    + ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CashAmount ?? 0);

                dto.ElectronicSales = ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.BankTransfer ||
                                                             o.PaymentMethod == PaymentMethod.CreditCard ||
                                                             o.PaymentMethod == PaymentMethod.DebitCard ||
                                                             o.PaymentMethod == PaymentMethod.MobilePayment).Sum(o => o.Total)
                    + ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CardAmount ?? 0);

                dto.DebtSales = ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => o.Total)
                    + ordersSummary.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.DebtAmount ?? 0);

                // 2. المرتجعات وصافي المبيعات
                dto.TotalReturns = Math.Round((decimal)(await context.ReturnOrders.AsNoTracking()
                    .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
                    .SumAsync(r => (double?)r.TotalAmount) ?? 0.0), 2);

                dto.NetSales = Math.Round(totalOrderNetAfterDiscount - dto.TotalReturns, 2);

                // 3. تحليل الصنف الأكثر مبيعاً وتكلفة البضاعة المباعة الحقيقية (COGS & Top Product)
                var orderItemsQuery = context.OrderItems.AsNoTracking()
                    .Where(oi => countableOrdersQuery.Select(o => o.Id).Contains(oi.OrderId));

                var orderItemsProjection = await orderItemsQuery
                    .Select(oi => new
                    {
                        oi.Id,
                        oi.ProductId,
                        oi.AddonId,
                        oi.Size,
                        oi.Quantity,
                        oi.UnitCost,
                        ProductName = oi.Product != null ? oi.Product.Name : (oi.Addon != null ? oi.Addon.Name : null),
                        IsProduct = oi.ProductId != null,
                        TypeWarehouse = oi.Product != null ? oi.Product.TypeWarehoseFromProduct : null,
                        BasePrice = oi.Product != null ? oi.Product.BasePrice : 0m,
                        AddonBuyingPrice = oi.Addon != null ? oi.Addon.BuyingPrice : 0m,
                        YieldRate = oi.Product != null && oi.Product.YieldRate > 0 ? oi.Product.YieldRate : 1
                    })
                    .ToListAsync();

                if (orderItemsProjection.Count > 0)
                {
                    var topProduct = orderItemsProjection
                        .Where(oi => !string.IsNullOrEmpty(oi.ProductName))
                        .GroupBy(oi => oi.ProductName!)
                        .OrderByDescending(g => g.Sum(oi => oi.Quantity > 0 ? oi.Quantity : 0))
                        .FirstOrDefault();

                    dto.TopSellingProduct = topProduct?.Key ?? "لا يوجد";
                }
                else
                {
                    dto.TopSellingProduct = "لا يوجد";
                }

                // 4. احتساب تكلفة البضاعة المباعة الفعلية من وصفات ومكونات المطبخ والمستودع (True Recipe & BOM COGS)
                var productIdsInSales = orderItemsProjection
                    .Where(x => x.ProductId.HasValue)
                    .Select(x => x.ProductId!.Value)
                    .Distinct()
                    .ToList();

                var recipesByProductId = productIdsInSales.Count > 0
                    ? (await context.Recipes.AsNoTracking()
                        .Where(r => productIdsInSales.Contains(r.ProductId))
                        .ToListAsync())
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => g.ToList())
                    : new Dictionary<int, List<Recipe>>();

                var orderItemIdsSubquery = context.OrderItems.AsNoTracking()
                    .Where(oi => countableOrdersQuery.Select(o => o.Id).Contains(oi.OrderId))
                    .Select(oi => oi.Id);

                var rawLinkedAddons = await context.addonAndorderItem.AsNoTracking()
                    .Where(aoi => orderItemIdsSubquery.Contains(aoi.OrderitemID))
                    .Select(aoi => new { aoi.OrderitemID, Cost = aoi.Addon != null ? aoi.Addon.BuyingPrice : 0m })
                    .ToListAsync();

                var linkedAddonsByOrderItem = rawLinkedAddons
                    .GroupBy(x => x.OrderitemID)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Cost));

                // جلب كميات الأصناف المرتجعة الصالحة لخصمها من تكلفة البضاعة المباعة (COGS Netting)
                var rawReturnItems = await context.ReturnOrderItems.AsNoTracking()
                    .Where(ri => countableOrdersQuery.Select(o => o.Id).Contains(ri.ReturnOrder.OriginalOrderId))
                    .Select(ri => new { ri.OriginalOrderItemId, ri.Quantity, ri.ReasonCode })
                    .ToListAsync();

                var returnedQtyByOrderItem = rawReturnItems
                    .Where(ri => string.IsNullOrWhiteSpace(ri.ReasonCode)
                              || (!ri.ReasonCode.Contains("[تالف]") && !ri.ReasonCode.Contains("[Waste]"))
                              || ri.ReasonCode.Contains("[مخزون]")
                              || ri.ReasonCode.Contains("[Restock]"))
                    .GroupBy(ri => ri.OriginalOrderItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                var allWarehouseProducts = await context.warehouseFromProducts.AsNoTracking().ToListAsync();
                var warehouseEntityById = allWarehouseProducts.ToDictionary(w => w.Id);

                var warehouseProductByName = allWarehouseProducts
                    .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                    .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var warehouseByName = allWarehouseProducts
                    .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                    .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Price, StringComparer.OrdinalIgnoreCase);

                var warehouseById = allWarehouseProducts
                    .ToDictionary(w => w.Id, w => w.Price);

                decimal totalCOGS = 0m;
                foreach (var item in orderItemsProjection)
                {
                    int returnedQty = returnedQtyByOrderItem.TryGetValue(item.Id, out var rQty) ? rQty : 0;
                    int netQuantity = Math.Max(0, item.Quantity - returnedQty);
                    if (netQuantity <= 0) continue;

                    decimal itemCOGS = 0m;

                    if (item.UnitCost > 0)
                    {
                        decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, netQuantity);
                        decimal factor = deduction > 0 ? deduction : netQuantity;
                        itemCOGS = factor * item.UnitCost;
                    }
                    else if (item.IsProduct && item.ProductId.HasValue)
                    {
                        if (recipesByProductId.TryGetValue(item.ProductId.Value, out var recipes) && recipes.Count > 0)
                        {
                            decimal unitRecipeCost = 0m;
                            foreach (var r in recipes)
                            {
                                if (r.Quantity <= 0) continue;

                                WarehouseFromProduct? whProd = null;
                                if (r.WarehouseFromProductId.HasValue && warehouseEntityById.TryGetValue(r.WarehouseFromProductId.Value, out var foundWh))
                                {
                                    whProd = foundWh;
                                }
                                else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseProductByName.TryGetValue(r.Name.Trim(), out var byName))
                                {
                                    whProd = byName;
                                }

                                if (whProd != null && whProd.Price > 0)
                                {
                                    decimal convertedQty = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                                    if (r.IsMainIngredient)
                                    {
                                        int yield = item.YieldRate > 0 ? item.YieldRate : 1;
                                        unitRecipeCost += (convertedQty * whProd.Price) / yield;
                                    }
                                    else
                                    {
                                        unitRecipeCost += convertedQty * whProd.Price;
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseByName.TryGetValue(r.Name.Trim(), out var fallbackPrice) && fallbackPrice > 0)
                                {
                                    if (r.IsMainIngredient)
                                    {
                                        int yield = item.YieldRate > 0 ? item.YieldRate : 1;
                                        unitRecipeCost += (r.Quantity * fallbackPrice) / yield;
                                    }
                                    else
                                    {
                                        unitRecipeCost += r.Quantity * fallbackPrice;
                                    }
                                }
                            }

                            if (unitRecipeCost > 0)
                            {
                                decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, netQuantity);
                                decimal factor = deduction > 0 ? deduction : netQuantity;
                                itemCOGS = factor * unitRecipeCost;
                            }
                        }

                        if (itemCOGS == 0)
                        {
                            decimal unitCost = 0m;
                            var typeWarehouse = item.TypeWarehouse;
                            if (!string.IsNullOrWhiteSpace(typeWarehouse))
                            {
                                var trimmedType = typeWarehouse.Trim();
                                if (int.TryParse(trimmedType, out int wpId) && warehouseById.TryGetValue(wpId, out var priceById) && priceById > 0)
                                    unitCost = priceById;
                                else if (warehouseByName.TryGetValue(trimmedType, out var priceByName) && priceByName > 0)
                                    unitCost = priceByName;
                            }

                            if (unitCost == 0 && !string.IsNullOrWhiteSpace(item.ProductName))
                            {
                                var trimmedName = item.ProductName.Trim();
                                if (warehouseByName.TryGetValue(trimmedName, out var priceByName) && priceByName > 0)
                                    unitCost = priceByName;
                            }

                            if (unitCost == 0 && item.BasePrice > 0)
                            {
                                unitCost = item.BasePrice;
                            }

                            itemCOGS = netQuantity * unitCost;
                        }
                    }
                    else if (item.AddonId.HasValue)
                    {
                        decimal addonCost = item.AddonBuyingPrice;
                        if (addonCost == 0 && !string.IsNullOrWhiteSpace(item.ProductName))
                        {
                            var trimmedName = item.ProductName.Trim();
                            if (warehouseByName.TryGetValue(trimmedName, out var priceByName) && priceByName > 0)
                                addonCost = priceByName;
                        }

                        itemCOGS = netQuantity * addonCost;
                    }

                    if (linkedAddonsByOrderItem.TryGetValue(item.Id, out var linkedAddonCost) && linkedAddonCost > 0)
                    {
                        itemCOGS += netQuantity * linkedAddonCost;
                    }

                    totalCOGS += itemCOGS;
                }

                dto.TotalCOGS = Math.Round(totalCOGS, 2);

                // 5. التوزيع اليومي للمبيعات والديون
                var debts = await context.CustomerDebts.AsNoTracking()
                    .Where(d => d.OrderId != null && countableOrdersQuery.Select(o => o.Id).Contains(d.OrderId.Value))
                    .Select(d => new { OrderId = d.OrderId!.Value, d.PaidAmount, d.RemainingAmount })
                    .ToListAsync();

                var debtDict = debts.GroupBy(d => d.OrderId).ToDictionary(g => g.Key, g => g.FirstOrDefault());

                dto.DailySalesBreakdown = ordersSummary
                    .GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new DailySalesItem
                    {
                        Date = g.Key,
                        TotalAmount = g.Sum(s => s.Total),
                        CashSales = g.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(s => s.Total)
                            + g.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CashAmount ?? 0),
                        ElectronicSales = g.Where(o => o.PaymentMethod == PaymentMethod.BankTransfer ||
                                                       o.PaymentMethod == PaymentMethod.CreditCard ||
                                                       o.PaymentMethod == PaymentMethod.DebitCard ||
                                                       o.PaymentMethod == PaymentMethod.MobilePayment).Sum(s => s.Total)
                            + g.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CardAmount ?? 0),
                        DebtSales = g.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(s => s.Total)
                            + g.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.DebtAmount ?? 0),
                        PaidDebtSales = g.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => debtDict.TryGetValue(o.Id, out var dt) && dt != null ? dt.PaidAmount : 0),
                        UnpaidDebtSales = g.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => debtDict.TryGetValue(o.Id, out var dt) && dt != null ? dt.RemainingAmount : o.Total)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // 6. ديون العملاء
                var customerDebtsQuery = context.CustomerDebts.AsNoTracking()
                    .Where(d => d.DebtDate >= startDate && d.DebtDate <= endDate);

                var allDebts = await customerDebtsQuery
                    .Select(d => new
                    {
                        d.CustomerName,
                        d.Amount,
                        d.PaidAmount,
                        d.RemainingAmount
                    })
                    .ToListAsync();

                dto.TotalCustomerDebts = allDebts.Where(d => d.Amount >= 0).Sum(d => d.Amount);
                dto.CustomerDebtCount = allDebts.Count;
                dto.TotalPaidDebts = allDebts.Sum(d => d.PaidAmount);
                dto.TotalUnpaidDebts = allDebts.Sum(d => d.RemainingAmount);

                dto.CustomersDetails = allDebts
                    .Where(d => !string.IsNullOrEmpty(d.CustomerName))
                    .GroupBy(d => d.CustomerName!)
                    .Select(group => new CustomerReportItem
                    {
                        CustomerName = group.Key,
                        TotalDebt = group.Sum(d => d.Amount),
                        PaidAmount = group.Sum(d => d.PaidAmount),
                        RemainingAmount = group.Sum(d => d.RemainingAmount)
                    })
                    .ToList();

                dto.RegularCustomerCount = dto.CustomersDetails.Count;

                // 7. المصروفات التشغيلية وتفصيلها حسب الفئة وطريقة الدفع / المحفظة
                var periodExpensesList = await context.Expenses.AsNoTracking()
                    .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate && e.Amount >= 0 &&
                                (e.Notes == null || !e.Notes.Contains("[راتب:")))
                    .OrderByDescending(e => e.ExpenseDate)
                    .ToListAsync();

                dto.TotalExpenses = Math.Round(periodExpensesList.Sum(e => e.Amount), 2);
                dto.CashExpensesTotal = Math.Round(periodExpensesList.Where(e => e.PaymentMethod == PaymentMethod.Cash).Sum(e => e.Amount), 2);
                dto.WalletExpensesTotal = Math.Round(periodExpensesList.Where(e => e.PaymentMethod != PaymentMethod.Cash).Sum(e => e.Amount), 2);
                dto.ExpensesDetails = periodExpensesList.Select(e => new ExpenseReportItem
                {
                    Name = e.Name ?? "مصروف",
                    Category = string.IsNullOrWhiteSpace(e.Category) ? "أخرى" : e.Category,
                    PaymentMethodText = e.PaymentMethodText,
                    Amount = e.Amount,
                    ExpenseDate = e.ExpenseDate,
                    Notes = e.Notes ?? ""
                }).ToList();

                // 7.1. احتساب خسائر عجز وهدر وتلف المخزون (Stock Shrinkage, Waste & Inventory Variances)
                var stockWasteMovements = await context.StockMovements.AsNoTracking()
                    .Where(sm => sm.MovementDate >= startDate && sm.MovementDate <= endDate
                              && (sm.MovementType == StockMovementType.Waste
                               || (sm.MovementType == StockMovementType.Adjustment && sm.Quantity < 0)))
                    .Select(sm => new
                    {
                        sm.Quantity,
                        sm.UnitCost,
                        sm.TotalCost
                    })
                    .ToListAsync();

                decimal totalShortageCost = stockWasteMovements.Sum(sm =>
                    sm.TotalCost > 0 ? sm.TotalCost : Math.Abs(sm.Quantity) * sm.UnitCost);

                var stockSurplusMovements = await context.StockMovements.AsNoTracking()
                    .Where(sm => sm.MovementDate >= startDate && sm.MovementDate <= endDate
                              && sm.MovementType == StockMovementType.Adjustment
                              && sm.Quantity > 0)
                    .Select(sm => new
                    {
                        sm.Quantity,
                        sm.UnitCost,
                        sm.TotalCost
                    })
                    .ToListAsync();

                decimal totalSurplusGain = stockSurplusMovements.Sum(sm =>
                    sm.TotalCost > 0 ? sm.TotalCost : sm.Quantity * sm.UnitCost);

                var cookingBurnWastes = await context.StockWastes.AsNoTracking()
                    .Where(sw => sw.WasteDate >= startDate && sw.WasteDate <= endDate && (sw.Reason == WasteReason.CookingBurn || sw.Reason == WasteReason.StaffMeal || sw.Reason == WasteReason.Charity || (sw.Notes != null && sw.Notes.Contains("[صرف متبقي الكاشير:"))))
                    .Select(sw => sw.TotalCost)
                    .ToListAsync();
                decimal cookingBurnTotal = cookingBurnWastes.Sum();

                dto.TotalStockWasteAndShortage = Math.Round(Math.Max(0m, totalShortageCost + cookingBurnTotal - totalSurplusGain), 2);

                // 8. المشتريات والمرتجعات
                dto.TotalPurchases = await _purchaseService.GetTotalByDateRangeAsync(startDate, endDate);
                dto.TotalPurchaseReturns = (decimal)(await context.PurchaseReturns.AsNoTracking()
                    .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
                    .SumAsync(r => (double?)r.TotalAmount) ?? 0.0);

                // 9. الرواتب والموظفين - حساب دقيق متوافق مع الفترة الزمنية للتقرير (FIX ISSUE-FIN-001 & ISSUE-FIN-002)
                var activeEmployeesList = await context.Users.AsNoTracking()
                    .Where(u => u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        Name = u.Name ?? "غير محدد",
                        Role = u.Role.ToString(),
                        BaseSalary = u.BaseSalary ?? 0m,
                        HireDate = u.HireDate ?? u.CreatedAt
                    })
                    .ToListAsync();

                dto.TotalEmployees = activeEmployeesList.Count;
                dto.MonthlyBaseSalaries = Math.Round(activeEmployeesList.Where(u => u.BaseSalary > 0).Sum(u => u.BaseSalary), 2);

                var recordedSalariesInPeriod = await context.EmployeeSalaries.AsNoTracking()
                    .Where(s => (s.IsPaid && s.PaymentDate >= startDate && s.PaymentDate <= endDate) ||
                                (s.SalaryDate >= startDate && s.SalaryDate <= endDate))
                    .Select(s => new { s.EmployeeId, s.BaseSalary, s.AdvanceDeduction, s.SalaryDate, s.IsPaid })
                    .ToListAsync();

                double periodDays = Math.Max(1.0, (endDate.Date - startDate.Date).TotalDays + 1.0);

                if (periodDays < 27.0)
                {
                    // التقرير اليومي أو الأسبوعي: يحمّل فقط الرواتب المسجلة/المصروفة فعلياً ضمن هذه الأيام دون تحميل راتب الشهر كاملاً على يوم واحد
                    dto.TotalSalaries = Math.Round(recordedSalariesInPeriod.Sum(s => s.BaseSalary), 2);
                }
                else if (periodDays <= 32.0)
                {
                    // التقرير الشهري: يحمّل الراتب الشهري المستحق لكل موظف نشط (مع مراعاة تاريخ التعيين أو المسير المسجل)
                    decimal monthlyPayrollTotal = 0m;
                    int reportYear = endDate.Year;
                    int reportMonth = endDate.Month;
                    int daysInMonth = DateTime.DaysInMonth(reportYear, reportMonth);

                    foreach (var emp in activeEmployeesList.Where(e => e.BaseSalary > 0))
                    {
                        var recordedForEmp = recordedSalariesInPeriod.Where(s => s.EmployeeId == emp.Id).ToList();
                        if (recordedForEmp.Count > 0)
                        {
                            monthlyPayrollTotal += recordedForEmp.Sum(s => s.BaseSalary);
                        }
                        else if (emp.HireDate.Date <= endDate.Date)
                        {
                            if (emp.HireDate.Year == reportYear && emp.HireDate.Month == reportMonth && emp.HireDate.Day > 1)
                            {
                                int workedDays = daysInMonth - emp.HireDate.Day + 1;
                                monthlyPayrollTotal += Math.Round(emp.BaseSalary * ((decimal)workedDays / daysInMonth), 2);
                            }
                            else
                            {
                                monthlyPayrollTotal += emp.BaseSalary;
                            }
                        }
                    }

                    dto.TotalSalaries = Math.Round(monthlyPayrollTotal, 2);
                }
                else
                {
                    // التقرير الربع سنوي أو السنوي (أكثر من شهر): يجمع الرواتب المستحقة لكل شهر عمل ضمن الفترة
                    decimal multiMonthTotal = 0m;
                    var cursor = new DateTime(startDate.Year, startDate.Month, 1);
                    var lastMonth = new DateTime(endDate.Year, endDate.Month, 1);
                    var currentMonthCap = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    if (lastMonth > currentMonthCap) lastMonth = currentMonthCap;

                    while (cursor <= lastMonth)
                    {
                        int yr = cursor.Year;
                        int mo = cursor.Month;
                        int dim = DateTime.DaysInMonth(yr, mo);
                        var monthEnd = new DateTime(yr, mo, dim);

                        foreach (var emp in activeEmployeesList.Where(e => e.BaseSalary > 0))
                        {
                            var recInMonth = recordedSalariesInPeriod
                                .Where(s => s.EmployeeId == emp.Id && s.SalaryDate.Year == yr && s.SalaryDate.Month == mo)
                                .ToList();

                            if (recInMonth.Count > 0)
                            {
                                multiMonthTotal += recInMonth.Sum(s => s.BaseSalary);
                            }
                            else if (emp.HireDate.Date <= monthEnd)
                            {
                                if (emp.HireDate.Year == yr && emp.HireDate.Month == mo && emp.HireDate.Day > 1)
                                {
                                    int workedDays = dim - emp.HireDate.Day + 1;
                                    multiMonthTotal += Math.Round(emp.BaseSalary * ((decimal)workedDays / dim), 2);
                                }
                                else
                                {
                                    multiMonthTotal += emp.BaseSalary;
                                }
                            }
                        }

                        cursor = cursor.AddMonths(1);
                    }

                    dto.TotalSalaries = Math.Round(multiMonthTotal, 2);
                }

                dto.AdvanceDeductions = Math.Round(recordedSalariesInPeriod.Sum(s => s.AdvanceDeduction), 2);

                var unpaidAdvances = await context.EmployeeAdvances.AsNoTracking()
                    .Where(a => !a.IsRepaid && (a.Amount - a.PaidAmount) >= 0)
                    .Select(a => new { a.EmployeeId, Unpaid = a.Amount - a.PaidAmount })
                    .ToListAsync();
                dto.TotalAdvances = unpaidAdvances.Sum(a => a.Unpaid);

                var advancesByEmployee = unpaidAdvances
                    .GroupBy(a => a.EmployeeId)
                    .ToDictionary(g => g.Key, g => g.Sum(a => a.Unpaid));

                dto.EmployeesDetails = activeEmployeesList.Select(emp => new EmployeeReportItem
                {
                    EmployeeName = emp.Name,
                    Role = emp.Role,
                    BaseSalary = emp.BaseSalary,
                    TotalAdvances = advancesByEmployee.TryGetValue(emp.Id, out var adv) ? adv : 0m
                }).ToList();

                AppLogger.LogInfo($"Financial statement generated: GrossSales={dto.TotalGrossSales:N0}, Discounts={dto.TotalDiscounts:N0}, NetSales={dto.NetSales:N0}, COGS={dto.TotalCOGS:N0}, Expenses={dto.TotalExpenses:N0}, Salaries={dto.TotalSalaries:N0}, NetProfit={dto.NetProfit:N0}");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error generating financial statement in domain service", ex);
                throw;
            }

            return dto;
        }
    }
}
