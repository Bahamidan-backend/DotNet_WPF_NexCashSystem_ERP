using System;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.ViewModels.Orders
{
    public partial class OrdersViewModel
    {
        private readonly IOrderService _orderService;
        private readonly User _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly POSSystem.WPF.Business.Services.interfase.IShiftService _shiftService;
        private readonly System.Threading.SemaphoreSlim _dbSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        
        public OrdersCommands Commands { get; }
        public OrdersProberty ordersProberty { get; }
        public User CurrentUser => _currentUser;

        public event EventHandler<Order> OrderDetailsRequested;
        public event EventHandler<Order>? ReopenOrderInCashierRequested;
    }
}
