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
            var adUserResult = _adAuthService.IsExistsOnAd(model);

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

            // Map from AD to domain model
            //var userDomain = _mapper.Map<ApplicationUser>(adUserResult.AppUser);

            var result = await _userRepo.Create(adUserResult.AppUser);

            if (result)
            {
                TempData["Success"] = $"User {model.UserName} was created successfully.";
                _logger.LogInformation($"User {model.UserName} was created successfully.");
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create user. Please contact administrator.");
                _logger.LogError("Failed to create user.");
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
        //[RequirePermission(SystemPermissions.Account.EditApplicationUser)]
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
                TempData["updated"] = "true";

                if (model.IsActive)
                    _logger.LogInformation($"User account {model.UserName} has been activated.");
                else
                    _logger.LogInformation($"User account {model.UserName} has been deactivated and disabled.");

                return RedirectToAction("Index");
            }
            else
            {
                TempData["updated"] = "false";
                _logger.LogError($"Failed to update user details for {model.UserName}. Please contact Administrator");
                return RedirectToAction("Index");
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
                //foreach (var userApplicationRole in userApplicationRoles)
                //{
                //    foreach (var applicationRoleList in model.ApplicationRolesByApplications.Values)
                //    {
                //        if (!applicationRoleList.Any(r => r.IsSelected && r.RoleName == userApplicationRole.ApplicationRole.Role.Name))
                //        {
                //            bool isDeleted = await _userAppRoleRepo.Delete(userApplicationRole.Id);
                //            if (!isDeleted)
                //            {
                //                _logger.LogError($"Failed to remove role {userApplicationRole.ApplicationRole.Role.Name} from user {user.UserName}.");
                //                TempData["Error"] = $"Failed to remove role {userApplicationRole.ApplicationRole.Role.Name} from user {user.UserName}. Please try again";
                //            }
                //        }
                //    }
                //}

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
