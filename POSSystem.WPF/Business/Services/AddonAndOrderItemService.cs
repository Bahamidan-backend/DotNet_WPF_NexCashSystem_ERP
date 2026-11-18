

namespace POSSystem.WPF.Business.Services
{
    public class AddonAndOrderItemService : IAddonAndOrderItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddonAndOrderItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        

        public async Task<int> CreateToDbAddonAndOrderitem(AddonAndOrderItem addonAndOrderItem)
        {
            await _unitOfWork.AddonsOrderItem.AddAsync(addonAndOrderItem);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> CreateToDbAddonAndOrderItemsBatchAsync(IEnumerable<AddonAndOrderItem> items)
        {
            if (items == null) return 0;
            var itemList = items as ICollection<AddonAndOrderItem> ?? items.ToList();
            if (itemList.Count == 0) return 0;

            await _unitOfWork.AddonsOrderItem.AddRangeAsync(itemList);
            return await _unitOfWork.SaveChangesAsync();
        }
    }
}
