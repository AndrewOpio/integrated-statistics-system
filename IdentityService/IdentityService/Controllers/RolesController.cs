using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Core.Repository.ApplicationPermissions;
using IdentityService.Core.Repository.ApplicationRoles;
using IdentityService.Core.Repository.Applications;
using IdentityService.Core.Repository.Roles;
using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Repository.Users;
using IdentityService.Core.Static;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IdentityService.Web.Controllers
{
    [Authorize]
    public class RolesController(IRoleRepository _roleRepo, IApplicationRepository _appRepo, 
        IApplicationRoleRepository _appRoleRepo, IUserApplicationRoleRepository _userAppRoleRepo, 
        IUserRepository _userRepo, IMapper _mapper, ILogger<RolesController> _logger, 
        IApplicationPermissionRepository _permissionRepo) : Controller
    {
        // GET: RolesController
        public async Task<ActionResult> Index()
        {
            List<ApplicationViewModel> applications = _mapper.Map<List<ApplicationViewModel>>(await _appRepo.FindAll());
            List<SelectListItem> selectApplications = applications
                .Select(a => new SelectListItem { Text = a.Name, Value = a.Id.ToString() })
                .ToList();

            List<RoleViewModel> roles = _mapper.Map<List<RoleViewModel>>(await _roleRepo.FindAll());
            List<SelectListItem> selectRoles = roles
                    .Select(r => new SelectListItem { Text = r.Name, Value = r.Id.ToString() })
                    .ToList();

            List<ApplicationRoleViewModel> applicationRoles = _mapper.Map<List<ApplicationRoleViewModel>>(await _appRoleRepo.FindAll());

            roles.ForEach(role =>
            {
                role.Applications = applicationRoles.Where(a => a.RoleId == role.Id).Select(a => a.Application).ToList();
            });

            RoleManagementViewModel model = new RoleManagementViewModel
            {
                Roles = roles,
                SelectApplications = selectApplications,
                SelectRoles = selectRoles,
                ApplicationRoles = applicationRoles,
            };
            return View(model);
        }

        // GET: RolesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RoleManagementViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    TempData["Error"] = "Failed to create role. Something went Wrong. ModelState is invalid";
            //    ModelState.AddModelError("Failed to create role. Something went wrong. ModelState is invalid", "Failed to create role. Something went wrong. ModelState is invalid");
            //}

            if (model.Role.Name == null || model.Role.Name == string.Empty)
            {
                TempData["Error"] = "Please enter the role name";
                return RedirectToAction(nameof(Index));
            }

            var roleExists = await _roleRepo.IsExists(model.Role.Name);
            if (roleExists)
            {
                TempData["Error"] = $"Role '{model.Role.Name}' already exists";
                ModelState.AddModelError("", $"Role '{model.Role.Name}' already exists");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var role = _mapper.Map<Role>(model.Role);
                bool isCreated = await _roleRepo.Create(role);
                if (isCreated)
                {
                    TempData["Success"] = "Role created successfully!";
                } else
                {
                    TempData["Error"] = "Failed to create role. Something went Wrong. Please try again";
                    _logger.LogError("Error creating role");
                }
            }
            catch(Exception ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogError(ex, "Error creating role");
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string roleId)
        {
            RoleViewModel model = _mapper.Map<RoleViewModel>(await _roleRepo.FindById(roleId));

            if (model == null)
            {
                TempData["Error"] = "The requested role could not be found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[RequirePermission(SystemPermissions.Roles.EditRole)]
        public async Task<IActionResult> Edit(RoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var role = await _roleRepo.FindById(model.Id);

            if (role == null)
            {
                TempData["Error"] = "The requested role could not be found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                role.Name = model.Name;
                bool isUpdated = await _roleRepo.Update(role);

                if (isUpdated)
                {
                    _logger.LogInformation($"Role '{model.Name}' updated successfully.");
                    TempData["Success"] = $"Role '{model.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                } else
                {
                    TempData["Error"] = $"Failed to update role. Please try again.";
                    ModelState.AddModelError("", "Failed to update role. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex) {
                TempData["Error"] = $"Failed to update role. Please try again. \n{ex.Message}";
                ModelState.AddModelError("", "Failed to update role. Please try again.");
                return View(model);
            }
        }

        // POST: RolesController/CreateApplicationRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateApplicationRole(RoleManagementViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    TempData["Error"] = "Failed to create application role. Something went Wrong. ModelState is invalid";
            //    ModelState.AddModelError("Failed to create application role. Something went wrong. ModelState is invalid", "Failed to create role. Something went wrong. ModelState is invalid");
            //}
            if (model.SelectedApplicationId == null)
            {
                TempData["Error"] = "Please select the application";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(model.SelectedRoleId) || string.IsNullOrWhiteSpace(model.SelectedRoleId))
            {
                TempData["Error"] = "Please select the role";
                return RedirectToAction(nameof(Index));
            }

            var roleExists = await _appRoleRepo.IsExists(model.SelectedApplicationId ?? 0, model.SelectedRoleId);
            if (roleExists)
            {
                TempData["Error"] = "Application Role already exists";
                ModelState.AddModelError("", "Application Role already exists");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                ApplicationRole applicationRole = new ApplicationRole
                {
                    ApplicationId = model.SelectedApplicationId ?? 0,
                    RoleId = model.SelectedRoleId
                };

                bool isCreated = await _appRoleRepo.Create(applicationRole);
                if (isCreated)
                {
                    TempData["Success"] = "Application Role created successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to create application role. Something went Wrong. Please try again";
                    _logger.LogError("Error creating application role");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogError(ex, "Error creating application role");
            }
            return RedirectToAction(nameof(Index));
        }

        //[Breadcrumb("Manage Permissions", FromAction = nameof(Index))]
        //[RequirePermission(SystemPermissions.Roles.EditRole)]
        public async Task<IActionResult> ManagePermissions(int appRoleId)
        {
            var appRole = await _appRoleRepo.FindById(appRoleId);
            if (appRole == null)
                return NotFound();

            var allPermissions = await _permissionRepo.GetAllPermissionsForApplication(appRole.Application.Acronym);
            var rolePermissions = await _permissionRepo.GetAllPermissionsForApplicationForRole(appRole.Application.Acronym, appRole.RoleId);

            // Group permissions by controller name
            var model = new ManagePermissionsViewModel
            {
                ApplicationRoleId = appRoleId,
                RoleName = appRole.Role.Name,
                ApplicationName = appRole.Application.Name,
                PermissionGroups = allPermissions
                    .GroupBy(p => ExtractControllerName(p))
                    .Select(g => new PermissionGroupViewModel
                    {
                        GroupName = g.Key,
                        Permissions = g.Select(p => new PermissionViewModel
                        {
                            Name = p,
                            DisplayName = ExtractActionName(p),
                            IsSelected = rolePermissions.Contains(p)
                        }).ToList()
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[RequirePermission(SystemPermissions.Roles.EditRole)]
        public async Task<IActionResult> ManagePermissions(ManagePermissionsViewModel model)
        {
            var appRole = await _appRoleRepo.FindById(model.ApplicationRoleId);
            if (appRole == null)
                return NotFound();

            var currentPermissions = await _permissionRepo.GetAllPermissionsForApplicationForRole(appRole.Application.Acronym, appRole.RoleId);

            var selectedPermissions = model.PermissionGroups
                .SelectMany(g => g.Permissions)
                .Where(p => p.IsSelected)
                .Select(p => p.Name)
                .ToList();

            // Determine permissions to add and remove
            var permissionsToAdd = selectedPermissions.Except(currentPermissions).ToList();
            var permissionsToRemove = currentPermissions.Except(selectedPermissions).ToList();

            try
            {
                // Update permissions
                await _permissionRepo.AddPermissionsToRole(appRole.Application.Acronym, appRole.RoleId, permissionsToAdd);
                await _permissionRepo.RemovePermissionsFromRole(appRole.RoleId, permissionsToRemove);

                _logger.LogInformation("Permissions updated successfully.");
                TempData["Success"] = "Permissions updated successfully.";
            } catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogError(ex.Message);
            }

            return View(model);
        }

        private string ExtractControllerName(string permission)
        {
            var parts = permission.Split(' ');
            return parts.Length > 1 ? parts[1] : "Other";
        }

        private string ExtractActionName(string permission)
        {
            var parts = permission.Split(' ');
            return parts.Length > 2 ? parts[2] : permission;
        }

        // GET: RolesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: RolesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
