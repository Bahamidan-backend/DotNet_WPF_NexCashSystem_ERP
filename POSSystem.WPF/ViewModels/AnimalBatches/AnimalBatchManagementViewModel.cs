using POSSystem.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.Views;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AnimalBatchManagementViewModel : BaseViewModel
    {
        public AnimalBatchManagementViewModel(IAnimalBatchService batchService, IUnitOfWork unitOfWork)
        {
            _batchService = batchService;
            _unitOfWork = unitOfWork;
            _batches = new ObservableCollection<AnimalBatch>();
            
            LoadBatchesCommand = new RelayCommand(async () => await LoadBatchesAsync());
            AddBatchCommand = new RelayCommand(AddBatch);

            ShowDetailsCommand = new RelayCommand<object>(p => ShowDetails(p));
            RefreshCommand = new RelayCommand(async () => await LoadBatchesAsync());
            BreakdownFromInventoryCommand = new RelayCommand(OpenBreakdownFromInventory);
            AddPartialCarcassCommand = new RelayCommand(OpenAddPartialCarcassPopup);
            DeleteBatchCommand = new RelayCommand<object>(async p => await DeleteBatchAsync(p));

            LoadBatchesAsync().SafeFireAndForget();
        }

        private async Task LoadBatchesAsync()
        {
            try
            {
                var batches = await _batchService.GetAllBatchesAsync();
                _allBatches = batches.OrderByDescending(b => b.BatchDate).ToList();
                
                FilterBatches();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterBatches()
        {
            var filtered = _allBatches.AsEnumerable();

            if (SelectedAnimalTypeId > 0)
            {
                filtered = filtered.Where(b => (int)b.Type == SelectedAnimalTypeId);
            }

            if (ShowOnlyFullAnimals)
            {
                filtered = filtered.Where(b => b.IsFullAnimal);
            }

            var result = filtered.ToList();

            Batches.Clear();
            foreach (var item in result)
            {
                Batches.Add(item);
            }

            OnPropertyChanged(nameof(TotalBatches));
            OnPropertyChanged(nameof(FullAnimalsCount));
            OnPropertyChanged(nameof(BrokenBatchesCount));
        }

        private void OpenBreakdownFromInventory()
        {
            var breakdownPopup = new BreakdownInventoryPopup(_batchService, _unitOfWork);
            
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != null && activeWindow != breakdownPopup)
            {
                breakdownPopup.Owner = activeWindow;
            }

            breakdownPopup.ShowDialog();
            
            LoadBatchesAsync().SafeFireAndForget();
            POSSystem.WPF.Business.ServiceLocator.NotifyInventoryChanged();
        }

        private void OpenAddPartialCarcassPopup()
        {
            var addPartialPopup = new AddPartialCarcassPopup(_batchService, _unitOfWork);
            
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != null && activeWindow != addPartialPopup)
            {
                addPartialPopup.Owner = activeWindow;
            }

            addPartialPopup.ShowDialog();
            
            LoadBatchesAsync().SafeFireAndForget();
            POSSystem.WPF.Business.ServiceLocator.NotifyInventoryChanged();
        }

        private void AddBatch()
        {
            var addBatchPopup = new AddAnimalBatchPopup();
            
            var adminWindow = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
            if (adminWindow != null)
            {
                adminWindow.ShowOverlay(addBatchPopup);
            }
        }

        private void ShowDetails(object parameter)
        {
            if (parameter is AnimalBatch batch)
            {
                var details = $"""
                    تفاصيل الذبيحة
                    
                    النوع: {GetTypeString(batch.Type)}
                    التاريخ: {batch.BatchDate:yyyy-MM-dd HH:mm}
                    
                    المكونات:
                    - الرأس: {batch.HeadCount}
                    - الظهر: {batch.BackWeight} كجم
                    - الكبدة: {batch.LiverWeight} كجم
                    - اللحم المتبقي: {batch.RemainingWeight} كجم
                    
                    الحالة: {(batch.IsFullAnimal ? "كاملة ✓" : "مفككة ✗")}
                    """;

                MessageBox.Show(details, "تفاصيل الذبيحة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task DeleteBatchAsync(object parameter)
        {
            if (parameter is not AnimalBatch batch) return;

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف هذه الذبيحة؟\nالنوع: {GetTypeString(batch.Type)}\nالتاريخ: {batch.BatchDate:yyyy-MM-dd}",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var success = await _batchService.DeleteBatchAsync(batch.Id);
                if (success)
                {
                    MessageBox.Show("تم حذف الذبيحة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadBatchesAsync();
                }
                else
                {
                    MessageBox.Show("فشل في حذف الذبيحة. قد تكون مرتبطة بسجلات أخرى.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetTypeString(AnimalType type)
        {
            return type switch
            {
                AnimalType.Sheep => "خروف",
                AnimalType.Cow => "بقرة",
                AnimalType.Camel => "جمل",
                _ => type.ToString()
            };
        }
    }
}
