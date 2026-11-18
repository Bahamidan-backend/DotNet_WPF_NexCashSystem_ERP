using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Expenses;

namespace POSSystem.WPF.Shered.Commands
{
    public class ExpenseManagementCommands
    {
        public ICommand LoadExpensesCommand { get; }
        public ICommand LoadVouchersCommand { get; }

        public ICommand AddExpenseCommand { get; }
        public ICommand EditExpenseCommand { get; }
        public ICommand DeleteExpenseCommand { get; }

        public ICommand AddVoucherCommand { get; }
        public ICommand EditVoucherCommand { get; }
        public ICommand DeleteVoucherCommand { get; }

        public ICommand RefreshCommand { get; }
        public ICommand ShowExpensesCommand { get; }
        public ICommand ShowAccountsCommand { get; }
        public ICommand ShowVouchersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ClearVoucherFiltersCommand { get; }

        public ICommand SelectExpenseAccountCommand { get; }
        public ICommand AddNewAccountCommand { get; }
        public ICommand AddExpenseForSelectedAccountCommand { get; }
        public ICommand StartEditAccountCommand { get; }
        public ICommand SaveEditAccountCommand { get; }
        public ICommand CancelEditAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }

        public ExpenseManagementCommands(ExpenseManagementViewModel vm)
        {
            LoadExpensesCommand =
                new RelayCommand(async () => await vm.LoadExpensesAsync());

            LoadVouchersCommand =
                new RelayCommand(async () => await vm.LoadVouchersAsync());

            AddExpenseCommand =
                new RelayCommand(() => vm.AddExpense());

            EditExpenseCommand =
                new RelayCommand<Expense>(
                    (e) => vm.EditExpense(e));

            DeleteExpenseCommand =
                new RelayCommand<Expense>(
                    async (e) => await vm.DeleteExpenseAsync(e));

            AddVoucherCommand =
                new RelayCommand(() => vm.AddVoucher());

            EditVoucherCommand =
                new RelayCommand<ReceiptVoucher>(
                    (v) => vm.EditVoucher(v));

            DeleteVoucherCommand =
                new RelayCommand<ReceiptVoucher>(
                    async (v) => await vm.DeleteVoucherAsync(v));

            RefreshCommand =
                new RelayCommand(async () => await vm.RefreshAsync());

            ShowExpensesCommand =
                new RelayCommand(() => vm.ShowExpenses());

            ShowAccountsCommand =
                new RelayCommand(() => vm.ShowAccounts());

            ShowVouchersCommand =
                new RelayCommand(() => vm.ShowVouchers());

            ClearFiltersCommand =
                new RelayCommand(() => vm.ClearFilters());

            ClearVoucherFiltersCommand =
                new RelayCommand(() => vm.ClearVoucherFilters());

            SelectExpenseAccountCommand =
                new RelayCommand<ExpenseAccountSummaryItem>(
                    (acc) => vm.SelectExpenseAccount(acc));

            AddNewAccountCommand =
                new RelayCommand(() => vm.AddNewExpenseAccount());

            AddExpenseForSelectedAccountCommand =
                new RelayCommand<ExpenseAccountSummaryItem>(
                    (acc) => vm.AddExpenseForAccount(acc));

            StartEditAccountCommand =
                new RelayCommand<ExpenseAccountSummaryItem>(
                    (acc) => vm.StartEditExpenseAccount(acc));

            SaveEditAccountCommand =
                new RelayCommand(async () => await vm.SaveEditExpenseAccountAsync());

            CancelEditAccountCommand =
                new RelayCommand(() => vm.CancelEditExpenseAccount());

            DeleteAccountCommand =
                new RelayCommand<ExpenseAccountSummaryItem>(
                    async (acc) => await vm.DeleteExpenseAccountAsync(acc));
        }
    }
}
