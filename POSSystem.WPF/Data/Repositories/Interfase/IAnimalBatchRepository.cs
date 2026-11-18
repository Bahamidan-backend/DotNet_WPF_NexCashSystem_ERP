
using POSSystem.WPF.Data.Repositories.Interfase;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IAnimalBatchRepository : IRepository<AnimalBatch>
    {
        IEnumerable<AnimalBatch> GetAllBatchesWithComponents();
        Task<IEnumerable<AnimalBatch>> GetAllBatchesWithComponentsAsync();
        IEnumerable<AnimalBatch> GetBatchesByAnimalType(AnimalType type);
        Task<IEnumerable<AnimalBatch>> GetBatchesByAnimalTypeAsync(AnimalType type);
        IEnumerable<AnimalBatch> GetFullAnimalsOnly();
        Task<IEnumerable<AnimalBatch>> GetFullAnimalsOnlyAsync();
        AnimalBatch GetBatchWithComponents(int id);
        Task<AnimalBatch?> GetBatchWithComponentsAsync(int id);
        void UpdateBatchComponents(BatchComponent component);
        void AddBatchWithComponents(AnimalBatch batch);
    }
}
