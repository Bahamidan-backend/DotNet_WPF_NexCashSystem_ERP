using POSSystem.WPF.Services.Logging;

using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Views;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels.Orders;
using POSSystem.WPF.ViewModels.Settings;
using POSSystem.WPF.ViewModels.Admin;
using POSSystem.WPF.ViewModels.Warehouses;
using POSSystem.WPF.ViewModels.Suppliers;
using POSSystem.WPF.ViewModels.Employees;
using POSSystem.WPF.ViewModels.Customers;
using POSSystem.WPF.ViewModels.Products;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels.Discounts;
using POSSystem.WPF.ViewModels.Categories;
using POSSystem.WPF.ViewModels.Sales;
using POSSystem.WPF.ViewModels.Purchases;
using POSSystem.WPF.ViewModels.Returns;
using POSSystem.WPF.ViewModels.Expenses;
using POSSystem.WPF.ViewModels.Reports;
using POSSystem.WPF.ViewModels.Kitchen;
using POSSystem.WPF.ViewModels.AnimalBatches;

namespace POSSystem.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Standardize culture and date format across all machines (Gregorian Arabic yyyy/MM/dd)
            ConfigureGlobalCulture();

            // Register global crash and unhandled exception interceptors immediately
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                AppLogger.LogError("Unobserved Task Exception in background worker", args.Exception);
                args.SetObserved();
            };

            AppLogger.LogInfo("================ SYSTEM INITIALIZATION STARTED ================");
            AppLogger.LogInfo($"Environment: {Environment.OSVersion} | 64-bit OS: {Environment.Is64BitOperatingSystem} | Machine: {Environment.MachineName}");
            AppLogger.LogInfo($"Working Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            AppLogger.LogInfo($"Logs Directory: {AppLogger.LogDirectory}");
            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                // Initialize the application services using the DI provider
                await ServiceLocator.InitializeAsync(ServiceProvider);

                // Check license expiration
                var licenseService = new Services.LicenseService();
                string license = licenseService.LoadLicense();
                if (!string.IsNullOrEmpty(license))
                {
                    int daysRemaining = licenseService.GetDaysRemaining(license);
                    if (daysRemaining >= 0 && daysRemaining <= 5)
                    {
                        string message = daysRemaining == 0 
                            ? "تنتهي صلاحية الترخيص اليوم! يرجى التجديد لتجنب توقف البرنامج." 
                            : $"تنتهي صلاحية الترخيص خلال {daysRemaining} أيام. يرجى التجديد قريباً.";
                        
                        MessageBox.Show(message, "تنبيه انتهاء الترخيص", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                // Create a scope for the login window and show it
                ShowWindowInScope<LoginWindow>();

                // Global exception handlers to capture crashes during navigation
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // فحص تلقائي للتحديثات (في الخلفية بدون إعاقة التشغيل)
                _ = CheckForUpdateSilentlyAsync();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                var crashLogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt");
                System.IO.File.WriteAllText(crashLogPath, ex.ToString());
                MessageBox.Show($"Application startup error: {ex.Message}\nCheck {crashLogPath} for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Creates a new DI scope and shows the specified window within that scope.
        /// This ensures each window gets its own DbContext/UnitOfWork instance.
        /// </summary>
        public static void ShowWindowInScope<TWindow>() where TWindow : Window
        {
            var scope = ServiceProvider.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<TWindow>();
            
            if (Current != null && (Current.MainWindow == null || !Current.MainWindow.IsVisible))
            {
                Current.MainWindow = window;
            }

            // Keep scope alive while window is open
            window.Closed += (s, e) => scope.Dispose();
            
            window.Show();
        }

        /// <summary>
        /// Creates a new DI scope and shows the specified window as a dialog within that scope.
        /// </summary>
        public static bool? ShowDialogInScope<TWindow>() where TWindow : Window
        {
            using var scope = ServiceProvider.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<TWindow>();
            return window.ShowDialog();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // Ignore transient Windows Clipboard lock contention (0x800401D0 / CLIPBRD_E_CANT_OPEN) and copy safely
                if (e.Exception is System.Runtime.InteropServices.COMException comEx &&
                    ((uint)comEx.ErrorCode == 0x800401D0 || e.Exception.Message.Contains("CLIPBRD_E_CANT_OPEN") || e.Exception.Message.Contains("OpenClipboard")))
                {
                    if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox tb && !string.IsNullOrEmpty(tb.SelectedText))
                    {
                        POSSystem.WPF.Helpers.SafeClipboard.SetText(tb.SelectedText);
                    }
                    e.Handled = true;
                    return;
                }
                if (e.Exception?.Message != null && (e.Exception.Message.Contains("CLIPBRD_E_CANT_OPEN") || e.Exception.Message.Contains("OpenClipboard")))
                {
                    if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox tb && !string.IsNullOrEmpty(tb.SelectedText))
                    {
                        POSSystem.WPF.Helpers.SafeClipboard.SetText(tb.SelectedText);
                    }
                    e.Handled = true;
                    return;
                }

                AppLogger.LogFatal(e.Exception, "Unhandled UI Exception caught by DispatcherUnhandledException");
                MessageBox.Show($"حدث خطأ غير متوقع في النظام: {e.Exception.Message}\n\nتم تسجيل تفاصيل العطل في السجل:\n{AppLogger.LogDirectory}", "خطأ غير متوقع", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    AppLogger.LogFatal(ex, $"Unhandled Background Exception (IsTerminating: {e.IsTerminating})");
                }
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                ServiceLocator.Dispose();
                AppLogger.LogInfo($"Application shutting down cleanly with ExitCode: {e.ApplicationExitCode}");
                AppLogger.Shutdown();
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error during application shutdown", ex);
            }
            
            base.OnExit(e);
        }

        /// <summary>
        /// فحص تلقائي صامت للتحديثات عند بدء التشغيل
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdateSilentlyAsync()
        {
            try
            {
                // انتظار 5 ثوانٍ حتى يكتمل تشغيل البرنامج
                await System.Threading.Tasks.Task.Delay(5000);

                var updateService = new Services.UpdateService();
                var updateInfo = await updateService.CheckForUpdateAsync();

                if (updateInfo != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"يتوفر تحديث جديد للبرنامج!\n\n" +
                            $"الإصدار الجديد: {updateInfo.Version}\n" +
                            $"الإصدار الحالي: {updateService.GetCurrentVersion()}\n\n" +
                            $"يمكنك تحميل التحديث من صفحة الإعدادات ← تحديثات البرنامج",
                            "🔄 تحديث متاح",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                // لا نعرض أي خطأ للمستخدم - الفحص الصامت لا يزعج المستخدم
                System.Diagnostics.Debug.WriteLine($"Silent update check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// يوحد إعدادات اللغة وتنسيق التاريخ عالمياً على مستوى التطبيق بالكامل
        /// بحيث يظهر التاريخ بالتقويم الميلادي بتنسيق سنة/شهر/يوم (yyyy/MM/dd م)
        /// بغض النظر عن لغة أو إعدادات نظام التشغيل ويندوز لدى العميل لمنع أي لبس بين اليوم والشهر.
        /// </summary>
        private static void ConfigureGlobalCulture()
        {
            try
            {
                var arabicCulture = new CultureInfo("ar-YE");
                arabicCulture.DateTimeFormat.Calendar = new GregorianCalendar();
                arabicCulture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
                arabicCulture.DateTimeFormat.LongDatePattern = "yyyy/MM/dd";
                arabicCulture.DateTimeFormat.YearMonthPattern = "yyyy MMMM";
                arabicCulture.DateTimeFormat.DateSeparator = "/";
                arabicCulture.NumberFormat.CurrencySymbol = "ريال";
                arabicCulture.NumberFormat.CurrencyPositivePattern = 3; // n $ -> 100.00 ريال
                arabicCulture.NumberFormat.CurrencyNegativePattern = 8; // -n $ -> -100.00 ريال

                CultureInfo.DefaultThreadCurrentCulture = arabicCulture;
                CultureInfo.DefaultThreadCurrentUICulture = arabicCulture;
                Thread.CurrentThread.CurrentCulture = arabicCulture;
                Thread.CurrentThread.CurrentUICulture = arabicCulture;

                // توحيد لغة عناصر WPF لمنع الرجوع للغة الإنجليزية en-US أو الإماراتية ar-AE الافتراضية
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ar-YE")));

                // شبكة أمان لمدخلات DatePicker: معالجة الأرقام العربية ومنع أي خطأ استثنائي عند الكتابة اليدوية
                EventManager.RegisterClassHandler(
                    typeof(DatePicker),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler((sender, args) =>
                    {
                        if (sender is DatePicker dp)
                        {
                            dp.DateValidationError -= OnDatePickerDateValidationError;
                            dp.DateValidationError += OnDatePickerDateValidationError;
                        }
                    }));

                // حماية شاملة ضد خطأ OpenClipboard Failed (0x800401D0) عند النسخ Ctrl+C في أي حقل نصي
                System.Windows.Input.CommandManager.RegisterClassCommandBinding(
                    typeof(System.Windows.Controls.TextBox),
                    new System.Windows.Input.CommandBinding(
                        System.Windows.Input.ApplicationCommands.Copy,
                        (sender, args) =>
                        {
                            if (sender is System.Windows.Controls.TextBox tb && !string.IsNullOrEmpty(tb.SelectedText))
                            {
                                POSSystem.WPF.Helpers.SafeClipboard.SetText(tb.SelectedText);
                                args.Handled = true;
                            }
                        }));

                AppLogger.LogInfo("Global Gregorian Arabic culture (yyyy/MM/dd) initialized successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Failed to configure global culture", ex);
            }
        }

        private static void OnDatePickerDateValidationError(object? sender, DatePickerDateValidationErrorEventArgs args)
        {
            if (sender is DatePicker dp && !string.IsNullOrWhiteSpace(args.Text))
            {
                string normalized = NormalizeArabicDigitsAndDateText(args.Text);
                string[] formats = new[]
                {
                    "yyyy/MM/dd", "yyyy/M/d", "yyyy-MM-dd", "yyyy-M-d",
                    "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                    "yyyy/MM/dd \u0645", "dd/MM/yyyy \u0645"
                };

                if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
                    DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    dp.SelectedDate = parsedDate;
                }
            }
            args.ThrowException = false;
        }

        private static string NormalizeArabicDigitsAndDateText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value
                .Replace('٠', '0')
                .Replace('١', '1')
                .Replace('٢', '2')
                .Replace('٣', '3')
                .Replace('٤', '4')
                .Replace('٥', '5')
                .Replace('٦', '6')
                .Replace('٧', '7')
                .Replace('٨', '8')
                .Replace('٩', '9')
                .Replace('م', ' ')
                .Trim();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database - Scoped: one DbContext per logical operation/window
            services.AddDbContext<POSDbContext>(options => { }, ServiceLifetime.Scoped);
            
            // UnitOfWork - Scoped: shares the same DbContext within a scope
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Services - Scoped: share UnitOfWork/DbContext within a scope
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<ICustomerDebtService, CustomerDebtService>();
            services.AddScoped<IKitchenService, KitchenService>();
            services.AddScoped<IAddonService, AddonService>();
            services.AddScoped<IReturnOrderService, ReturnOrderService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
            services.AddScoped<IAnimalBatchService, AnimalBatchService>();
            services.AddScoped<IBankDepositService, BankDepositService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IFinancialEngineService, FinancialEngineService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<ICashBoxTransactionService, CashBoxTransactionService>();
            services.AddScoped<IShiftService, ShiftService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<IHeldOrderService, HeldOrderService>();
            services.AddScoped<IStockMovementService, StockMovementService>();
            services.AddScoped<IStockWasteService, StockWasteService>();
            services.AddScoped<IKitchenCookingBatchService, KitchenCookingBatchService>();
            
            // Missing Services - including Expense services
            services.AddScoped<IDiscountsServices, DiscountsServices>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<IReceiptVoucherService, ReceiptVoucherService>();
            services.AddScoped<IRecipesServices, RecipesServices>(); 
            services.AddScoped<IAddonAndOrderItemService, AddonAndOrderItemService>();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IFinancialChannelService, FinancialChannelService>();
            services.AddScoped<IFinancialAccountService, FinancialAccountService>();

            // Windows - Transient: new instance per show
            services.AddTransient<LoginWindow>();
            services.AddTransient<CashierWindow>();
            services.AddTransient<AdminDashboardWindow>();
        }
    }
}
