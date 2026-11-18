using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.ViewModels.Sales
{
    public partial class SalesManagementViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly IReturnOrderService _returnOrderService;

        public SalesManagementCommands Commands { get; }
    }
}