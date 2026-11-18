using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Services
{
    public class PermissionDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public static class SystemPermissions
    {
        // 0. لوحة التحكم
        public const string DashboardView = "Dashboard.View";

        // 1. إدارة المنتجات والأصناف
        public const string ProductsView = "Products.View";
        public const string ProductsAdd = "Products.Add";
        public const string ProductsEdit = "Products.Edit";
        public const string ProductsDelete = "Products.Delete";
        public const string ProductsChangePrice = "Products.ChangePrice";
        public const string RecipesManage = "Products.RecipesManage";
        public const string AddonsManage = "Products.AddonsManage";
        public const string DiscountsManage = "Products.DiscountsManage";

        // 2. إدارة المخزون والمستودعات
        public const string InventoryView = "Inventory.View";
        public const string InventoryAddMaterial = "Inventory.AddMaterial";
        public const string InventoryEditQty = "Inventory.EditQty";
        public const string InventoryTransfer = "Inventory.Transfer";
        public const string InventoryWaste = "Inventory.Waste";
        public const string AnimalBatchesManage = "Inventory.AnimalBatches";

        // 3. إدارة المشتريات والموردين
        public const string PurchasesView = "Purchases.View";
        public const string PurchasesCreate = "Purchases.Create";
        public const string PurchasesEdit = "Purchases.Edit";
        public const string PurchasesReturn = "Purchases.Return";
        public const string SuppliersManage = "Suppliers.Manage";
        public const string SuppliersPay = "Suppliers.Pay";

        // 4. المبيعات والكاشير والطلبات
        public const string SalesView = "Sales.View";
        public const string SalesCreateOrder = "Sales.CreateOrder";
        public const string SalesApplyDiscount = "Sales.ApplyDiscount";
        public const string SalesEditItemPrice = "Sales.EditItemPrice";
        public const string SalesReprintReceipt = "Sales.ReprintReceipt";
        public const string SalesHoldOrder = "Sales.HoldOrder";
        public const string SalesVoidOrder = "Sales.VoidOrder";
        public const string SalesReopenOrder = "Sales.ReopenOrder";

        // 5. المرتجعات
        public const string ReturnsView = "Returns.View";
        public const string ReturnsCreate = "Returns.Create";
        public const string ReturnsApprove = "Returns.Approve";

        // 6. المطبخ وأوامر الطبخ
        public const string KitchenView = "Kitchen.View";
        public const string KitchenStartCookingBatch = "Kitchen.StartCookingBatch";
        public const string KitchenSettleSurplus = "Kitchen.SettleSurplus";
        public const string KitchenApproveDeficit = "Kitchen.ApproveDeficit";

        // 7. الحسابات المالية والمحافظ والبنوك
        public const string FinancialAccountsView = "FinancialAccounts.View";
        public const string FinancialAccountsCreate = "FinancialAccounts.Create";
        public const string FinancialAccountsEdit = "FinancialAccounts.Edit";
        public const string FinancialAccountsStatement = "FinancialAccounts.Statement";
        public const string FinancialAccountsDeposit = "FinancialAccounts.Deposit";
        public const string FinancialAccountsWithdraw = "FinancialAccounts.Withdraw";
        public const string FinancialAccountsTransfer = "FinancialAccounts.Transfer";
        public const string FinancialAccountsOwnerBankDeposit = "FinancialAccounts.OwnerBankDeposit";

        // Aliases used by FinancialAccountsView.xaml.cs
        public const string FinancialAccounts_Manage = FinancialAccountsEdit;
        public const string FinancialAccounts_Deposit = FinancialAccountsDeposit;
        public const string FinancialAccounts_Withdraw = FinancialAccountsWithdraw;
        public const string FinancialAccounts_Transfer = FinancialAccountsTransfer;
        public const string FinancialAccounts_Statement = FinancialAccountsStatement;

        // 8. الصندوق والورديات
        public const string CashBoxView = "CashBox.View";
        public const string ShiftOpen = "Shift.Open";
        public const string ShiftClose = "Shift.Close";
        public const string CashBoxViewBalance = "CashBox.ViewBalance";
        public const string CashBoxExpenseFromDrawer = "CashBox.ExpenseFromDrawer";
        public const string CashierAuditView = "CashBox.AuditView";

        // 9. العملاء والديون
        public const string CustomersView = "Customers.View";
        public const string CustomersManage = "Customers.Manage";
        public const string CustomersSellOnCredit = "Customers.SellOnCredit";
        public const string CustomersCollectDebt = "Customers.CollectDebt";

        // 10. الموظفين والرواتب والسلف
        public const string EmployeesView = "Employees.View";
        public const string EmployeesAdd = "Employees.Add";
        public const string EmployeesEdit = "Employees.Edit";
        public const string EmployeesPermissionsManage = "Employees.PermissionsManage";
        public const string EmployeesAdvance = "Employees.Advance";
        public const string EmployeesSalaryPay = "Employees.SalaryPay";

        // 11. المصروفات والتقارير
        public const string ExpensesView = "Expenses.View";
        public const string ExpensesCreate = "Expenses.Create";
        public const string ExpensesEdit = "Expenses.Edit";
        public const string ExpensesDelete = "Expenses.Delete";
        public const string FinancialReportsView = "Reports.FinancialView";
        public const string GeneralReportsView = "Reports.GeneralView";
        public const string ReportsExportPrint = "Reports.ExportPrint";

        // 12. الإعدادات والنسخ الاحتياطي
        public const string SettingsView = "Settings.View";
        public const string SettingsEdit = "Settings.Edit";
        public const string BackupCreate = "Settings.BackupCreate";
        public const string BackupRestore = "Settings.BackupRestore";

        public static readonly IReadOnlyList<PermissionDefinition> AllDefinitions = new List<PermissionDefinition>
        {
            new() { Key = DashboardView, Category = "لوحة التحكم", DisplayName = "عرض لوحة التحكم الرئيسية" },

            new() { Key = ProductsView, Category = "المنتجات والأصناف", DisplayName = "عرض قائمة المنتجات والأصناف" },
            new() { Key = ProductsAdd, Category = "المنتجات والأصناف", DisplayName = "إضافة منتج جديد" },
            new() { Key = ProductsEdit, Category = "المنتجات والأصناف", DisplayName = "تعديل بيانات منتج" },
            new() { Key = ProductsDelete, Category = "المنتجات والأصناف", DisplayName = "حذف أو تعطيل منتج" },
            new() { Key = ProductsChangePrice, Category = "المنتجات والأصناف", DisplayName = "تعديل أسعار البيع والتكلفة" },
            new() { Key = RecipesManage, Category = "المنتجات والأصناف", DisplayName = "إدارة الوصفات ومكونات المنتجات" },
            new() { Key = DiscountsManage, Category = "المنتجات والأصناف", DisplayName = "إدارة الخصومات والعروض" },

            new() { Key = InventoryView, Category = "المخزون والمستودعات", DisplayName = "عرض المخزون والمستودعات" },
            new() { Key = InventoryAddMaterial, Category = "المخزون والمستودعات", DisplayName = "إضافة مادة خام جديدة" },
            new() { Key = InventoryEditQty, Category = "المخزون والمستودعات", DisplayName = "تعديل كميات المخزون يدوياً" },
            new() { Key = InventoryTransfer, Category = "المخزون والمستودعات", DisplayName = "التحويل بين المستودعات" },
            new() { Key = InventoryWaste, Category = "المخزون والمستودعات", DisplayName = "تسجيل التالف والهدر" },
            new() { Key = AnimalBatchesManage, Category = "المخزون والمستودعات", DisplayName = "إدارة الذبائح وتفكيكها" },

            new() { Key = PurchasesView, Category = "المشتريات والموردين", DisplayName = "عرض فواتير المشتريات" },
            new() { Key = PurchasesCreate, Category = "المشتريات والموردين", DisplayName = "إنشاء فاتورة مشتريات" },
            new() { Key = PurchasesEdit, Category = "المشتريات والموردين", DisplayName = "تعديل فاتورة مشتريات" },
            new() { Key = PurchasesReturn, Category = "المشتريات والموردين", DisplayName = "إنشاء مرتجع مشتريات" },
            new() { Key = SuppliersManage, Category = "المشتريات والموردين", DisplayName = "إدارة الموردين وكشف حساباتهم" },
            new() { Key = SuppliersPay, Category = "المشتريات والموردين", DisplayName = "صرف دفعات للموردين" },

            new() { Key = SalesView, Category = "المبيعات والكاشير والطلبات", DisplayName = "عرض سجل المبيعات والطلبات" },
            new() { Key = SalesCreateOrder, Category = "المبيعات والكاشير والطلبات", DisplayName = "البيع وإنشاء طلب جديد في الكاشير" },
            new() { Key = SalesApplyDiscount, Category = "المبيعات والكاشير والطلبات", DisplayName = "منح خصم على الفاتورة" },
            new() { Key = SalesEditItemPrice, Category = "المبيعات والكاشير والطلبات", DisplayName = "تعديل سعر الحبة داخل سلة الكاشير" },
            new() { Key = SalesReprintReceipt, Category = "المبيعات والكاشير والطلبات", DisplayName = "إعادة طباعة الفاتورة" },
            new() { Key = SalesHoldOrder, Category = "المبيعات والكاشير والطلبات", DisplayName = "تعليق واسترجاع الطلبات" },
            new() { Key = SalesVoidOrder, Category = "المبيعات والكاشير والطلبات", DisplayName = "إلغاء الفاتورة السريع (Void)" },
            new() { Key = SalesReopenOrder, Category = "المبيعات والكاشير والطلبات", DisplayName = "إلغاء الفاتورة وإعادة فتحها للتعديل" },

            new() { Key = ReturnsView, Category = "المرتجعات", DisplayName = "عرض سجل المرتجعات" },
            new() { Key = ReturnsCreate, Category = "المرتجعات", DisplayName = "إنشاء مرتجع مبيعات" },
            new() { Key = ReturnsApprove, Category = "المرتجعات", DisplayName = "اعتماد المرتجعات واسترداد المبالغ" },

            new() { Key = KitchenView, Category = "المطبخ وأوامر الطبخ", DisplayName = "عرض شاشة المطبخ وسجل الطبخ" },
            new() { Key = KitchenStartCookingBatch, Category = "المطبخ وأوامر الطبخ", DisplayName = "تشغيل قدر جديد (سحب مواد خام)" },
            new() { Key = KitchenSettleSurplus, Category = "المطبخ وأوامر الطبخ", DisplayName = "تسوية وفر وزيادة القدر دون سحب مخزون" },
            new() { Key = KitchenApproveDeficit, Category = "المطبخ وأوامر الطبخ", DisplayName = "اعتماد عجز ونقص الطبخة" },

            new() { Key = FinancialAccountsView, Category = "الحسابات والمحافظ والبنوك", DisplayName = "عرض الحسابات والمحافظ والبنوك وأرصدتها" },
            new() { Key = FinancialAccountsCreate, Category = "الحسابات والمحافظ والبنوك", DisplayName = "إضافة حساب بنكي أو محفظة جديدة" },
            new() { Key = FinancialAccountsEdit, Category = "الحسابات والمحافظ والبنوك", DisplayName = "تعديل أو تعطيل حساب مالي" },
            new() { Key = FinancialAccountsStatement, Category = "الحسابات والمحافظ والبنوك", DisplayName = "عرض وطباعة كشف الحساب" },
            new() { Key = FinancialAccountsDeposit, Category = "الحسابات والمحافظ والبنوك", DisplayName = "تسجيل إيداع في حساب" },
            new() { Key = FinancialAccountsWithdraw, Category = "الحسابات والمحافظ والبنوك", DisplayName = "تسجيل سحب أو صرف من حساب" },
            new() { Key = FinancialAccountsTransfer, Category = "الحسابات والمحافظ والبنوك", DisplayName = "التحويل بين الحسابات المالية" },
            new() { Key = FinancialAccountsOwnerBankDeposit, Category = "الحسابات والمحافظ والبنوك", DisplayName = "إيداع صاحب المطعم من الصندوق للبنك" },

            new() { Key = CashBoxView, Category = "الصندوق والورديات", DisplayName = "عرض إدارة الكاشير والورديات" },
            new() { Key = ShiftOpen, Category = "الصندوق والورديات", DisplayName = "فتح وردية كاشير" },
            new() { Key = ShiftClose, Category = "الصندوق والورديات", DisplayName = "إغلاق وتسليم وردية كاشير" },
            new() { Key = CashBoxViewBalance, Category = "الصندوق والورديات", DisplayName = "عرض رصيد الصندوق وحركاته" },
            new() { Key = CashBoxExpenseFromDrawer, Category = "الصندوق والورديات", DisplayName = "تسجيل مصروف نقدي من درج الكاشير" },
            new() { Key = CashierAuditView, Category = "الصندوق والورديات", DisplayName = "عرض رقابة وتدقيق الكاشير" },

            new() { Key = CustomersView, Category = "العملاء والديون", DisplayName = "عرض قائمة العملاء ومديونياتهم" },
            new() { Key = CustomersManage, Category = "العملاء والديون", DisplayName = "إضافة وتعديل بيانات العملاء" },
            new() { Key = CustomersSellOnCredit, Category = "العملاء والديون", DisplayName = "البيع الآجل (على الحساب)" },
            new() { Key = CustomersCollectDebt, Category = "العملاء والديون", DisplayName = "تحصيل وتسديد ديون العملاء" },

            new() { Key = EmployeesView, Category = "الموظفين والرواتب", DisplayName = "عرض قائمة الموظفين" },
            new() { Key = EmployeesAdd, Category = "الموظفين والرواتب", DisplayName = "إضافة موظف جديد" },
            new() { Key = EmployeesEdit, Category = "الموظفين والرواتب", DisplayName = "تعديل بيانات الموظفين" },
            new() { Key = EmployeesPermissionsManage, Category = "الموظفين والرواتب", DisplayName = "تعديل صلاحيات الموظفين" },
            new() { Key = EmployeesAdvance, Category = "الموظفين والرواتب", DisplayName = "صرف سلفة لموظف" },
            new() { Key = EmployeesSalaryPay, Category = "الموظفين والرواتب", DisplayName = "صرف رواتب الموظفين" },

            new() { Key = ExpensesView, Category = "المصروفات والتقارير", DisplayName = "عرض إدارة المصروفات والسندات" },
            new() { Key = ExpensesCreate, Category = "المصروفات والتقارير", DisplayName = "إضافة مصروف أو سند قبض/صرف" },
            new() { Key = ExpensesEdit, Category = "المصروفات والتقارير", DisplayName = "تعديل مصروف" },
            new() { Key = ExpensesDelete, Category = "المصروفات والتقارير", DisplayName = "حذف مصروف" },
            new() { Key = FinancialReportsView, Category = "المصروفات والتقارير", DisplayName = "عرض التقارير المالية ومطابقة الإيداعات" },
            new() { Key = GeneralReportsView, Category = "المصروفات والتقارير", DisplayName = "عرض التقارير العامة والشهرية" },
            new() { Key = ReportsExportPrint, Category = "المصروفات والتقارير", DisplayName = "طباعة وتصدير التقارير" },

            new() { Key = SettingsView, Category = "الإعدادات والنظام", DisplayName = "الدخول إلى إعدادات النظام" },
            new() { Key = SettingsEdit, Category = "الإعدادات والنظام", DisplayName = "تعديل إعدادات المطعم والضرائب والطابعات" },
            new() { Key = BackupCreate, Category = "الإعدادات والنظام", DisplayName = "إنشاء نسخة احتياطية" },
            new() { Key = BackupRestore, Category = "الإعدادات والنظام", DisplayName = "استعادة نسخة احتياطية" }
        };

        public static readonly IReadOnlyDictionary<string, string[]> RoleTemplates = new Dictionary<string, string[]>
        {
            ["كاشير"] = new[]
            {
                SalesCreateOrder, SalesApplyDiscount, SalesReprintReceipt, SalesHoldOrder,
                SalesVoidOrder, SalesReopenOrder, ReturnsCreate,
                ShiftOpen, ShiftClose, CashBoxViewBalance, CashBoxExpenseFromDrawer,
                CustomersSellOnCredit, CustomersCollectDebt
            },
            ["مسؤول مخزون"] = new[]
            {
                ProductsView, RecipesManage,
                InventoryView, InventoryAddMaterial, InventoryEditQty, InventoryTransfer, InventoryWaste, AnimalBatchesManage,
                KitchenView, KitchenStartCookingBatch, KitchenSettleSurplus
            },
            ["مسؤول مشتريات"] = new[]
            {
                InventoryView, InventoryAddMaterial,
                PurchasesView, PurchasesCreate, PurchasesEdit, PurchasesReturn,
                SuppliersManage, SuppliersPay
            },
            ["محاسب"] = new[]
            {
                DashboardView, SalesView, ReturnsView, PurchasesView, SuppliersManage, SuppliersPay,
                FinancialAccountsView, FinancialAccountsCreate, FinancialAccountsEdit, FinancialAccountsStatement,
                FinancialAccountsDeposit, FinancialAccountsWithdraw, FinancialAccountsTransfer, FinancialAccountsOwnerBankDeposit,
                CashBoxView, CashBoxViewBalance, CashierAuditView,
                CustomersView, CustomersCollectDebt,
                EmployeesView, EmployeesAdvance, EmployeesSalaryPay,
                ExpensesView, ExpensesCreate, ExpensesEdit,
                FinancialReportsView, GeneralReportsView, ReportsExportPrint
            },
            ["مدير فرع"] = AllDefinitions
                .Where(d => d.Key != BackupRestore && d.Key != EmployeesPermissionsManage)
                .Select(d => d.Key)
                .ToArray(),
            ["مدير كامل الصلاحيات"] = AllDefinitions.Select(d => d.Key).ToArray()
        };
    }

    public static class PermissionGuard
    {
        private static readonly HashSet<string> AdminPanelScreenPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            SystemPermissions.DashboardView,
            SystemPermissions.ProductsView,
            SystemPermissions.InventoryView,
            SystemPermissions.AddonsManage,
            SystemPermissions.DiscountsManage,
            SystemPermissions.PurchasesView,
            SystemPermissions.SalesView,
            SystemPermissions.ReturnsView,
            SystemPermissions.CustomersView,
            SystemPermissions.EmployeesView,
            SystemPermissions.CashBoxView,
            SystemPermissions.KitchenView,
            SystemPermissions.FinancialAccountsView,
            SystemPermissions.ExpensesView,
            SystemPermissions.FinancialReportsView,
            SystemPermissions.GeneralReportsView,
            SystemPermissions.SettingsView
        };

        public static HashSet<string> GetEffectivePermissions(User? user)
        {
            if (user == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (user.Role == UserRole.Admin || user.Id == 1)
            {
                return new HashSet<string>(
                    SystemPermissions.AllDefinitions.Select(d => d.Key),
                    StringComparer.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(user.PermissionsJson) &&
                !string.Equals(user.RoleTemplateName?.Trim(), "كاشير", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(user.PermissionsJson);
                    if (list != null)
                    {
                        var set = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                        // Grant section view permission ONLY for admin-only sub-permissions (never for cashier-level sub-permissions unless explicitly checked)
                        if (set.Contains(SystemPermissions.ProductsAdd) || set.Contains(SystemPermissions.ProductsEdit) || set.Contains(SystemPermissions.ProductsDelete) || set.Contains(SystemPermissions.ProductsChangePrice) || set.Contains(SystemPermissions.RecipesManage))
                            set.Add(SystemPermissions.ProductsView);
                        if (set.Contains(SystemPermissions.InventoryAddMaterial) || set.Contains(SystemPermissions.InventoryEditQty) || set.Contains(SystemPermissions.InventoryTransfer) || set.Contains(SystemPermissions.InventoryWaste))
                            set.Add(SystemPermissions.InventoryView);
                        if (set.Any(p => p.StartsWith("Purchases.", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Suppliers.", StringComparison.OrdinalIgnoreCase)))
                            set.Add(SystemPermissions.PurchasesView);
                        if (set.Any(p => p.StartsWith("FinancialAccounts.", StringComparison.OrdinalIgnoreCase)))
                            set.Add(SystemPermissions.FinancialAccountsView);
                        if (set.Any(p => p.StartsWith("Expenses.", StringComparison.OrdinalIgnoreCase)))
                            set.Add(SystemPermissions.ExpensesView);
                        if (set.Any(p => p.StartsWith("Employees.", StringComparison.OrdinalIgnoreCase)))
                            set.Add(SystemPermissions.EmployeesView);
                        if (set.Contains(SystemPermissions.CustomersManage))
                            set.Add(SystemPermissions.CustomersView);
                        if (set.Contains(SystemPermissions.CashierAuditView))
                            set.Add(SystemPermissions.CashBoxView);
                        if (set.Any(p => p.StartsWith("Settings.", StringComparison.OrdinalIgnoreCase)))
                            set.Add(SystemPermissions.SettingsView);
                        return set;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(user.RoleTemplateName) &&
                SystemPermissions.RoleTemplates.TryGetValue(user.RoleTemplateName, out var templatePerms))
            {
                return new HashSet<string>(templatePerms, StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>(
                SystemPermissions.RoleTemplates["كاشير"],
                StringComparer.OrdinalIgnoreCase);
        }

        public static bool HasPermission(User? user, string permissionKey)
        {
            if (user == null) return false;
            if (!user.IsActive && user.Role != UserRole.Admin && user.Id != 1) return false;
            if (user.Role == UserRole.Admin || user.Id == 1) return true;

            var perms = GetEffectivePermissions(user);
            return perms.Contains(permissionKey);
        }

        public static bool EnsurePermission(User? user, string permissionKey, string? actionDisplayName = null)
        {
            if (HasPermission(user, permissionKey))
                return true;

            var def = SystemPermissions.AllDefinitions.FirstOrDefault(d => d.Key.Equals(permissionKey, StringComparison.OrdinalIgnoreCase));
            string name = actionDisplayName ?? def?.DisplayName ?? permissionKey;
            MessageBox.Show($"ليس لديك صلاحية تنفيذ هذه العملية:\n{name}", "صلاحية غير متوفرة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        public static bool CanEnterAdminPanelFromCashier(User? user)
        {
            if (user == null)
                return false;

            if (user.Role == UserRole.Admin || user.Id == 1)
                return true;

            // If employee template is explicitly "كاشير", do NOT show the button to enter Admin Dashboard
            if (string.IsNullOrWhiteSpace(user.RoleTemplateName) ||
                string.Equals(user.RoleTemplateName.Trim(), "كاشير", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // If "مخصص" (or another non-cashier template), show the button if they have any admin screen permission
            var perms = GetEffectivePermissions(user);
            return perms.Any(p => AdminPanelScreenPermissions.Contains(p));
        }

        public static bool ShouldOpenAdminDashboardOnLogin(User user)
        {
            if (user.Role == UserRole.Admin || user.Id == 1)
                return true;

            if (string.Equals(user.RoleTemplateName?.Trim(), "كاشير", StringComparison.OrdinalIgnoreCase))
                return false;

            // If the employee's job title is Cashier ("كاشير") and their role is "مخصص", open CashierWindow first (with the button to enter Admin Dashboard)
            if (!string.IsNullOrWhiteSpace(user.JobTitle) && user.JobTitle.Contains("كاشير") &&
                string.Equals(user.RoleTemplateName?.Trim(), "مخصص", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var perms = GetEffectivePermissions(user);
            bool hasAdminScreen = perms.Any(p => AdminPanelScreenPermissions.Contains(p));
            bool hasCashierCreate = perms.Contains(SystemPermissions.SalesCreateOrder);

            if (hasAdminScreen && !hasCashierCreate)
                return true;

            if (user.RoleTemplateName is "مسؤول مخزون" or "مسؤول مشتريات" or "محاسب" or "مدير فرع" or "مدير كامل الصلاحيات")
                return true;

            return false;
        }

        public static string SerializePermissions(IEnumerable<string> permissions)
        {
            return JsonSerializer.Serialize(permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
    }
}
