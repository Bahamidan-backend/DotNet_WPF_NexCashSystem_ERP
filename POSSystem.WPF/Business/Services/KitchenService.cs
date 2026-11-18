using System.Linq;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class KitchenService : IKitchenService
    {
        private readonly IUnitOfWork _unitOfWork;

        public KitchenService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Kitchen>> GetAllKitchensAsync()
        {
            return await _unitOfWork.Kitchens.GetAllAsync();
        }

        public async Task<Kitchen?> GetKitchenByIdAsync(int id)
        {
            return await _unitOfWork.Kitchens.GetByIdAsync(id);
        }

        public async Task CreateKitchenAsync(Kitchen kitchen)
        {
            await _unitOfWork.Kitchens.AddAsync(kitchen);
            await _unitOfWork.SaveChangesAsync();
        }

        public Task UpdateKitchenAsync(Kitchen kitchen)
        {
            _unitOfWork.Kitchens.Update(kitchen);
            return _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteKitchenAsync(Kitchen kitchen)
        {
            await _unitOfWork.GetContext().Kitchens
                .Where(k => k.Id == kitchen.Id)
                .ExecuteDeleteAsync();
            _unitOfWork.ClearEntityTracking();
        }
    }
}
