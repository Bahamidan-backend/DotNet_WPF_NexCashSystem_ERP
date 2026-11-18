using System.Windows.Input;

namespace POSSystem.WPF.ViewModels.Kitchen
{
    public partial class KitchenManagementViewModel
    {
        public ICommand SaveCommand { get; private set; } = null!;
        public ICommand EditCommand { get; private set; } = null!;
        public ICommand DeleteCommand { get; private set; } = null!;
        public ICommand ClearFormCommand { get; private set; } = null!;

        public ICommand RefreshBatchesCommand { get; private set; } = null!;
        public ICommand FilterTodayCommand { get; private set; } = null!;
        public ICommand FilterThisWeekCommand { get; private set; } = null!;
        public ICommand FilterThisMonthCommand { get; private set; } = null!;
    }
}
