
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Data.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            // سحب حقل كلمة المرور فقط (Projection) بدون تتبع الكائن في الذاكرة
            var storedHash = await _dbSet
                .AsNoTracking()
                .Where(u => u.Email == email && u.IsActive)
                .Select(u => u.Password)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(storedHash)) return false;
            return PasswordHasher.VerifyPassword(password, storedHash);
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            // FIX: Added AsNoTracking — read-only list query.
            return await _dbSet.AsNoTracking().Where(u => u.IsActive).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            // FIX: Added AsNoTracking — read-only list query.
            return await _dbSet.AsNoTracking().Where(u => u.IsActive && u.Role == role).ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            // تحديث مباشر في SQLite باستعلام واحد دون سحب الكائن إلى الرام
            await _dbSet
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.UpdatedAt, DateTime.Now));
        }

        public string HashPassword(string password)
        {
            return PasswordHasher.HashPassword(password);
        }

        // FIX: Renamed from GetUserByjobnamenameAsync (typo) — kept old name as overload for compatibility.
        public async Task<List<User>?> GetUsersByJobTitleAsync(string jobTitle)
        {
            return await _dbSet.AsNoTracking()
                .Where(u => u.JobTitle == jobTitle && u.IsActive)
                .ToListAsync();
        }

        /// <summary>Kept for backward compatibility. Use GetUsersByJobTitleAsync instead.</summary>
        public async Task<List<User>?> GetUserByjobnamenameAsync(string jobname)
            => await GetUsersByJobTitleAsync(jobname);
    }
}
