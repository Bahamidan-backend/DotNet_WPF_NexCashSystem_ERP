using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class DiscountsProberty:BaseModel
    {




        public  ObservableCollection<Discount> _allDiscounts;
        public  ObservableCollection<Discount> _filteredDiscounts;
        public   string _searchText = "";
        public  bool _allStatusSelected = true;
        public  bool _activeSelected = false;
        public  bool _expiredSelected = false;
        public  bool _pendingSelected = false;
        public  bool _inactiveSelected = false;
        private decimal _bulkDiscountPercentage;


        public DiscountsProberty()
        {
            _allDiscounts = new ObservableCollection<Discount>();
            _filteredDiscounts = new ObservableCollection<Discount>();
        }
        public  ObservableCollection<Discount> Discounts => _filteredDiscounts;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterDiscounts();
            }
        }

        public bool AllStatusSelected
        {
            get => _allStatusSelected;
            set
            {
                _allStatusSelected = value;
                OnPropertyChanged();
                FilterDiscounts();
            }
        }

        public bool ActiveSelected
        {
            get => _activeSelected;
            set
            {
                _activeSelected = value;
                if (value) _allStatusSelected = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllStatusSelected));
                FilterDiscounts();
            }
        }

        public bool ExpiredSelected
        {
            get => _expiredSelected;
            set
            {
                _expiredSelected = value;
                if (value) _allStatusSelected = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllStatusSelected));
                FilterDiscounts();
            }
        }

        public bool PendingSelected
        {
            get => _pendingSelected;
            set
            {
                _pendingSelected = value;
                if (value) _allStatusSelected = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllStatusSelected));
                FilterDiscounts();
            }
        }

        public bool InactiveSelected
        {
            get => _inactiveSelected;
            set
            {
                _inactiveSelected = value;
                if (value) _allStatusSelected = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AllStatusSelected));
                FilterDiscounts();
            }
        }

        public decimal BulkDiscountPercentage
        {
            get => _bulkDiscountPercentage;
            set
            {
                _bulkDiscountPercentage = value;
                OnPropertyChanged();
            }
        }






        public  ObservableCollection<Discount> FilterDiscounts()
        {
            _filteredDiscounts.Clear();

            var filtered = _allDiscounts.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(d =>
                    (d.Name != null && d.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (d.Code != null && d.Code.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (d.Description != null && d.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
            }

            // Filter by status
            if (!_allStatusSelected)
            {
                var statusFilters = new List<string>();
                if (_activeSelected) statusFilters.Add("Active");
                if (_expiredSelected) statusFilters.Add("Expired");
                if (_pendingSelected) statusFilters.Add("Pending");
                if (_inactiveSelected) statusFilters.Add("Inactive");

                if (statusFilters.Any())
                {
                    filtered = filtered.Where(d => statusFilters.Contains(d.Status));
                }
            }

            foreach (var discount in filtered.OrderBy(d => d.Name))
            {
                _filteredDiscounts.Add(discount);
            }
            return Discounts;

        }

    }
}
