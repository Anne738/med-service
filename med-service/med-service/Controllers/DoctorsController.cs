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

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
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
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,HospitalId,SpecializationId,ExperienceYears")] Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(doctor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // Общая обработка ошибок БД (нарушение внешних ключей и т.д.)
                    ModelState.AddModelError("", "Не удалось сохранить. Проверьте, что все выбранные значения существуют.");
                }
            }

            // Если что-то пошло не так, подготавливаем списки снова
            PrepareDropdownLists(doctor);
            return View(doctor);
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound();

            PrepareDropdownLists(doctor);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,HospitalId,SpecializationId,ExperienceYears")] Doctor doctor)
        {
            if (id != doctor.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                        return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Не удалось сохранить изменения. Проверьте, что все выбранные значения существуют.");
                }
            }

            PrepareDropdownLists(doctor);
            return View(doctor);
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

        // Вспомогательный метод для подготовки выпадающих списков
        private void PrepareDropdownLists(Doctor doctor = null)
        {
            // Используем Name/Description для отображения вместо Id
            ViewBag.HospitalId = new SelectList(_context.Hospitals, "Id", "Name", doctor?.HospitalId);
            ViewBag.SpecializationId = new SelectList(_context.Specializations, "Id", "Name", doctor?.SpecializationId);

            // Используем ФИО пользователя для отображения
            var usersQuery = _context.Users.Select(u => new
            {
                Id = u.Id,
                FullName = $"{u.LastName} {u.FirstName}"
            });
            ViewBag.UserId = new SelectList(usersQuery, "Id", "FullName", doctor?.UserId);
        }
    }
}