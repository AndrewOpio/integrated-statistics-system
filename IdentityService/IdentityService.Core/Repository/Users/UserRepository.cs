using IdentityService.Core.Models;
using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Core.Repository.Users
{
    public class UserRepository(ApplicationDbContext _db, UserManager<User> _userManager, ILogger<UserRepository> _logger) : IUserRepository
    {
        public async Task<IdentityResult> Create(User model, string password)
        {
            IdentityResult result;

            if (model.IsStaff)
            {
                // Staff users authenticate via AD, no password needed
                result = await _userManager.CreateAsync(model);
            }
            else
            {
                // Validate password before creating non-staff user
                if (string.IsNullOrWhiteSpace(password))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "PasswordRequired",
                        Description = "Password is required for non-staff users."
                    });
                }

                // Validate password against configured password policies
                var passwordValidators = _userManager.PasswordValidators;
                foreach (var validator in passwordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, model, password);
                    if (!validationResult.Succeeded)
                    {
                        _logger.LogWarning("Password validation failed for user {Username}: {Errors}",
                            model.UserName,
                            string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                        return validationResult;
                    }
                }

                // Set MustChangePassword flag for non-staff users on first creation
                model.MustChangePassword = true;
                model.LastPasswordChangeDate = null;

                // Non-staff users need a password for local authentication
                result = await _userManager.CreateAsync(model, password);
            }

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user {Username}: {Errors}",
                    model.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<IdentityResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "User not found."
                });
            }

            if (user.IsStaff)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "StaffUser",
                    Description = "Cannot reset password for staff users. Staff users must change their password through Active Directory."
                });
            }

            // Validate new password against configured password policies
            var passwordValidators = _userManager.PasswordValidators;
            foreach (var validator in passwordValidators)
            {
                var validationResult = await validator.ValidateAsync(_userManager, user, newPassword);
                if (!validationResult.Succeeded)
                {
                    _logger.LogWarning("Password validation failed for user {Username}: {Errors}",
                        user.UserName,
                        string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                    return validationResult;
                }
            }

            // Generate password reset token and reset the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Set flag to require password change on next login
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password reset successfully for user {Username} by admin. User must change password on next login.", user.UserName);
            }
            else
            {
                _logger.LogError("Password reset failed for user {Username}: {Errors}",
                    user.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<IdentityResult> ChangePassword(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "User not found."
                });
            }

            if (user.IsStaff)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "StaffUser",
                    Description = "Staff users cannot change their password here. Please use Active Directory."
                });
            }

            // Validate new password against configured password policies
            var passwordValidators = _userManager.PasswordValidators;
            foreach (var validator in passwordValidators)
            {
                var validationResult = await validator.ValidateAsync(_userManager, user, newPassword);
                if (!validationResult.Succeeded)
                {
                    _logger.LogWarning("Password validation failed for user {Username}: {Errors}",
                        user.UserName,
                        string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                    return validationResult;
                }
            }

            // Ensure new password is different from current
            if (currentPassword == newPassword)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "SamePassword",
                    Description = "New password must be different from current password."
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                // Clear the MustChangePassword flag
                user.MustChangePassword = false;
                user.LastPasswordChangeDate = DateTime.Now;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password changed successfully for user {Username}", user.UserName);
            }
            else
            {
                _logger.LogWarning("Password change failed for user {Username}: {Errors}",
                    user.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<ICollection<User>> FindAll()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<User> FindById(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<User?> FindByName(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<bool> IsExists(string id)
        {
            return await _userManager.Users.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(User model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user != null)
            {
                user.UserName = model.UserName;
                user.IsActive = model.IsActive;
                user.DisableDate = model.DisableDate;
                user.EndDate = model.EndDate;
                user.Title = model.Title;

                if (!user.IsStaff)
                {
                    user.Email = model.Email;
                    user.EmailConfirmed = model.EmailConfirmed;
                    user.FirstName = model.FirstName;
                    user.MiddleName = model.MiddleName;
                    user.LastName = model.LastName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public async Task<List<NonStaffUserViewModel>> GetNonStaffUserList()
        {
            return await _userManager.Users.Where(u => !u.IsStaff)
                .Select(u => new NonStaffUserViewModel()
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    MiddleName = u.MiddleName,
                    Title = u.Title,
                    Email = u.Email
                }).ToListAsync();
        }

        public Task<bool> Create(User entity)
        {
            throw new NotImplementedException();
        }
    }
}
