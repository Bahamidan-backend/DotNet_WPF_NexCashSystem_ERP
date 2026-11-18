using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using System.Windows.Input;

namespace POSSystem.WPF.ViewModels.Addons
{
    public partial class AddonManagementViewModel
    {
        private readonly IAddonService _addonService;
        private readonly ICategoryService _categoryService;

        public AddonManagementProberty addonManagement { get; }

        public OrderItem _orderItem;
        public AddonProberty AddonVM { get; set; }
        public EditAddonProberty EditAddonVM { get; set; }

        private ICommand? _addAddonCommand;
        private ICommand? _editAddonCommand;
        private ICommand? _deleteAddonCommand;
        private ICommand? _refreshCommand;
    }
}
