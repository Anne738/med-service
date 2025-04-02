using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Authorization;
using med_service.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using med_service.Helpers;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users/Index
        public async Task<IActionResult> Index(string sortOrder, string currentFilter,
                                               string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["FirstNameSortParam"] = sortOrder == "FirstName" ? "firstName_desc" : "FirstName";
            ViewData["LastNameSortParam"] = sortOrder == "LastName" ? "lastName_desc" : "LastName";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.FirstName.Contains(searchString) ||
                    u.LastName.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    u.UserName.Contains(searchString)
                );
            }

            //Convert to ViewModel before pagination
            var userQuery = users.Select(user => new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role
            });

            userQuery = sortOrder switch
            {
                "FirstName" => userQuery.OrderBy(u => u.FirstName),
                "firstName_desc" => userQuery.OrderByDescending(u => u.FirstName),
                "LastName" => userQuery.OrderBy(u => u.LastName),
                "lastName_desc" => userQuery.OrderByDescending(u => u.LastName),
                _ => userQuery
            };

            int pageSize = 7;
            var paginatedList = await PaginatedList<UserViewModel>.CreateAsync(userQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "Users"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role
            };

            return View(userViewModel);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel userViewModel)
        {
            // Remove the Id field from validation
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState validation failed!");
                foreach (var state in ModelState)
                {
                    Console.WriteLine($"Key: {state.Key}");
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Error: {error.ErrorMessage}");
                    }
                }
                return View(userViewModel); // Re-display the form with validation messages
            }

            // Map ViewModel to User model
            var user = new User
            {
                FirstName = userViewModel.FirstName,
                LastName = userViewModel.LastName,
                Email = userViewModel.Email,
                UserName = userViewModel.UserName,
                Role = userViewModel.Role
            };

            // Create the user in the database
            var result = await _userManager.CreateAsync(user, userViewModel.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(user.Role.ToString()))
                {
                    await _roleManager.CreateAsync(new IdentityRole(user.Role.ToString()));
                }
                await _userManager.AddToRoleAsync(user, user.Role.ToString());

                return RedirectToAction(nameof(Index));
            }

            // Handle creation errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(userViewModel);
        }




        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Map User to UserViewModel
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role,
                Password = string.Empty, // Initialize empty password field
                ConfirmPassword = string.Empty // Initialize empty confirm password field
            };

            return View(userViewModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel userViewModel)
        {
            if (id != userViewModel.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(userViewModel);
            }

            try
            {
                var existingUser = await _userManager.FindByIdAsync(userViewModel.Id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                // Update non-password fields
                existingUser.FirstName = userViewModel.FirstName;
                existingUser.LastName = userViewModel.LastName;
                existingUser.Email = userViewModel.Email;
                existingUser.UserName = userViewModel.UserName;
                existingUser.Role = userViewModel.Role;

                // If new password is provided, update the password
                if (!string.IsNullOrEmpty(userViewModel.Password))
                {
                    if (userViewModel.Password != userViewModel.ConfirmPassword)
                    {
                        ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                        return View(userViewModel);
                    }

                    // Remove existing password (if configured) and set the new one
                    var removePasswordResult = await _userManager.RemovePasswordAsync(existingUser);
                    if (!removePasswordResult.Succeeded)
                    {
                        foreach (var error in removePasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(userViewModel);
                    }

                    var addPasswordResult = await _userManager.AddPasswordAsync(existingUser, userViewModel.Password);
                    if (!addPasswordResult.Succeeded)
                    {
                        foreach (var error in addPasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(userViewModel);
                    }
                }

                // Update user in database
                var identityResult = await _userManager.UpdateAsync(existingUser);
                if (identityResult.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userViewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Concurrency error: the user was modified by another user.");
                    return View(userViewModel);
                }
            }

            return View(userViewModel);
        }


        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Map User to UserViewModel
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role
            };

            return View(userViewModel);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded) return RedirectToAction(nameof(Index));
            }
            return NotFound();
        }


        private bool UserExists(string id)
        {
            return _userManager.Users.Any(e => e.Id == id);
        }
    }
}
