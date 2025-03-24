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
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .Include(d => d.Schedules)
                .ToListAsync();

            return View(doctors);
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

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            PrepareDropdownLists();
            return View(new DoctorViewModel());
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
            return View(viewModel);
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
            return View(viewModel);
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
            return View(viewModel);
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

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
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