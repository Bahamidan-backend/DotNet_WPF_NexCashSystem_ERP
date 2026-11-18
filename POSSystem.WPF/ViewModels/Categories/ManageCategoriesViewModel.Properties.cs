using System.Collections.Generic;
using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels.Products;
using static POSSystem.WPF.Models.Category;

namespace POSSystem.WPF.ViewModels.Categories
{
    public partial class ManageCategoriesViewModel
    {
        private string _newCategoryName = "";
        private string _newCategoryDescription = "";
        private string _newCategoryColor = "#3498DB";
        private PortionSize _newCategoryportionSize = PortionSize.Single;
        private bool _newCategoryIsSub = false;
        private bool _isEditing = false;
        private int _editingCategoryId = 0;
        private Models.Kitchen? _selectedKitchen;
        private ObservableCollection<Models.Kitchen> _availableKitchens = new();

        public Models.Kitchen? SelectedKitchen
        {
            get => _selectedKitchen;
            set
            {
                _selectedKitchen = value;
                OnPropertyChanged(nameof(SelectedKitchen));
            }
        }

        public ObservableCollection<Models.Kitchen> AvailableKitchens
        {
            get => _availableKitchens;
            set
            {
                _availableKitchens = value;
                OnPropertyChanged(nameof(AvailableKitchens));
            }
        }

        public string NewCategoryName
        {
            get => _newCategoryName;
            set
            {
                _newCategoryName = value;
                OnPropertyChanged(nameof(NewCategoryName));
            }
        }

        public string NewCategoryDescription
        {
            get => _newCategoryDescription;
            set
            {
                _newCategoryDescription = value;
                OnPropertyChanged(nameof(NewCategoryDescription));
            }
        }

        public string NewCategoryColor
        {
            get => _newCategoryColor;
            set
            {
                _newCategoryColor = value;
                OnPropertyChanged(nameof(NewCategoryColor));
            }
        }

        public ObservableCollection<string> PredefinedColors { get; } = new()
        {
            "#3498DB", "#E74C3C", "#2ECC71", "#F1C40F", "#9B59B6", 
            "#E67E22", "#1ABC9C", "#34495E", "#D35400", "#C0392B"
        };

        public PortionSize NewCategoryportionSize
        {
            get => _newCategoryportionSize;
            set
            {
                _newCategoryportionSize = value;
                OnPropertyChanged(nameof(NewCategoryportionSize));
            }
        }

        public bool NewCategoryIsSub
        {
            get => _newCategoryIsSub;
            set
            {
                _newCategoryIsSub = value;
                OnPropertyChanged(nameof(NewCategoryIsSub));
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }

        public IEnumerable<dynamic> AvailableSizes 
        { 
            get
            {
                return POSSystem.WPF.Helpers.PortionSizeHelper.DisplayNames.Select(x => new { Name = x.Value, Value = x.Key }).ToList();
            }
        }

        private ICategoryService _categoryService;
        private IKitchenService _kitchenService;
        public ProductsViewModel? productsViewModel { get; set; }
        public AddonManagementViewModel? addonManagementViewModel { get; set; }
        public OrderItem _Orderitem;

        public ObservableCollection<Category> Categories { get; set; }
    }
}
