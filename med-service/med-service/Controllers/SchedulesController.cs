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
using med_service.ViewModels;
using med_service.Helpers;


namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index(string sortOrder, string currentFilter,
                                       string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DaySortParam"] = string.IsNullOrEmpty(sortOrder) ? "day_desc" : "";
            ViewData["DoctorSortParam"] = sortOrder == "Doctor" ? "doctor_desc" : "Doctor";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var schedulesQuery = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                schedulesQuery = schedulesQuery.Where(s =>
                    (s.Doctor.User.LastName + " " + s.Doctor.User.FirstName).Contains(searchString)
                );
            }

            schedulesQuery = sortOrder switch
            {
                "day_desc" => schedulesQuery.OrderByDescending(s => s.Day),
                "Doctor" => schedulesQuery.OrderBy(s => s.Doctor.User.LastName).ThenBy(s => s.Doctor.User.FirstName),
                "doctor_desc" => schedulesQuery.OrderByDescending(s => s.Doctor.User.LastName).ThenByDescending(s => s.Doctor.User.FirstName),
                _ => schedulesQuery.OrderBy(s => s.Day)
            };

            int pageSize = 7;
            var paginatedList = await PaginatedList<Schedule>.CreateAsync(schedulesQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "Schedules"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
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

            var model = new ScheduleViewModel
            {
                Id = schedule.Id,
                Day = schedule.Day,
                DoctorId = schedule.DoctorId,
                WorkDayStart = schedule.WorkDayStart,
                WorkDayEnd = schedule.WorkDayEnd,
                DoctorFullName = $"{schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}"
            };

            ViewData["DoctorFullName"] = $"{schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}";

            return View(model);
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

            var vm = new ScheduleViewModel
            {
                WorkDayStart = 8,
                WorkDayEnd = 18
            };

            return View(vm);
        }


        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleViewModel model)
        {
            if (model.WorkDayEnd <= model.WorkDayStart)
            {
                ModelState.AddModelError("WorkDayEnd", "Кінець робочого дня повинен бути пізніше початку");
            }

            if (ModelState.IsValid)
            {
                var schedule = new Schedule
                {
                    Day = (Schedule.DayOfWeek)model.Day,
                    DoctorId = model.DoctorId,
                    WorkDayStart = model.WorkDayStart,
                    WorkDayEnd = model.WorkDayEnd
                };

                try
                {
                    _context.Add(schedule);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Не вдалося зберегти. Перевірте правильність даних.");
                }
            }

            // Повторне заповнення даних для форми
            var doctors = _context.Doctors.Include(d => d.User).ToList();
            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}",
                Selected = d.Id == model.DoctorId
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", model.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(model);
        }



        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            var model = new ScheduleViewModel
            {
                Id = schedule.Id,
                Day = schedule.Day,
                DoctorId = schedule.DoctorId,
                WorkDayStart = schedule.WorkDayStart,
                WorkDayEnd = schedule.WorkDayEnd,
                DoctorFullName = $"{schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}" // ✅ ДОДАНО
            };

            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}",
                Selected = d.Id == model.DoctorId
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", model.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(model);
        }



        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScheduleViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (model.WorkDayEnd <= model.WorkDayStart)
            {
                ModelState.AddModelError("WorkDayEnd", "Кінець робочого дня повинен бути пізніше початку");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var schedule = await _context.Schedules.FindAsync(id);
                    if (schedule == null)
                    {
                        return NotFound();
                    }

                    schedule.Day = (Schedule.DayOfWeek)model.Day;
                    schedule.DoctorId = model.DoctorId;
                    schedule.WorkDayStart = model.WorkDayStart;
                    schedule.WorkDayEnd = model.WorkDayEnd;

                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(model.Id))
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
                    ModelState.AddModelError("", "Не вдалося зберегти зміни. Перевірте, що всі дані заповнені правильно.");
                }
            }

            var doctors = _context.Doctors
                .Include(d => d.User)
                .ToList();

            var doctorItems = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User?.LastName} {d.User?.FirstName}",
                Selected = d.Id == model.DoctorId
            }).ToList();

            ViewData["DoctorId"] = new SelectList(doctorItems, "Value", "Text", model.DoctorId);

            ViewBag.Hours = Enumerable.Range(0, 24).Select(h => new SelectListItem
            {
                Value = h.ToString(),
                Text = $"{h}:00"
            });

            return View(model);
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

            var model = new ScheduleViewModel
            {
                Id = schedule.Id,
                Day = schedule.Day,
                DoctorId = schedule.DoctorId,
                WorkDayStart = schedule.WorkDayStart,
                WorkDayEnd = schedule.WorkDayEnd,
                DoctorFullName = $"{schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}"
            };

            ViewData["DoctorFullName"] = $"{schedule.Doctor?.User?.LastName} {schedule.Doctor?.User?.FirstName}";

            return View(model);
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
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}
