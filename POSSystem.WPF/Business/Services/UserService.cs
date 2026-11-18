

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _unitOfWork.Users.GetAllAsync();
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _unitOfWork.Users.GetActiveUsersAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _unitOfWork.Users.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _unitOfWork.Users.GetUserByEmailAsync(email);
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _unitOfWork.Users.GetUsersByRoleAsync(role);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            if (!await ValidateUserDataAsync(user))
                throw new ArgumentException("Invalid user data");

            // Check if email already exists
            if (await _unitOfWork.Users.EmailExistsAsync(user.Email))
                throw new InvalidOperationException("User with this email already exists");

            user.Password = PasswordHasher.HashPassword(user.Password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            if (!await ValidateUserDataAsync(user))
                throw new ArgumentException("Invalid user data");

            var context = _unitOfWork.GetContext();

            // 1. فحص تكرار الإيميل مباشرة في SQLite بدون سحب كائن كامل للذاكرة
            bool emailTaken = await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == user.Email && u.Id != user.Id);

            if (emailTaken)
                throw new InvalidOperationException("User with this email already exists");

            var existingUser = await _unitOfWork.Users.GetByIdAsync(user.Id)
                ?? throw new ArgumentException("User not found");

            // 2. تحديث كلمة المرور عند تغيرها فقط
            if (!string.IsNullOrEmpty(user.Password) && user.Password != existingUser.Password)
            {
                user.Password = PasswordHasher.IsPasswordHash(user.Password)
                    ? user.Password
                    : PasswordHasher.HashPassword(user.Password);
            }
            else
            {
                user.Password = existingUser.Password;
            }

            user.UpdatedAt = DateTime.Now;

            // 3. نسخ القيم للكائن المتتبع مباشرة (يحدث الحقول المتغيرة فقط في SQL ويمنع أخطاء التتبع)
            context.Entry(existingUser).CurrentValues.SetValues(user);
            await _unitOfWork.SaveChangesAsync();

            return existingUser;
        }

        // حذف ناعم (Soft Delete) باستعلام واحد مباشر بدون سحب الكائن للرام
        public async Task DeleteUserAsync(int userId)
        {
            int updated = await _unitOfWork.GetContext().Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.IsActive, false)
                    .SetProperty(u => u.UpdatedAt, DateTime.Now));

            if (updated == 0)
                throw new ArgumentException("User not found");

            _unitOfWork.ClearEntityTracking();
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await _unitOfWork.Users.GetUserByEmailAsync(email);
            if (user == null)
                return false;

            var isValid = PasswordHasher.VerifyPassword(password, user.Password);
            if (isValid && PasswordHasher.NeedsRehash(user.Password))
            {
                user.Password = PasswordHasher.HashPassword(password);
                user.UpdatedAt = DateTime.Now;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            return isValid;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _unitOfWork.Users.EmailExistsAsync(email);
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            await _unitOfWork.Users.UpdateLastLoginAsync(userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public Task<bool> ValidateUserDataAsync(User user)
        {
            if (user == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(user.Email)) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(user.Name)) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(user.Password)) return Task.FromResult(false);

            // Basic email validation
            if (!user.Email.Contains("@") || !user.Email.Contains("."))
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public Task<string> HashPasswordAsync(string password)
        {
            return Task.FromResult(PasswordHasher.HashPassword(password));
        }


        public async Task SavePersonalInformation(User _currentUser, string FullName, string Email)
        {
            try
            {
                if (_currentUser != null)
                {
                    _currentUser.Name = FullName;
                    _currentUser.Email = Email;

                    await UpdateUserAsync(_currentUser);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في حفظ المعلومات الشخصية: {ex.Message}");
            }
        }


        // التحقق وتغيير الباسورد بدون استعلام إضافي من الداتابيز
        public async Task<bool> Changepassword(User _currentUser, string currentPassword, string newPassword)
        {
            if (_currentUser == null) return false;

            try
            {
                // التحقق في الذاكرة مباشرة بدلاً من استدعاء ValidateUserAsync الذي يضرب الداتابيز
                if (!PasswordHasher.VerifyPassword(currentPassword, _currentUser.Password))
                    return false;

                var now = DateTime.Now;
                var newHash = PasswordHasher.HashPassword(newPassword);

                await _unitOfWork.GetContext().Users
                    .Where(u => u.Id == _currentUser.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.Password, newHash)
                        .SetProperty(u => u.UpdatedAt, now));

                _currentUser.Password = newHash;
                _currentUser.UpdatedAt = now;
                _unitOfWork.ClearEntityTracking();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في تغيير كلمة المرور: {ex.Message}");
            }
        }



    }
}
