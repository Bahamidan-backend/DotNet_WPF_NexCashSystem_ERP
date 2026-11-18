

namespace POSSystem.WPF.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly POSDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(POSDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            // FIX: Added AsNoTracking — read-only list queries should not track entities in memory.
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await ToPagedResultAsync(_dbSet.AsNoTracking(), pageNumber, pageSize);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<PagedResult<T>> FindPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
        {
            return await ToPagedResultAsync(_dbSet.AsNoTracking().Where(predicate), pageNumber, pageSize);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var entry = _dbSet.Add(entity);
            return await Task.FromResult(entry.Entity);
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        protected void DetachConflictingTrackedEntity(T entity)
        {
            if (entity == null) return;
            try
            {
                var entry = _context.Entry(entity);
                if (entry.State != EntityState.Detached) return;

                var keyProps = entry.Metadata.FindPrimaryKey()?.Properties;
                if (keyProps == null || keyProps.Count == 0) return;

                foreach (var tracked in _context.ChangeTracker.Entries<T>().ToList())
                {
                    if (ReferenceEquals(tracked.Entity, entity) || tracked.State == EntityState.Detached)
                        continue;

                    bool sameKey = true;
                    foreach (var kp in keyProps)
                    {
                        var v1 = kp.PropertyInfo?.GetValue(entity);
                        var v2 = kp.PropertyInfo?.GetValue(tracked.Entity);
                        if (v1 == null || v2 == null || !v1.Equals(v2))
                        {
                            sameKey = false;
                            break;
                        }
                    }

                    if (sameKey)
                    {
                        tracked.State = EntityState.Detached;
                    }
                }
            }
            catch { }
        }

        public virtual void Update(T entity)
        {
            DetachConflictingTrackedEntity(entity);
            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                DetachConflictingTrackedEntity(entity);
            }
            _dbSet.UpdateRange(entities);
        }

        public virtual void Remove(T entity)
        {
            DetachConflictingTrackedEntity(entity);
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                DetachConflictingTrackedEntity(entity);
            }
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task RemoveByIdAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Remove(entity);
            }
        }

        public virtual IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            return _dbSet.Include(navigationPropertyPath);
        }

        public virtual IQueryable<T> IncludeMultiple(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return query;
        }

        protected async Task<PagedResult<T>> ToPagedResultAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var totalCount = await query.CountAsync();
            var orderedQuery = ApplyDefaultOrdering(query);
            var items = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
        }

        private static readonly bool _hasIdProperty = typeof(T).GetProperty("Id") != null;

        private static IQueryable<T> ApplyDefaultOrdering(IQueryable<T> query)
        {
            return _hasIdProperty
                ? query.OrderBy(e => EF.Property<int>(e, "Id"))
                : query;
        }
    }
}
