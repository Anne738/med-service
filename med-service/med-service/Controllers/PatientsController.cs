using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using med_service.ViewModels;
using med_service.Helpers;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; 

        public PatientsController(ApplicationDbContext context, UserManager<User> userManager) 
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string sortOrder, string currentFilter,
                                     string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParam"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var patientsQuery = _context.Patients
                .Include(p => p.User)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(searchString))
            {
                patientsQuery = patientsQuery.Where(p =>
                    (p.User.FirstName + " " + p.User.LastName).Contains(searchString) ||
                    p.User.FirstName.Contains(searchString) ||
                    p.User.LastName.Contains(searchString)
                );
            }

            patientsQuery = sortOrder switch
            {
                "name_desc" => patientsQuery.OrderByDescending(p => p.User.LastName).ThenByDescending(p => p.User.FirstName),
                "Date" => patientsQuery.OrderBy(p => p.DateOfBirth),
                "date_desc" => patientsQuery.OrderByDescending(p => p.DateOfBirth),
                _ => patientsQuery.OrderBy(p => p.User.LastName).ThenBy(p => p.User.FirstName)
            };

            int pageSize = 10;
            var paginatedList = await PaginatedList<Patient>.CreateAsync(patientsQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "Patients"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return PartialView("~/Views/Patients/_Details.cshtml", patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            var viewModel = new PatientViewModel
            {
                DateOfBirth = DateTime.Today,
                UserList = new SelectList(users, "Id", "FullName")
            };
            return PartialView("~/Views/Patients/_Create.cshtml", viewModel);
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(viewModel.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("UserId", "Користувач не знайдений");

                    var users = _context.Users.Select(u => new
                    {
                        u.Id,
                        FullName = $"{u.FirstName} {u.LastName}"
                    }).ToList();

                    viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                    return PartialView("~/Views/Patients/_Create.cshtml", viewModel);
                }

                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == viewModel.UserId);

                if (existingPatient != null)
                {
                    ModelState.AddModelError("UserId", "Пацієнт з цим користувачем вже існує");

                    var users = _context.Users.Select(u => new
                    {
                        u.Id,
                        FullName = $"{u.FirstName} {u.LastName}"
                    }).ToList();

                    viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                    return PartialView("~/Views/Patients/_Create.cshtml", viewModel);
                }

                var patient = new Patient
                {
                    UserId = viewModel.UserId,
                    DateOfBirth = viewModel.DateOfBirth
                };

                _context.Add(patient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var userList = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            viewModel.UserList = new SelectList(userList, "Id", "FullName", viewModel.UserId);
            return PartialView("~/Views/Patients/_Create.cshtml", viewModel);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            var users = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            var viewModel = new PatientViewModel
            {
                Id = patient.Id,
                UserId = patient.UserId,
                DateOfBirth = patient.DateOfBirth,
                FullName = $"{patient.User?.FirstName} {patient.User?.LastName}",
                UserList = new SelectList(users, "Id", "FullName", patient.UserId)
            };

            return PartialView("~/Views/Patients/_Edit.cshtml", viewModel);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _context.Patients.FindAsync(id);
                    if (patient == null)
                    {
                        return NotFound();
                    }

                    if (patient.UserId != viewModel.UserId)
                    {
                        var existingPatient = await _context.Patients
                            .FirstOrDefaultAsync(p => p.UserId == viewModel.UserId && p.Id != id);

                        if (existingPatient != null)
                        {
                            ModelState.AddModelError("UserId", "Пацієнт з цим користувачем вже існує");

                            var users = _context.Users.Select(u => new
                            {
                                u.Id,
                                FullName = $"{u.FirstName} {u.LastName}"
                            }).ToList();

                            viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                            return PartialView("~/Views/Patients/_Edit.cshtml", viewModel);
                        }
                    }

                    patient.UserId = viewModel.UserId;
                    patient.DateOfBirth = viewModel.DateOfBirth;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var userList = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            viewModel.UserList = new SelectList(userList, "Id", "FullName", viewModel.UserId);
            return PartialView("~/Views/Patients/_Edit.cshtml", viewModel);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return PartialView("~/Views/Patients/_Delete.cshtml", patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                var hasAppointments = await _context.Appointments
                    .AnyAsync(a => a.PatientId == id);

                if (hasAppointments)
                {
                    TempData["Error"] = "Неможливо видалити пацієнта, оскільки у нього є записи на прийом";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Patients.Remove(patient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

    }
}
