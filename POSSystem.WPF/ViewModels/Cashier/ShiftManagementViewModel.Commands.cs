using System.Windows.Input;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class ShiftManagementViewModel
    {
        public class ShiftManagementCommands
        {
            public ICommand SetShiftTypeCommand { get; }
            public ICommand StartShiftCommand { get; }
            public ICommand EndShiftCommand { get; }
            public ICommand CancelCommand { get; }
            public ICommand WithdrawCashCommand { get; }
            public ICommand ActionCommand { get; }

            public ShiftManagementCommands(ShiftManagementViewModel vm)
            {
                SetShiftTypeCommand = new RelayCommand<string>(vm.SetShiftType);
                StartShiftCommand = new RelayCommand(async () => await vm.StartShiftAsync());
                EndShiftCommand = new RelayCommand(async () => await vm.EndShiftAsync());
                CancelCommand = new RelayCommand(vm.Cancel);
                WithdrawCashCommand = new RelayCommand(async () => await vm.WithdrawCashAsync());
                ActionCommand = vm.CurrentShift == null ? StartShiftCommand : EndShiftCommand;
            }
        }
    }
}
