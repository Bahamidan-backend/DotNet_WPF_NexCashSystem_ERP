
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserAsync(string email, string password);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<bool> EmailExistsAsync(string email);
        Task UpdateLastLoginAsync(int userId);
        Task<List<User>?> GetUserByjobnamenameAsync(string jobname);

    }
}
