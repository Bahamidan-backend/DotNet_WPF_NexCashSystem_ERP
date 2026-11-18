using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.DTOs;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Kitchen
{
    public partial class KitchenManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public KitchenManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _kitchenService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IKitchenService>(sp);
            _batchService = ServiceLocator.KitchenCookingBatchService;
            KitchenProberty = new KitchenManagementProberty();

            SaveCommand = new RelayCommand(async () => await SaveKitchenAsync());
            EditCommand = new RelayCommand<Models.Kitchen>(kitchen => KitchenProberty.SetEditMode(kitchen));
            DeleteCommand = new RelayCommand<Models.Kitchen>(async kitchen => await DeleteKitchenAsync(kitchen));
            ClearFormCommand = new RelayCommand(() => KitchenProberty.ResetNewKitchen());

            RefreshBatchesCommand = new RelayCommand(async () => await LoadCookingBatchesAsync());
            FilterTodayCommand = new RelayCommand(() =>
            {
                FromDate = DateTime.Today;
                ToDate = DateTime.Today;
            });
            FilterThisWeekCommand = new RelayCommand(() =>
            {
                FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                ToDate = DateTime.Today;
            });
            FilterThisMonthCommand = new RelayCommand(() =>
            {
                FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                ToDate = DateTime.Today;
            });

            ServiceLocator.InventoryChanged += OnBatchesOrInventoryChanged;

            _ = InitializeKitchenDataAsync();
        }

        public void ApplySearch(string query)
        {
            KitchenProberty.SearchText = query ?? string.Empty;
            BatchSearchText = query ?? string.Empty;
        }

        private async Task InitializeKitchenDataAsync()
        {
            await LoadKitchensAsync();
            await LoadCookingBatchesAsync();
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            ServiceLocator.InventoryChanged -= OnBatchesOrInventoryChanged;
            base.Dispose();
        }

        private void OnBatchesOrInventoryChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(async () =>
            {
                await LoadCookingBatchesAsync();
            });
        }

        public async Task LoadKitchensAsync()
        {
            try
            {
                var kitchens = await _kitchenService.GetAllKitchensAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    KitchenProberty._allKitchens.Clear();
                    foreach (var kitchen in kitchens)
                    {
                        KitchenProberty._allKitchens.Add(kitchen);
                    }
                    KitchenProberty.FilterKitchens();
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في تحميل المطابخ: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task LoadCookingBatchesAsync()
        {
            try
            {
                var batches = await _batchService.GetBatchesByDateAsync(FromDate, ToDate);

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    AllBatches.Clear();
                    decimal totalPortions = 0m;
                    decimal totalSurplus = 0m;
                    decimal totalDeficit = 0m;
                    decimal totalCost = 0m;

                    foreach (var b in batches)
                    {
                        string unitName = PortionUnitHelper.GetDisplayName(b.PortionUnit);
                        decimal variance = b.ActualPortions - b.ExpectedPortions;

                        totalPortions += b.ActualPortions;
                        if (variance > 0) totalSurplus += variance;
                        else if (variance < 0) totalDeficit += Math.Abs(variance);
                        totalCost += b.TotalRawCost;

                        AllBatches.Add(new KitchenCookingBatchDisplayItem
                        {
                            Id = b.Id,
                            BatchNumber = b.BatchNumber,
                            ProductName = b.Product?.Name ?? "وجبة محذوفة",
                            CookedAt = b.CookedAt,
                            MainQty = b.MainIngredientQuantity,
                            UnitName = unitName,
                            ExpectedPortions = b.ExpectedPortions,
                            ActualPortions = b.ActualPortions,
                            TotalRawCost = b.TotalRawCost,
                            CookedByUserName = b.CookedByUser?.Name ?? "كاشير / غير محدد",
                            Notes = b.Notes ?? string.Empty
                        });
                    }

                    TotalBatchesCount = AllBatches.Count;
                    TotalActualPortions = totalPortions;
                    TotalSurplusPortions = totalSurplus;
                    TotalDeficitPortions = totalDeficit;
                    TotalBatchRawCost = totalCost;

                    FilterBatches();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KitchenManagement] Error loading batches: {ex.Message}");
            }
        }

        public void FilterBatches()
        {
            FilteredBatches.Clear();
            var search = BatchSearchText?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrWhiteSpace(search)
                ? AllBatches
                : AllBatches.Where(b =>
                    (b.ProductName != null && b.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (b.BatchNumber != null && b.BatchNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (b.CookedByUserName != null && b.CookedByUserName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (b.Notes != null && b.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)));

            foreach (var item in filtered)
            {
                FilteredBatches.Add(item);
            }
        }

        private async Task SaveKitchenAsync()
        {
            if (string.IsNullOrWhiteSpace(KitchenProberty.NewKitchen.Name))
            {
                MessageBox.Show("يرجى إدخال اسم المطبخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(KitchenProberty.NewKitchen.PrinterName))
            {
                MessageBox.Show("يرجى إدخال اسم الطابعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (KitchenProberty.IsEditMode)
                {
                    var existingKitchen = await _kitchenService.GetKitchenByIdAsync(KitchenProberty.NewKitchen.Id);
                    if (existingKitchen != null)
                    {
                        existingKitchen.Name = KitchenProberty.NewKitchen.Name;
                        existingKitchen.Description = KitchenProberty.NewKitchen.Description;
                        existingKitchen.PrinterName = KitchenProberty.NewKitchen.PrinterName;
                        existingKitchen.IsPrinterEnabled = KitchenProberty.NewKitchen.IsPrinterEnabled;
                        existingKitchen.IsActive = KitchenProberty.NewKitchen.IsActive;
                        existingKitchen.UpdatedAt = DateTime.Now;

                        await _kitchenService.UpdateKitchenAsync(existingKitchen);
                        MessageBox.Show("تم تحديث المطبخ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("المطبخ غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    await _kitchenService.CreateKitchenAsync(KitchenProberty.NewKitchen);
                    MessageBox.Show("تم إضافة المطبخ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                KitchenProberty.ResetNewKitchen();
                await LoadKitchensAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ المطبخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteKitchenAsync(Models.Kitchen kitchen)
        {
            if (kitchen == null) return;

            var result = MessageBox.Show($"هل أنت متأكد من حذف المطبخ '{kitchen.Name}'؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _kitchenService.DeleteKitchenAsync(kitchen);
                    await LoadKitchensAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف المطبخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
