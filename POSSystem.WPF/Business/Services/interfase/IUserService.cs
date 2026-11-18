
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task DeleteUserAsync(int userId);
        Task<bool> ValidateUserAsync(string email, string password);
        Task<bool> EmailExistsAsync(string email);
        Task UpdateLastLoginAsync(int userId);
        Task<bool> ValidateUserDataAsync(User user);
        Task<string> HashPasswordAsync(string password);
        Task<bool> Changepassword(User _currentUser, string currentPassword, string newPassword);
        Task SavePersonalInformation(User _currentUser, string FullName, string Email);


    }
}
