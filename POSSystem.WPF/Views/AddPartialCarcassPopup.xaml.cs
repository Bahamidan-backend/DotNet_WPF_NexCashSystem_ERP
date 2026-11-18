using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views
{
    public partial class AddPartialCarcassPopup : Window, INotifyPropertyChanged
    {
        private readonly IAnimalBatchService _batchService;
        private readonly IUnitOfWork _unitOfWork;

        private int _selectedTargetWarehouseId;
        private AnimalType _selectedAnimalType;
        private int? _selectedSupplierId;
        private decimal _purchaseCost;
        private bool _isWeightMode;

        private bool _includeHead;
        private decimal _headCount;
        private bool _includeLiver;
        private decimal _liverQty;
        private bool _includeBack;
        private decimal _backQty;
        private bool _includeMeat;
        private decimal _meatQty;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddPartialCarcassPopup(IAnimalBatchService batchService, IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            _batchService = batchService;
            _unitOfWork = unitOfWork;
            DataContext = this;

            AnimalTypes = new ObservableCollection<dynamic>
            {
                new { Id = (int)AnimalType.Sheep, Name = "خروف" },
                new { Id = (int)AnimalType.Cow, Name = "بقرة" },
                new { Id = (int)AnimalType.Camel, Name = "جمل" }
            };

            SelectedAnimalType = AnimalType.Sheep;
            
            LoadData();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => Close());
        }

        private async void LoadData()
        {
            try
            {
                var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
                Warehouses = new ObservableCollection<Warehouse>(warehouses);
                OnPropertyChanged(nameof(Warehouses));

                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
                Suppliers = new ObservableCollection<Supplier>(suppliers);
                OnPropertyChanged(nameof(Suppliers));

                if (Warehouses.Any())
                {
                    SelectedTargetWarehouseId = Warehouses.First().Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }

        public ObservableCollection<Warehouse> Warehouses { get; private set; }
        public ObservableCollection<dynamic> AnimalTypes { get; private set; }
        public ObservableCollection<Supplier> Suppliers { get; private set; }

        public int SelectedTargetWarehouseId
        {
            get => _selectedTargetWarehouseId;
            set { _selectedTargetWarehouseId = value; OnPropertyChanged(); }
        }

        public AnimalType SelectedAnimalType
        {
            get => _selectedAnimalType;
            set { _selectedAnimalType = value; OnPropertyChanged(); }
        }

        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set { _selectedSupplierId = value; OnPropertyChanged(); }
        }

        //public decimal PurchaseCost
        //{
        //    get => _purchaseCost;
        //    set { _purchaseCost = value; OnPropertyChanged(); }
        //}

        public bool IsWeightMode
        {
            get => _isWeightMode;
            set 
            { 
                _isWeightMode = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(LiverLabel));
                OnPropertyChanged(nameof(BackLabel));
                OnPropertyChanged(nameof(MeatLabel));
            }
        }

        public bool IncludeHead { get => _includeHead; set { _includeHead = value; OnPropertyChanged(); } }
        public decimal HeadCount { get => _headCount; set { _headCount = value; OnPropertyChanged(); } }

        public bool IncludeLiver { get => _includeLiver; set { _includeLiver = value; OnPropertyChanged(); } }
        public decimal LiverQty { get => _liverQty; set { _liverQty = value; OnPropertyChanged(); } }

        public bool IncludeBack { get => _includeBack; set { _includeBack = value; OnPropertyChanged(); } }
        public decimal BackQty { get => _backQty; set { _backQty = value; OnPropertyChanged(); } }

        public bool IncludeMeat { get => _includeMeat; set { _includeMeat = value; OnPropertyChanged(); } }
        public decimal MeatQty { get => _meatQty; set { _meatQty = value; OnPropertyChanged(); } }

        public string LiverLabel => IsWeightMode ? "الكبدة (كجم)" : "الكبدة (حبة)";
        public string BackLabel => IsWeightMode ? "الظهر (كجم)" : "الظهر (حبة)";
        public string MeatLabel => IsWeightMode ? "اللحم (كجم)" : "اللحم (ثمن)";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (SelectedTargetWarehouseId == 0)
            {
                MessageBox.Show("يرجى اختيار مخزن الإيداع");
                return;
            }

            if (!IncludeHead && !IncludeLiver && !IncludeBack && !IncludeMeat)
            {
                MessageBox.Show("يرجى اختيار جزء واحد على الأقل لإضافته");
                return;
            }

            try
            {
                var success = await _batchService.AddPartialCarcassAsync(
                    SelectedTargetWarehouseId,
                    SelectedAnimalType,
                    IncludeHead ? HeadCount : 0,
                    IncludeLiver ? LiverQty : 0,
                    IncludeBack ? BackQty : 0,
                    IncludeMeat ? MeatQty : 0,
                    IsWeightMode,
                    //PurchaseCost,
                    SelectedSupplierId
                );

                if (success)
                {
                    MessageBox.Show("تمت الإضافة بنجاح");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("فشل في إضافة الأجزاء، يرجى المحاولة مرة أخرى");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
