using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Core.Repository.ApplicationRoles;
using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Repository.Users;
using IdentityService.Core.Services.AdAuthentication;
using IdentityService.Core.Services.LDAPAuthentication;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace IdentityService.Web.Controllers
{
    [Authorize]
    public class UsersController(IUserRepository _userRepo, IApplicationRoleRepository _appRoleRepo, IUserApplicationRoleRepository _userAppRoleRepo, IMapper _mapper, ILogger<UsersController> _logger, ILdapAuthenticationService _adAuthService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            List<UserViewModel> model = _mapper.Map<List<UserViewModel>>(await _userRepo.FindAll());
            return View(model);
        }

        public IActionResult Create()
        {
            var model = new UserViewModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            User appUser = new()
            {
                UserName = model.UserName,
                EndDate = model.EndDate,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Email = model.Email,
                Title = model.Title,
                IsStaff = model.IsStaff
            };
            AdUserResult adUserResult = new()
            {
                AppUser = appUser,
                IsSuccess = true
            };

            if (model.IsStaff)
            {
                adUserResult = _adAuthService.IsExistsOnAd(model);
            } else
            {
                // Validate password for non-staff users
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError("Password", "Password is required for non-staff users.");
                    return View(model);
                }

                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                    return View(model);
                }

                // Validate required fields for non-staff users
                if (string.IsNullOrWhiteSpace(model.FirstName))
                {
                    ModelState.AddModelError("FirstName", "First Name is required for non-staff users.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.LastName))
                {
                    ModelState.AddModelError("LastName", "Last Name is required for non-staff users.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is required for non-staff users.");
                    return View(model);
                }
            }

            if (!adUserResult.IsSuccess)
            {
                switch (adUserResult.ResultType)
                {
                    case AdResultTypes.ServerUnavailable:
                        ModelState.AddModelError("", "The Active Directory server is currently unavailable. Please contact administrator.");
                        _logger.LogError("Active Directory server unavailable.");
                        break;

                    case AdResultTypes.UserNotFound:
                        ModelState.AddModelError("UserName", "Username does not exist in Active Directory.");
                        _logger.LogWarning("Attempted to create user with non-existent AD username: {Username}", model.UserName);
                        break;

                    default:
                        ModelState.AddModelError("", $"Error validating username: {adUserResult.ErrorMessage}");
                        _logger.LogError(adUserResult.Exception, "Error checking AD for username {Username}: {Message}",
                            model.UserName, adUserResult.ErrorMessage);
                        break;
                }

                return View(model);
            }

            // Check if user already exists in our database
            var existingUser = await _userRepo.FindByName(model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "A user with this username already exists in the system.");
                return View(model);
            }

            var result = await _userRepo.Create(adUserResult.AppUser, model.Password);

            if (result.Succeeded)
            {
                TempData["Success"] = $"User {model.UserName} was created successfully.";
                _logger.LogInformation("User {Username} was created successfully.", model.UserName);
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _logger.LogError("Error creating user {Username}: {Error}", model.UserName, error.Description);
                }
                return View(model);
            }
        }

        public async Task<IActionResult> Update(string userId)
        {
            var result = await _userRepo.FindById(userId);

            var model = _mapper.Map<UserViewModel>(result);

            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserViewModel model)
        {
            var userInfo = await _userRepo.FindById(model.Id);

            if (!model.IsActive)
            {
                userInfo.DisableDate = DateTime.Now;
            }
            else
            {
                userInfo.DisableDate = null;
            }

            var user = _mapper.Map<User>(model);

            var result = await _userRepo.Update(user);

            if (result)
            {
                TempData["Success"] = $"User {model.UserName} was updated successfully.";

                if (model.IsActive)
                    _logger.LogInformation($"User account {model.UserName} has been activated.");
                else
                    _logger.LogInformation($"User account {model.UserName} has been deactivated and disabled.");

                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = $"Failed to update user details for {model.UserName}. Please contact Administrator";
                _logger.LogError($"Failed to update user details for {model.UserName}. Please contact Administrator");
                return RedirectToAction("Index");
            }
        }

        // GET: Users/ResetPassword/5
        public async Task<IActionResult> ResetPassword(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var user = await _userRepo.FindById(userId);

            if (user == null)
            {
                TempData["Error"] = $"User not found.";
                return RedirectToAction("Index");
            }

            if (user.IsStaff)
            {
                TempData["Error"] = "Cannot reset password for staff users. Staff users must change their password through Active Directory.";
                return RedirectToAction("Index");
            }

            var model = new ResetPasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim()
            };

            return View(model);
        }

        // POST: Users/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userRepo.FindById(model.UserId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            if (user.IsStaff)
            {
                TempData["Error"] = "Cannot reset password for staff users.";
                return RedirectToAction("Index");
            }

            var result = await _userRepo.ResetPassword(model.UserId, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Password for user {user.UserName} has been reset successfully.";
                _logger.LogInformation("Password reset for user {Username} by admin", user.UserName);
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                
                // Repopulate display fields
                model.UserName = user.UserName;
                model.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
                
                return View(model);
            }
        }

        public async Task<IActionResult> ManageRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            // get user
            var user = await _userRepo.FindById(userId);

            if (user == null)
            {
                TempData["Error"] = $"User with ID {userId} was not found.";
                return RedirectToAction("Index");
            }

            var allApplicationRoles = await _appRoleRepo.FindAll();
            var userApplicationRoles = await _userAppRoleRepo.FindByUserId(userId);

            var userAppRoleIds = userApplicationRoles.Select(uar => uar.ApplicationRoleId).ToHashSet();
            List<Application> applications = allApplicationRoles.Select(ar => ar.Application).Distinct().ToList();

            Dictionary<string, List<ApplicationRoleSelectViewModel>> applicationRolesByApplications = new();

            foreach (var application in applications)
            {
                applicationRolesByApplications[application.Name] = allApplicationRoles
                    .Where(ar => ar.ApplicationId == application.Id)
                    .Select(ar => new ApplicationRoleSelectViewModel
                    {
                        ApplicationRoleId = ar.Id,
                        RoleName = ar.Role.Name,
                        IsSelected = userAppRoleIds.Contains(ar.Id)
                    })
                    .ToList();
            }

            UserRoleManagementViewModel model = new UserRoleManagementViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                ApplicationRolesByApplications = applicationRolesByApplications
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(UserRoleManagementViewModel model)
        {
            var user = await _userRepo.FindById(model.UserId);
            if (user == null)
                return NotFound();

            var userApplicationRoles = await _userAppRoleRepo.FindByUserId(model.UserId);

            try
            {
                // Remove applicationRoles that are no longer selected
                foreach (var userApplicationRole in userApplicationRoles)
                {
                    foreach (var applicationRoleList in model.ApplicationRolesByApplications.Values)
                    {
                        if (!applicationRoleList.Any(r => r.IsSelected && r.RoleName == userApplicationRole.ApplicationRole.Role.Name))
                        {
                            bool isDeleted = await _userAppRoleRepo.Delete(userApplicationRole.Id);
                            if (!isDeleted)
                            {
                                _logger.LogError($"Failed to remove role {userApplicationRole.ApplicationRole.Role.Name} from user {user.UserName}.");
                                TempData["Error"] = $"Failed to remove role {userApplicationRole.ApplicationRole.Role.Name} from user {user.UserName}. Please try again";
                            }
                        }
                    }
                }

                foreach (var applicationRoleList in model.ApplicationRolesByApplications.Values)
                {
                    foreach (var applicationRole in applicationRoleList.Where(r => r.IsSelected))
                    {
                        //var roleName = (await _roleRepo.FindById(role.RoleId)).Name;
                        if (!userApplicationRoles.Select(r => r.ApplicationRole.Id).ToList().Contains(applicationRole.ApplicationRoleId))
                        {
                            //await _accountRepo.AddApplicationUserFromRole(user, roleName);
                            UserApplicationRole newUserApplicationRole = new UserApplicationRole
                            {
                                UserId = user.Id,
                                ApplicationRoleId = applicationRole.ApplicationRoleId
                            };
                            bool isCreated = await _userAppRoleRepo.Create(newUserApplicationRole);
                            if (!isCreated)
                            {
                                _logger.LogError($"Failed to assign role {applicationRole.RoleName} to user {user.UserName}.");
                                TempData["Error"] = $"Failed to assign role {applicationRole.RoleName} to user {user.UserName}. Please try again";
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while managing roles for user {user.UserName}. {ex.Message}");
                TempData["Error"] = $"An unexpected error occurred while updating roles for user {user.UserName}. Please try again.";
                return View(model);
            }

            TempData["Success"] = $"Roles for user {user.UserName} have been updated successfully.";

            return View(model);
        }
    }
}
