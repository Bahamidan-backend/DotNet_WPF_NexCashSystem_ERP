using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.ViewModels.Expenses
{
    public partial class ExpenseManagementViewModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IReceiptVoucherService _receiptVoucherService;
        
        private Expense? _selectedExpense;
        private ReceiptVoucher? _selectedVoucher;
        public ExpenseManagementProberty expenseManagementProberty { get; }

        public Expense? SelectedExpense
        {
            get => _selectedExpense;
            set
            {
                if (SetProperty(ref _selectedExpense, value))
                {
                    ((RelayCommand<Expense>)Commands.EditExpenseCommand).RaiseCanExecuteChanged();
                    ((RelayCommand<Expense>)Commands.DeleteExpenseCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ReceiptVoucher? SelectedVoucher
        {
            get => _selectedVoucher;
            set
            {
                if (SetProperty(ref _selectedVoucher, value))
                {
                    ((RelayCommand<ReceiptVoucher>)Commands.EditVoucherCommand).RaiseCanExecuteChanged();
                    ((RelayCommand<ReceiptVoucher>)Commands.DeleteVoucherCommand).RaiseCanExecuteChanged();
                }
            }
        }
    }
}
