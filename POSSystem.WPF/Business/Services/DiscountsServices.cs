
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    class DiscountsServices : IDiscountsServices
    {
        private readonly IUnitOfWork _unitOfWork;

        public DiscountsServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> AddDiscounts(Discount discount)
        {
            _unitOfWork.ClearEntityTracking();
            var context = _unitOfWork.GetContext();
            discount.Code = string.IsNullOrWhiteSpace(discount.Code)
                ? $"DISC-{DateTime.Now:yyyyMMddHHmmss}"
                : discount.Code.Trim();

            if (await context.Discounts.AsNoTracking().AnyAsync(d => d.Code.ToLower() == discount.Code.ToLower()))
                throw new System.InvalidOperationException($"كود الخصم ({discount.Code}) مستخدم مسبقاً، يرجى اختيار كود آخر.");

            await _unitOfWork.Discounts.AddAsync(discount);
            var res = await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyDiscountsChanged();
            return res;
        }

        public async Task<int> DeleteDiscounts(Discount discount)
        {
            int deletedRows = await _unitOfWork.GetContext().Discounts
                .Where(d => d.Id == discount.Id)
                .ExecuteDeleteAsync();

            _unitOfWork.ClearEntityTracking();
            ServiceLocator.NotifyDiscountsChanged();
            return deletedRows;
        }

        public async Task<IEnumerable<Discount>> GetAllDiscounts()
        {
            return await _unitOfWork.Discounts.GetAllAsync();
        }

        public async Task<int> UpdateDiscounts(Discount discount)
        {
            _unitOfWork.ClearEntityTracking();
            var context = _unitOfWork.GetContext();
            discount.Code = string.IsNullOrWhiteSpace(discount.Code)
                ? $"DISC-{discount.Id}-{DateTime.Now:HHmmss}"
                : discount.Code.Trim();

            if (await context.Discounts.AsNoTracking().AnyAsync(d => d.Id != discount.Id && d.Code.ToLower() == discount.Code.ToLower()))
                throw new System.InvalidOperationException($"كود الخصم ({discount.Code}) مستخدم مسبقاً لخصم آخر.");

            _unitOfWork.Discounts.Update(discount);
            var res = await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyDiscountsChanged();
            return res;
        }
    }
}
