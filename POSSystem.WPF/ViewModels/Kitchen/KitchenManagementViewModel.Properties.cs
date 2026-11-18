using System;
using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models.DTOs;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Kitchen
{
    public partial class KitchenManagementViewModel
    {
        private readonly IKitchenService _kitchenService;
        private readonly IKitchenCookingBatchService _batchService;
        public KitchenManagementProberty KitchenProberty { get; }

        public ObservableCollection<KitchenCookingBatchDisplayItem> AllBatches { get; } = new();
        public ObservableCollection<KitchenCookingBatchDisplayItem> FilteredBatches { get; } = new();

        private DateTime _fromDate = DateTime.Today;
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                    _ = LoadCookingBatchesAsync();
            }
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                    _ = LoadCookingBatchesAsync();
            }
        }

        private string _batchSearchText = string.Empty;
        public string BatchSearchText
        {
            get => _batchSearchText;
            set
            {
                if (SetProperty(ref _batchSearchText, value))
                    FilterBatches();
            }
        }

        private int _totalBatchesCount;
        public int TotalBatchesCount
        {
            get => _totalBatchesCount;
            set => SetProperty(ref _totalBatchesCount, value);
        }

        private decimal _totalActualPortions;
        public decimal TotalActualPortions
        {
            get => _totalActualPortions;
            set => SetProperty(ref _totalActualPortions, value);
        }

        private decimal _totalSurplusPortions;
        public decimal TotalSurplusPortions
        {
            get => _totalSurplusPortions;
            set => SetProperty(ref _totalSurplusPortions, value);
        }

        private decimal _totalDeficitPortions;
        public decimal TotalDeficitPortions
        {
            get => _totalDeficitPortions;
            set => SetProperty(ref _totalDeficitPortions, value);
        }

        private decimal _totalBatchRawCost;
        public decimal TotalBatchRawCost
        {
            get => _totalBatchRawCost;
            set => SetProperty(ref _totalBatchRawCost, value);
        }
    }
}
