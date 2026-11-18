using System.Collections.ObjectModel;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Shered.Proberty
{
    public class KitchenManagementProberty : BaseViewModel
    {
        public ObservableCollection<Kitchen> _allKitchens = new();
        private ObservableCollection<Kitchen> _filteredKitchens = new();

        public ObservableCollection<Kitchen> FilteredKitchens
        {
            get => _filteredKitchens;
            set => SetProperty(ref _filteredKitchens, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterKitchens();
                }
            }
        }

        private Kitchen? _selectedKitchen;
        public Kitchen? SelectedKitchen
        {
            get => _selectedKitchen;
            set => SetProperty(ref _selectedKitchen, value);
        }

        private Kitchen _newKitchen = new() { IsActive = true };
        public Kitchen NewKitchen
        {
            get => _newKitchen;
            set => SetProperty(ref _newKitchen, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public void FilterKitchens()
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allKitchens
                : new ObservableCollection<Kitchen>(_allKitchens.Where(k => 
                    (k.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (k.PrinterName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)));

            FilteredKitchens = new ObservableCollection<Kitchen>(filtered);
        }

        public void ResetNewKitchen()
        {
            NewKitchen = new Kitchen { IsActive = true };
            IsEditMode = false;
        }

        public void SetEditMode(Kitchen kitchen)
        {
            NewKitchen = new Kitchen
            {
                Id = kitchen.Id,
                Name = kitchen.Name,
                Description = kitchen.Description,
                PrinterName = kitchen.PrinterName,
                IsPrinterEnabled = kitchen.IsPrinterEnabled,
                IsActive = kitchen.IsActive,
                CreatedAt = kitchen.CreatedAt
            };
            IsEditMode = true;
        }
    }
}
