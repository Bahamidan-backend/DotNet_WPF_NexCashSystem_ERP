
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IRepository<T> where T : class
    {
        // Get operations
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate);
        Task<PagedResult<T>> FindPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        // Add operations
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        // Update operations
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        // Remove operations
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task RemoveByIdAsync(int id);

        // Include operations for related entities
        IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);
        IQueryable<T> IncludeMultiple(params Expression<Func<T, object>>[] includes);
    }
}
