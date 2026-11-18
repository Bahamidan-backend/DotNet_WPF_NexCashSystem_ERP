
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;

namespace POSSystem.WPF.Data.Repositories
{
    public class AnimalBatchRepository : Repository<AnimalBatch>, IAnimalBatchRepository
    {
        public AnimalBatchRepository(POSDbContext context) : base(context)
        {
        }

        public IEnumerable<AnimalBatch> GetAllBatchesWithComponents()
        {
            // FIX: Added AsNoTracking — sync read-only query.
            return _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .OrderByDescending(ab => ab.BatchDate)
                .ToList();
        }

        public async Task<IEnumerable<AnimalBatch>> GetAllBatchesWithComponentsAsync()
        {
            return await _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .OrderByDescending(ab => ab.BatchDate)
                .ToListAsync();
        }

        public IEnumerable<AnimalBatch> GetBatchesByAnimalType(AnimalType type)
        {
            // FIX: Added AsNoTracking — sync read-only query.
            return _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .Where(ab => ab.Type == type)
                .OrderByDescending(ab => ab.BatchDate)
                .ToList();
        }

        public async Task<IEnumerable<AnimalBatch>> GetBatchesByAnimalTypeAsync(AnimalType type)
        {
            return await _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .Where(ab => ab.Type == type)
                .OrderByDescending(ab => ab.BatchDate)
                .ToListAsync();
        }

        public IEnumerable<AnimalBatch> GetFullAnimalsOnly()
        {
            // Direct translation to SQL using database columns instead of non-translatable C# computed property IsFullAnimal
            return _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .Where(ab => ab.HeadCount > 0 && ab.BackWeight > 0 && ab.LiverWeight > 0 && ab.RemainingWeight > 0)
                .OrderByDescending(ab => ab.BatchDate)
                .ToList();
        }

        public async Task<IEnumerable<AnimalBatch>> GetFullAnimalsOnlyAsync()
        {
            // Direct translation to SQL using database columns instead of non-translatable C# computed property IsFullAnimal
            return await _context.AnimalBatches
                .AsNoTracking()
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .Where(ab => ab.HeadCount > 0 && ab.BackWeight > 0 && ab.LiverWeight > 0 && ab.RemainingWeight > 0)
                .OrderByDescending(ab => ab.BatchDate)
                .ToListAsync();
        }

        public AnimalBatch GetBatchWithComponents(int id)
        {
            return _context.AnimalBatches
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .FirstOrDefault(ab => ab.Id == id);
        }

        public async Task<AnimalBatch?> GetBatchWithComponentsAsync(int id)
        {
            return await _context.AnimalBatches
                .Include(ab => ab.Warehouse)
                .Include(ab => ab.Supplier)
                .Include(ab => ab.Components)
                .FirstOrDefaultAsync(ab => ab.Id == id);
        }

        public void UpdateBatchComponents(BatchComponent component)
        {
            component.UpdatedAt = DateTime.Now;
            var local = _context.BatchComponents.Local.FirstOrDefault(c => c.Id == component.Id);
            if (local != null && !ReferenceEquals(local, component))
            {
                local.Quantity = component.Quantity;
                local.UpdatedAt = component.UpdatedAt;
            }
            else
            {
                _context.BatchComponents.Update(component);
            }
        }

        public void AddBatchWithComponents(AnimalBatch batch)
        {
            _context.AnimalBatches.Add(batch);
        }

        public override void Remove(AnimalBatch entity)
        {
            // Explicitly remove components to avoid FK constraints if cascade is not enabled
            if (entity.Components != null && entity.Components.Any())
            {
                _context.BatchComponents.RemoveRange(entity.Components);
            }
            base.Remove(entity);
        }
    }
}
