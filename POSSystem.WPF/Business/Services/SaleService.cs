

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Sale>> GetAllSalesAsync()
        {
            return await _unitOfWork.Sales.GetAllAsync();
        }

        public async Task<IEnumerable<Sale>> GetSalesWithItemsAsync()
        {
            return await _unitOfWork.Sales.GetSalesWithItemsAsync();
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            return await _unitOfWork.Sales.GetByIdAsync(id);
        }

        public async Task<Sale?> GetSaleWithItemsAsync(int id)
        {
            return await _unitOfWork.Sales.GetSaleWithItemsAsync(id);
        }

        public async Task<IEnumerable<Sale>> GetSalesByUserAsync(int userId)
        {
            return await _unitOfWork.Sales.GetSalesByUserAsync(userId);
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Sales.GetSalesByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Sale>> GetSalesByStatusAsync(SaleStatus status)
        {
            return await _unitOfWork.Sales.GetSalesByStatusAsync(status);
        }

        public async Task<IEnumerable<Sale>> GetTodaySalesAsync()
        {
            return await _unitOfWork.Sales.GetTodaySalesAsync();
        }

        public async Task<Sale> CreateSaleAsync(Sale sale)
        {
            if (!await ValidateSaleAsync(sale))
                throw new ArgumentException("Invalid sale data");

            // Generate unique invoice number
            sale.InvoiceNumber = await _unitOfWork.Sales.GenerateUniqueInvoiceNumberAsync();

            // Set timestamps
            var now = DateTime.Now;
            sale.CreatedAt = now;
            sale.UpdatedAt = now;

            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            return sale;
        }

        public async Task<Sale> UpdateSaleAsync(Sale sale)
        {
            if (!await ValidateSaleAsync(sale))
                throw new ArgumentException("Invalid sale data");

            var existingSale = await _unitOfWork.Sales.GetByIdAsync(sale.Id);
            if (existingSale == null)
                throw new ArgumentException("Sale not found");

            sale.UpdatedAt = DateTime.Now;
            _unitOfWork.ClearEntityTracking();
            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            return sale;
        }

        public async Task DeleteSaleAsync(int saleId)
        {
            int deleted = await _unitOfWork.GetContext().Sales
                .Where(s => s.Id == saleId)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                throw new ArgumentException("Sale not found");

            _unitOfWork.ClearEntityTracking();
        }

        public async Task<string> GenerateUniqueInvoiceNumberAsync()
        {
            return await _unitOfWork.Sales.GenerateUniqueInvoiceNumberAsync();
        }

        public Task<bool> ValidateSaleAsync(Sale sale)
        {
            if (sale == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(sale.InvoiceNumber)) return Task.FromResult(false);
            if (sale.UserId <= 0) return Task.FromResult(false);
            if (sale.SaleItems == null || !sale.SaleItems.Any()) return Task.FromResult(false);
            if (sale.FinalAmount <= 0) return Task.FromResult(false);

            // Validate sale items
            foreach (var item in sale.SaleItems)
            {
                if (item.ProductId <= 0) return Task.FromResult(false);
                if (item.Quantity <= 0) return Task.FromResult(false);
                if (item.UnitPrice <= 0) return Task.FromResult(false);
                if (item.TotalPrice <= 0) return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public async Task<decimal> GetTotalSalesByDateAsync(DateTime date)
        {
            return await _unitOfWork.Sales.GetTotalSalesByDateAsync(date);
        }

        public async Task<decimal> GetTotalSalesByMonthAsync(int year, int month)
        {
            return await _unitOfWork.Sales.GetTotalSalesByMonthAsync(year, month);
        }

        public async Task<bool> ProcessSaleAsync(Sale sale)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _unitOfWork.ClearEntityTracking();
                sale.Id = 0;
                if (sale.SaleItems != null)
                {
                    foreach (var item in sale.SaleItems) item.Id = 0;
                }
                
                // Create the sale
                var createdSale = await CreateSaleAsync(sale);
                
                // Update product stock
                var productService = ServiceLocator.ProductService;
                foreach (var item in sale.SaleItems!)
                {
                    bool success = await productService.DeductStockAsync(item.ProductId, item.Size, item.Quantity);
                    if (!success)
                    {
                        var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                        throw new InvalidOperationException($"Insufficient stock for product: {product?.Name ?? "Unknown"}");
                    }
                }

                // الحفظ النهائي لتعديلات المخزون
                await _unitOfWork.SaveChangesAsync();
            });
            return true;
        }

        private async Task BeginTransactionAsync()
        {
            await _unitOfWork.BeginTransactionAsync();
        }

        private async Task CommitTransactionAsync()
        {
            await _unitOfWork.CommitTransactionAsync();
        }

        private async Task RollbackTransactionAsync()
        {
            await _unitOfWork.RollbackTransactionAsync();
        }
    }
}
