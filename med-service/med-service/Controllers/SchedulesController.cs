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
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // GET: Schedules/Create
        public IActionResult Create()
        {
            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}"
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text");

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            var schedule = new Schedule
            {
                WorkDayStart = 8,
                WorkDayEnd = 18
            };

            return View(schedule);
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Day,DoctorId,WorkDayStart,WorkDayEnd")] Schedule schedule)
        {
            if (schedule.WorkDayEnd <= schedule.WorkDayStart)
            {
                ModelState.AddModelError("WorkDayEnd", "Конец рабочего дня должен быть позже начала");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(schedule);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Не удалось сохранить. Проверьте, что все выбранные значения существуют.");
                }
            }

            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}",
                Selected = d.Id == schedule.DoctorId
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", schedule.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(schedule);
        }

        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}"
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", schedule.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(schedule);
        }

        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Day,DoctorId,WorkDayStart,WorkDayEnd")] Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return NotFound();
            }

            if (schedule.WorkDayEnd <= schedule.WorkDayStart)
            {
                ModelState.AddModelError("WorkDayEnd", "Конец рабочего дня должен быть позже начала");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Не удалось сохранить изменения. Проверьте, что все выбранные значения существуют.");
                }
            }

            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}",
                Selected = d.Id == schedule.DoctorId
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", schedule.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(schedule);
        }

        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.AvailableSlots)
                .ThenInclude(ts => ts.Appointment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule != null)
            {
                foreach (var slot in schedule.AvailableSlots)
                {
                    if (slot.Appointment != null)
                    {
                        _context.Appointments.Remove(slot.Appointment);
                    }
                }

                foreach (var slot in schedule.AvailableSlots.ToList())
                {
                    _context.TimeSlots.Remove(slot);
                }

                _context.Schedules.Remove(schedule);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}
