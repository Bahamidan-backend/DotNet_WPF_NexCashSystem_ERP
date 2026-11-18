
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IAddonAndOrderItemService
    {
        Task<int> CreateToDbAddonAndOrderitem(AddonAndOrderItem addonAndOrderItem);
        Task<int> CreateToDbAddonAndOrderItemsBatchAsync(IEnumerable<AddonAndOrderItem> items);
    }
}
