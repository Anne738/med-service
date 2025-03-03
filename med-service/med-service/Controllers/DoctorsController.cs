using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;

namespace med_service.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            // Include Hospital, Specialization, and User for display
            var doctors = _context.Doctors
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .Include(d => d.User);
            return View(await doctors.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id");
            ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id");
            ViewBag.UserId = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,HospitalId,SpecializationId,ExperienceYears,WorkingHours")] Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                return View(doctor);
            }

            // Find and assign Hospital 
            if (doctor.HospitalId > 0)
            {
                var hospital = await _context.Hospitals.FindAsync(doctor.HospitalId);
                if (hospital == null)
                {
                    ModelState.AddModelError("HospitalId", "Указанная больница не найдена.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.Hospital = hospital;
            }

            // Find and assign Specialization
            if (doctor.SpecializationId > 0)
            {
                var specialization = await _context.Specializations.FindAsync(doctor.SpecializationId);
                if (specialization == null)
                {
                    ModelState.AddModelError("SpecializationId", "Указанная специализация не найдена.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.Specialization = specialization;
            }

            // Find and assign User
            if (!string.IsNullOrEmpty(doctor.UserId))
            {
                var user = await _context.Users.FindAsync(doctor.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("UserId", "Указанный пользователь не найден.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.User = user;
            }

            _context.Add(doctor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound();

            ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
            ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
            ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,HospitalId,SpecializationId,ExperienceYears,WorkingHours")] Doctor doctor)
        {
            if (id != doctor.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                return View(doctor);
            }

            // Find references again, similar to Create
            if (doctor.HospitalId > 0)
            {
                var hospital = await _context.Hospitals.FindAsync(doctor.HospitalId);
                if (hospital == null)
                {
                    ModelState.AddModelError("HospitalId", "Указанная больница не найдена.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.Hospital = hospital;
            }

            if (doctor.SpecializationId > 0)
            {
                var specialization = await _context.Specializations.FindAsync(doctor.SpecializationId);
                if (specialization == null)
                {
                    ModelState.AddModelError("SpecializationId", "Указанная специализация не найдена.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.Specialization = specialization;
            }

            if (!string.IsNullOrEmpty(doctor.UserId))
            {
                var user = await _context.Users.FindAsync(doctor.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("UserId", "Указанный пользователь не найден.");
                    ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Id", doctor.HospitalId);
                    ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Id", doctor.SpecializationId);
                    ViewBag.UserId = new SelectList(_context.Users, "Id", "Id", doctor.UserId);
                    return View(doctor);
                }
                doctor.User = user;
            }

            try
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoctorExists(doctor.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
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
    }
}