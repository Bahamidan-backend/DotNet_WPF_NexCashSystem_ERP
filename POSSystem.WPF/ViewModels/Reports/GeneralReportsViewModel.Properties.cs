using POSSystem.WPF.Helpers;
using System;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Reports
{
    public partial class GeneralReportsViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IExpenseService _expenseService;
        private readonly IEmployeeService _employeeService;
        private readonly ICustomerDebtService _customerDebtService;
        private readonly IPurchaseService _purchaseService;
        private readonly IOrderService _orderService;
        private readonly IShiftService _shiftService;
        private readonly ICashBoxTransactionService _cashBoxTransactionService;
        private readonly IWarehouseService _warehouseService;
        private readonly IReportService _reportService;
        private readonly IFinancialEngineService _financialEngineService;
        private readonly IUnitOfWork _unitOfWork;

        private DateTime _reportDate = DateTime.Today;
        private ReportPeriodType _periodType = ReportPeriodType.Daily;

        public GeneralReportProberty generalReportProberty { get; }

        public DateTime ReportDate
        {
            get => _reportDate;
            set
            {
                if (SetProperty(ref _reportDate, value))
                    GenerateReportAsync().SafeFireAndForget();
            }
        }

        public ReportPeriodType PeriodType
        {
            get => _periodType;
            set
            {
                if (SetProperty(ref _periodType, value))
                    GenerateReportAsync().SafeFireAndForget();
            }
        }
    }
}
