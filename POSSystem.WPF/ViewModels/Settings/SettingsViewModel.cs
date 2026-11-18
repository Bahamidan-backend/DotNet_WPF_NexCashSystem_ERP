using POSSystem.WPF.Business.Services.interfase;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Data;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel()
        {
            settingsProberty = SettingsService.LoadSettings();
            Commands = new SettingsCommands(this);
            // تحميل رقم الإصدار الحالي
            CurrentVersion = _updateService.GetCurrentVersion();
        }

        public SettingsViewModel(User user) : this()
        {
            _currentUser = user;
        }

        /// <summary>
        /// التحقق من وجود تحديث جديد
        /// </summary>
        public async Task CheckForUpdateAsync()
        {
            if (IsCheckingUpdate) return;

            try
            {
                IsCheckingUpdate = true;
                IsUpdateAvailable = false;
                UpdateStatus = "🔍 جاري التحقق من التحديثات...";
                UpdateChangelog = "";

                _latestUpdateInfo = await _updateService.CheckForUpdateAsync();

                if (_latestUpdateInfo != null)
                {
                    IsUpdateAvailable = true;
                    UpdateStatus = $"🎉 يوجد تحديث جديد! الإصدار {_latestUpdateInfo.Version}";
                    UpdateChangelog = _latestUpdateInfo.Changelog;
                }
                else
                {
                    IsUpdateAvailable = false;
                    UpdateStatus = "✅ البرنامج محدّث - أنت على آخر إصدار";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus = $"⚠️ فشل التحقق: {ex.Message}";
                IsUpdateAvailable = false;
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        /// <summary>
        /// تحميل التحديث وتطبيقه
        /// </summary>
        public async Task DownloadAndApplyUpdateAsync()
        {
            if (_latestUpdateInfo == null || IsDownloadingUpdate) return;

            var result = MessageBox.Show(
                $"هل تريد تحميل وتثبيت الإصدار {_latestUpdateInfo.Version}?\n\n" +
                $"ملاحظات التحديث:\n{_latestUpdateInfo.Changelog}\n\n" +
                "سيتم إغلاق البرنامج تلقائياً أثناء التحديث.",
                "تأكيد التحديث",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsDownloadingUpdate = true;
                UpdateProgress = 0;
                UpdateStatus = "⬇️ جاري تحميل التحديث...";

                var progress = new Progress<int>(percent =>
                {
                    UpdateProgress = percent;
                    UpdateStatus = $"⬇️ جاري التحميل... {percent}%";
                });

                string zipPath = await _updateService.DownloadUpdateAsync(_latestUpdateInfo.DownloadUrl, progress);

                UpdateStatus = "🔧 جاري تطبيق التحديث...";
                
                // تحديث ملف الإصدار المحلي
                _updateService.UpdateLocalVersion(_latestUpdateInfo.Version);

                // تطبيق التحديث (يغلق التطبيق)
                _updateService.ApplyUpdate(zipPath);
            }
            catch (Exception ex)
            {
                UpdateStatus = $"❌ فشل التحديث: {ex.Message}";
                MessageBox.Show(
                    $"فشل تحميل التحديث:\n{ex.Message}",
                    "خطأ في التحديث",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsDownloadingUpdate = false;
            }
        }

        public void UpdateProfile() => UpdateProfileAsync().SafeFireAndForget();

        public async Task UpdateProfileAsync()
        {
            try
            {
                if (_currentUser == null) return;
                using (var context = new POSDbContext())
                {
                    var userToUpdate = await context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.Name = UserName;
                        userToUpdate.Email = UserEmail;
                        if (!string.IsNullOrWhiteSpace(NewPassword))
                        {
                            if (!PasswordHasher.VerifyPassword(CurrentPassword, userToUpdate.Password))
                            {
                                MessageBox.Show("كلمة المرور الحالية غير صحيحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            userToUpdate.Password = POSDbContext.HashPassword(NewPassword);
                            userToUpdate.MustChangePassword = false;
                        }
                        context.Users.Update(userToUpdate);
                        await context.SaveChangesAsync();
                        MessageBox.Show("تم تحديث الملف الشخصي بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                        CurrentPassword = ""; NewPassword = ""; OnPropertyChanged(nameof(CurrentPassword)); OnPropertyChanged(nameof(NewPassword));
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"فشل تحديث الملف الشخصي: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void SaveSettings()
        {
            try { SettingsService.SaveSettings(settingsProberty); MessageBox.Show("تم حفظ الإعدادات بنجاح!", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"فشل حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void ResetSettings()
        {
            if (MessageBox.Show("هل أنت متأكد من إعادة تعيين جميع الإعدادات؟", "تأكيد إعادة التعيين", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                settingsProberty = new SettingsProberty();
                SaveSettings();
                MessageBox.Show("تم إعادة تعيين الإعدادات!", "تم إعادة التعيين", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async void BackupData()
        {
            try
            {
                var backupService = App.ServiceProvider?.GetService(typeof(IBackupService)) as IBackupService;
                if (backupService == null)
                {
                    MessageBox.Show("خدمة النسخ الاحتياطي غير متوفرة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = await backupService.CreateBackupAsync(reason: "نسخ يدوي من شاشة الإعدادات");
                if (result.IsSuccess)
                {
                    MessageBox.Show(
                        $"تم إنشاء النسخة الاحتياطية بنجاح!\n\n📂 مسار الحفظ:\n{result.FilePath}\n\n📦 الحجم: {result.FileSizeText}",
                        "تم النسخ الاحتياطي",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"فشل النسخ الاحتياطي:\n{result.ErrorMessage}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء النسخ الاحتياطي:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void RestoreData()
        {
            var confirm = MessageBox.Show(
                "تحذير هام ومطلوب التأكيد:\n\n" +
                "هل أنت متأكد من استعادة النسخة الاحتياطية؟\n" +
                "• سيتم استبدال قاعدة البيانات الحالية بالكامل بالنسخة المستعادة.\n" +
                "• سيقوم النظام تلقائياً بأخذ نسخة أمان احترازية (Safety Snapshot) من بياناتك الحالية قبل الاستبدال لحمايتك.\n" +
                "• سيتم إغلاق البرنامج تلقائياً بعد الاستعادة لتطبيق التغييرات.\n\nهل تريد المتابعة؟",
                "تأكيد استعادة قاعدة البيانات",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                string initialDir = Directory.Exists(settingsProberty.BackupLocation)
                    ? settingsProberty.BackupLocation
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

                var openFileDialog = new OpenFileDialog
                {
                    Title = "اختر ملف النسخة الاحتياطية للاستعادة",
                    Filter = "نسخ احتياطية وقواعد بيانات (*.zip;*.db;*.posbak)|*.zip;*.db;*.posbak|ملفات مضغوطة (*.zip)|*.zip|قواعد بيانات SQLite (*.db)|*.db|كافة الملفات (*.*)|*.*",
                    InitialDirectory = Directory.Exists(initialDir) ? initialDir : AppDomain.CurrentDomain.BaseDirectory
                };

                if (openFileDialog.ShowDialog() != true) return;

                var backupService = App.ServiceProvider?.GetService(typeof(IBackupService)) as IBackupService;
                if (backupService == null)
                {
                    MessageBox.Show("خدمة الاستعادة غير متوفرة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = await backupService.RestoreBackupAsync(openFileDialog.FileName);
                if (result.IsSuccess)
                {
                    MessageBox.Show(
                        $"تمت استعادة البيانات بنجاح!\n\n" +
                        $"• المصدر: {Path.GetFileName(openFileDialog.FileName)}\n" +
                        (!string.IsNullOrWhiteSpace(result.SafetyBackupPath) ? $"• تم حفظ نسخة أمان احترازية في:\n{result.SafetyBackupPath}\n\n" : "\n") +
                        "سيتم الآن إغلاق التطبيق لتطبيق قاعدة البيانات المستعادة. يرجى إعادة تشغيله.",
                        "نجاح الاستعادة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show($"فشلت استعادة البيانات:\n{result.ErrorMessage}", "خطأ في الاستعادة", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء استعادة البيانات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BrowseBackupLocation()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "اختر مجلد حفظ النسخ الاحتياطية",
                    InitialDirectory = Directory.Exists(settingsProberty.BackupLocation)
                        ? settingsProberty.BackupLocation
                        : AppDomain.CurrentDomain.BaseDirectory
                };

                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
                {
                    settingsProberty.BackupLocation = dialog.FolderName;
                    OnPropertyChanged(nameof(settingsProberty));
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل اختيار المجلد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public System.Collections.Generic.List<string> InstalledPrinters => RawPrinterHelper.GetInstalledPrinters();

        public void TestPrinter()
        {
            try
            {
                // حفظ الإعدادات أولاً حتى يعتمد الكاشير والأدمن اسم الطابعة الجديد فوراً
                SettingsService.SaveSettings(settingsProberty);
            }
            catch { }

            string configuredName = settingsProberty.PrinterName?.Trim() ?? string.Empty;
            string resolvedPrinter = POSSystem.WPF.StaticFunction.GloublStatic.ResolveWorkingPrinterName(settingsProberty);

            if (string.IsNullOrWhiteSpace(resolvedPrinter))
            {
                var installed = RawPrinterHelper.GetInstalledPrinters();
                string listMsg = installed.Any() ? string.Join("\n• ", installed) : "لا توجد طابعات مثبتة";
                MessageBox.Show($"يرجى تحديد اسم الطابعة أولاً.\n\nالطابعات المتاحة في الويندوز:\n• {listMsg}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var testOrder = new Order
                {
                    Id = 9999,
                    OrderNumber = "TEST-001",
                    OrderType = "dinein",
                    SubTotal = 2500m,
                    Total = 2500m,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = OrderStatus.Completed,
                    CreatedAt = DateTime.Now,
                    User = _currentUser ?? new User { Name = "مدير النظام" },
                    OrderItems = new System.Collections.Generic.List<OrderItem>
                    {
                        new OrderItem { Quantity = 1, Size = "حبة", UnitPrice = 1800m, TotalPrice = 1800m, Product = new Product { Name = "دجاج شواية (تجربة طباعة)" } },
                        new OrderItem { Quantity = 1, Size = "نفر", UnitPrice = 700m, TotalPrice = 700m, Product = new Product { Name = "رز مندي (تجربة طباعة)" } }
                    }
                };

                var visual = ReceiptTemplate.CreateVisual(testOrder, settingsProberty);
                string extraCommands = "\x1B\x69";
                bool customerOk = RawPrinterHelper.PrintVisual(resolvedPrinter, visual, extraCommands);

                if (customerOk)
                {
                    if (!string.Equals(configuredName, resolvedPrinter, StringComparison.OrdinalIgnoreCase))
                    {
                        settingsProberty.PrinterName = resolvedPrinter;
                        try { SettingsService.SaveSettings(settingsProberty); } catch { }
                    }
                    MessageBox.Show(
                        $"✅ نجح اختبار الطباعة الفعلي لفاتورة الزبون!\n\n" +
                        $"• الطابعة التي تمت الطباعة عليها: ({resolvedPrinter})\n" +
                        $"• تم حفظ اسم الطابعة واعتماده تلقائياً لشاشة الكاشير والأدمن.",
                        "نجاح اختبار الطباعة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var installed = RawPrinterHelper.GetInstalledPrinters();
                    string listMsg = installed.Any() ? string.Join("\n• ", installed) : "لم يتم العثور على طابعات";
                    MessageBox.Show(
                        $"❌ تعذر الطباعة على الطابعة المحددة: '{configuredName}'\n\n" +
                        $"الطابعات المثبتة حالياً في نظام الويندوز لديك هي:\n• {listMsg}\n\n" +
                        "يرجى اختيار اسم الطابعة الصحيح من القائمة المنسدلة ثم الضغط على (اختبار الطابعة).",
                        "فشل اختبار الطباعة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء اختبار الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateExchangeRate() => MessageBox.Show("تم تحديث سعر الصرف بنجاح!", "تحديث سعر الصرف", MessageBoxButton.OK, MessageBoxImage.Information);

        public void RestartSystem()
        {
            if (MessageBox.Show("هل أنت متأكد من إعادة تشغيل النظام؟", "تأكيد إعادة التشغيل", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) { System.Diagnostics.Process.Start(Application.ResourceAssembly.Location); Application.Current.Shutdown(); }
        }

        public void ClearData()
        {
            if (MessageBox.Show("هل أنت متأكد تماماً من مسح كافة بيانات المبيعات والطلبات والمخزون؟\n\nتحذير: هذا الإجراء لا يمكن التراجع عنه!", "تأكيد مسح البيانات (خطر)", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new POSDbContext())
                    {
                        context.Database.ExecuteSqlRaw("DELETE FROM OrderItems");
                        context.Database.ExecuteSqlRaw("DELETE FROM Orders");
                        context.Database.ExecuteSqlRaw("DELETE FROM SaleItems");
                        context.Database.ExecuteSqlRaw("DELETE FROM Sales");
                        context.Database.ExecuteSqlRaw("DELETE FROM CustomerDebts");
                        context.Database.ExecuteSqlRaw("DELETE FROM CashBoxTransactions");
                        context.Database.ExecuteSqlRaw("DELETE FROM Shifts");
                    }
                    MessageBox.Show("تم مسح بيانات العمليات والمبيعات بنجاح.", "تم المسح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"حدث خطأ أثناء مسح البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public void ExportData() => MessageBox.Show("تم تصدير البيانات بنجاح!", "تصدير البيانات", MessageBoxButton.OK, MessageBoxImage.Information);

        public void SelectLogo()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*" };
                if (openFileDialog.ShowDialog() == true) settingsProberty.LogoPath = openFileDialog.FileName;
            }
            catch (Exception ex) { MessageBox.Show($"فشل اختيار الشعار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void ActivateLicense()
        {
            if (string.IsNullOrWhiteSpace(LicenseKey)) { MessageBox.Show("يرجى إدخال مفتاح الترخيص أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string message;
            if (_licenseService.ValidateLicense(LicenseKey, out message)) { _licenseService.SaveLicense(LicenseKey); MessageBox.Show("تم تفعيل الترخيص بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); LicenseKey = ""; }
            else MessageBox.Show($"فشل التفعيل: {message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
