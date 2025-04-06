using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using med_service.ViewModels;
using med_service.Helpers;
using System.Numerics;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<DoctorsController> _localizer;

        public DoctorsController(ApplicationDbContext context, IStringLocalizer<DoctorsController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: Doctors
        public async Task<IActionResult> Index(string sortOrder, string currentFilter,
                                              string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["HospitalSortParam"] = sortOrder == "Hospital" ? "hospital_desc" : "Hospital";
            ViewData["SpecSortParam"] = sortOrder == "Specialization" ? "spec_desc" : "Specialization";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var doctorsQuery = _context.Doctors
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                doctorsQuery = doctorsQuery.Where(d =>
                    (d.User.LastName + " " + d.User.FirstName).Contains(searchString) ||
                    d.Hospital.Name.Contains(searchString) ||
                    d.Specialization.Name.Contains(searchString)
                );
            }

            doctorsQuery = sortOrder switch
            {
                "name_desc" => doctorsQuery.OrderByDescending(d => d.User.LastName).ThenByDescending(d => d.User.FirstName),
                "Hospital" => doctorsQuery.OrderBy(d => d.Hospital.Name),
                "hospital_desc" => doctorsQuery.OrderByDescending(d => d.Hospital.Name),
                "Specialization" => doctorsQuery.OrderBy(d => d.Specialization.Name),
                "spec_desc" => doctorsQuery.OrderByDescending(d => d.Specialization.Name),
                _ => doctorsQuery.OrderBy(d => d.User.LastName).ThenBy(d => d.User.FirstName)
            };

            int pageSize = 7;
            var paginatedList = await PaginatedList<Doctor>.CreateAsync(doctorsQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "Doctors"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
        }

        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
                return NotFound();

            return PartialView("~/Views/Doctors/_Details.cshtml", doctor);
        }

        // Controller modifications
        // GET: Doctors/Create
        public IActionResult Create()
        {
            PrepareDropdownLists();
            return PartialView("~/Views/Doctors/_Create.cshtml", new DoctorViewModel());
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = new Doctor
                    {
                        UserId = viewModel.UserId,
                        HospitalId = viewModel.HospitalId,
                        SpecializationId = viewModel.SpecializationId,
                        ExperienceYears = viewModel.ExperienceYears
                    };
                    _context.Add(doctor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", _localizer["SaveError"]);
                }
            }
            else
            {
                // Для отладки валидации
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                   .Select(e => e.ErrorMessage)
                   .ToList();
                foreach (var error in errors)
                {
                    // Можно логировать ошибки или временно выводить для отладки
                    Console.WriteLine($"Validation error: {error}");
                }
            }
            PrepareDropdownLists();
            return PartialView("~/Views/Doctors/_Create.cshtml", viewModel);
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound();

            var viewModel = new DoctorViewModel
            {
                Id = doctor.Id,
                UserId = doctor.UserId,
                HospitalId = doctor.HospitalId,
                SpecializationId = doctor.SpecializationId,
                ExperienceYears = doctor.ExperienceYears
            };

            PrepareDropdownLists();
            //return View(viewModel);
            return PartialView("~/Views/Doctors/_Edit.cshtml", viewModel);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                        return NotFound();

                    doctor.UserId = viewModel.UserId;
                    doctor.HospitalId = viewModel.HospitalId;
                    doctor.SpecializationId = viewModel.SpecializationId;
                    doctor.ExperienceYears = viewModel.ExperienceYears;

                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(viewModel.Id))
                        return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", _localizer["UpdateError"]);
                }
            }

            PrepareDropdownLists();
            //return View(viewModel);
            return PartialView("~/Views/Doctors/_Edit.cshtml", viewModel);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var doctor = await _context.Doctors
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null)
                return NotFound();
            return PartialView("~/Views/Doctors/_Delete.cshtml", doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var appointments = await _context.Appointments
                        .Where(a => a.DoctorId == id)
                        .ToListAsync();

                    foreach (var appointment in appointments)
                    {
                        var timeSlot = await _context.TimeSlots.FindAsync(appointment.TimeSlotId);
                        if (timeSlot != null)
                        {
                            timeSlot.isBooked = false;
                            timeSlot.Appointment = null;
                            _context.TimeSlots.Update(timeSlot);
                        }

                        _context.Appointments.Remove(appointment);
                    }

                    var schedules = await _context.Schedules
                        .Where(s => s.DoctorId == id)
                        .Include(s => s.AvailableSlots)
                        .ToListAsync();

                    foreach (var schedule in schedules)
                    {
                        _context.TimeSlots.RemoveRange(schedule.AvailableSlots);
                        _context.Schedules.Remove(schedule);
                    }

                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                        return NotFound();

                    _context.Doctors.Remove(doctor);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    Console.WriteLine($"Ошибка при удалении доктора: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");

                    ModelState.AddModelError("", _localizer["DeleteError"] + ": " + ex.Message);

                    var doctor = await _context.Doctors
                        .Include(d => d.Hospital)
                        .Include(d => d.Specialization)
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    return PartialView("~/Views/Doctors/_Delete.cshtml", doctor);
                }
            }
        }



        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        private void PrepareDropdownLists()
        {
            ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Name");
            ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Name");

            var usersQuery = _context.Users.Select(u => new
            {
                Id = u.Id,
                FullName = $"{u.LastName} {u.FirstName}"
            });
            ViewBag.UserId = new SelectList(usersQuery, "Id", "FullName");
        }
    }
}
