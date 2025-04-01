using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using med_service.ViewModels;
using med_service.Helpers;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TimeSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TimeSlotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TimeSlots
        public async Task<IActionResult> Index(string sortOrder, string currentFilter,
                                       string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["ScheduleSortParam"] = string.IsNullOrEmpty(sortOrder) ? "schedule_desc" : "";
            ViewData["TimeSortParam"] = sortOrder == "Time" ? "time_desc" : "Time";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var query = _context.TimeSlots
                .Include(t => t.Schedule)
                .ThenInclude(s => s.Doctor)
                .ThenInclude(d => d.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(ts =>
                    ts.Schedule.Doctor.User.LastName.Contains(searchString) ||
                    ts.Schedule.Doctor.User.FirstName.Contains(searchString) ||
                    ts.Schedule.Day.ToString().Contains(searchString));
            }

            query = sortOrder switch
            {
                "schedule_desc" => query.OrderByDescending(ts => ts.Schedule.Doctor.User.LastName),
                "Time" => query.OrderBy(ts => ts.StartTime),
                "time_desc" => query.OrderByDescending(ts => ts.StartTime),
                _ => query.OrderBy(ts => ts.Schedule.Doctor.User.LastName)
            };

            var projectedQuery = query.Select(ts => new TimeSlotViewModel
            {
                Id = ts.Id,
                ScheduleId = ts.ScheduleId,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                IsBooked = ts.isBooked,
                ScheduleString = ts.Schedule.Doctor.User.LastName + " - " + ts.Schedule.Day.ToString()
            });

            int pageSize = 7;
            var paginatedList = await PaginatedList<TimeSlotViewModel>.CreateAsync(projectedQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "TimeSlots"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
        }


        // GET: TimeSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Schedule)
                .ThenInclude(s => s.Doctor)
                .ThenInclude(d => d.User)
                .Select(ts => new TimeSlotViewModel
                {
                    Id = ts.Id,
                    ScheduleId = ts.ScheduleId,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    IsBooked = ts.isBooked,
                    ScheduleString = ts.Schedule.Doctor.User.LastName + " - " + ts.Schedule.Day.ToString()
                })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            return View(timeSlot);
        }

        // GET: TimeSlots/Create
        public IActionResult Create()
        {
            // отримуємо розклад з інформацією лікарів для випадаючого списку
            var schedules = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            var scheduleItems = schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Doctor?.User?.LastName} - {s.Day}"
            }).ToList();

            ViewBag.ScheduleId = new SelectList(scheduleItems, "Value", "Text");

            // передаємо словник з діапазоном робочого часу для кожного розкладу
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new
                {
                    Start = s.WorkDayStart,
                    End = s.WorkDayEnd
                }
            );

            ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);

            // часові опції будуть завантажуватися динамічно в залежності від розкладу
            ViewBag.TimeOptions = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(new TimeSlotViewModel());
        }

        // POST: TimeSlots/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleId,StartTime")] TimeSlotViewModel viewModel)
        {
            // перевірка на дублікат з врахуванням точного формату часу
            var existingSlot = await _context.TimeSlots
                .FirstOrDefaultAsync(ts =>
                    ts.ScheduleId == viewModel.ScheduleId &&
                    ts.StartTime.Hours == viewModel.StartTime.Hours &&
                    ts.StartTime.Minutes == viewModel.StartTime.Minutes);

            if (existingSlot != null)
            {
                ModelState.AddModelError("StartTime",
                    $"Временной слот для этого расписания уже существует ({viewModel.StartTime.Hours:D2}:{viewModel.StartTime.Minutes:D2})");
            }

            // перевірка чи слот зноходиться в рамках робочого часу
            var schedule = await _context.Schedules.FindAsync(viewModel.ScheduleId);
            if (schedule != null)
            {
                if (viewModel.StartTime.Hours < schedule.WorkDayStart ||
                    viewModel.StartTime.Hours >= schedule.WorkDayEnd ||
                    (viewModel.StartTime.Hours == schedule.WorkDayEnd - 1 && viewModel.StartTime.Minutes > 0))
                {
                    ModelState.AddModelError("StartTime",
                        $"Время слота должно быть в рамках рабочего времени ({schedule.WorkDayStart}:00 - {schedule.WorkDayEnd}:00)");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var timeSlot = new TimeSlot
                    {
                        ScheduleId = viewModel.ScheduleId,
                        StartTime = viewModel.StartTime,
                        EndTime = viewModel.StartTime.Add(TimeSpan.FromMinutes(30)),
                        isBooked = false
                    };

                    _context.Add(timeSlot);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении: " + ex.Message);
                }
            }

            // повторно налаштовуємо списки при помилці
            var schedules = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            var scheduleItems = schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Doctor?.User?.LastName} - {s.Day}",
                Selected = s.Id == viewModel.ScheduleId
            }).ToList();

            ViewBag.ScheduleId = new SelectList(scheduleItems, "Value", "Text");

            // якщо обрано розклад то генеруємо варіанти часу для нього
            if (schedule != null)
            {
                ViewBag.TimeOptions = new SelectList(
                    GenerateTimeOptions(schedule.WorkDayStart, schedule.WorkDayEnd),
                    "Value", "Text");

                // передаємо словник з діапазоном робочого часц для кожного розкладу
                var schedulesWorkHours = schedules.ToDictionary(
                    s => s.Id,
                    s => new
                    {
                        Start = s.WorkDayStart,
                        End = s.WorkDayEnd
                    }
                );

                ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);
            }

            return View(viewModel);
        }

        // GET: TimeSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Schedule)
                .ThenInclude(s => s.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            var viewModel = new TimeSlotViewModel
            {
                Id = timeSlot.Id,
                ScheduleId = timeSlot.ScheduleId,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                IsBooked = timeSlot.isBooked,
                ScheduleString = timeSlot.Schedule.Doctor.User.LastName + " - " + timeSlot.Schedule.Day.ToString()
            };

            // Отримуємо розклад з інформацією про лікарів для випадаючого списку
            var schedules = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            var scheduleItems = schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Doctor?.User?.LastName} - {s.Day}"
            }).ToList();

            ViewBag.ScheduleId = new SelectList(scheduleItems, "Value", "Text", timeSlot.ScheduleId);

            // словник з діапазоном робочого часу для кодного розкладу
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new
                {
                    Start = s.WorkDayStart,
                    End = s.WorkDayEnd
                }
            );

            ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);

            // гереруємо часові опції для обраного розкладу
            if (timeSlot.Schedule != null)
            {
                var timeOptions = GenerateTimeOptions(timeSlot.Schedule.WorkDayStart, timeSlot.Schedule.WorkDayEnd);
                ViewBag.TimeOptions = new SelectList(timeOptions, "Value", "Text",
                    $"{timeSlot.StartTime.Hours:D2}:{timeSlot.StartTime.Minutes:D2}:00");
            }
            else
            {
                ViewBag.TimeOptions = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            return View(viewModel);
        }

        // POST: TimeSlots/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ScheduleId,StartTime,IsBooked")] TimeSlotViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            // перевірка чи слот знаходиться в педіоді робочого часу
            var schedule = await _context.Schedules.FindAsync(viewModel.ScheduleId);
            if (schedule != null)
            {
                if (viewModel.StartTime.Hours < schedule.WorkDayStart ||
                    viewModel.StartTime.Hours >= schedule.WorkDayEnd ||
                    (viewModel.StartTime.Hours == schedule.WorkDayEnd - 1 && viewModel.StartTime.Minutes > 0))
                {
                    ModelState.AddModelError("StartTime",
                        $"Время слота должно быть в рамках рабочего времени ({schedule.WorkDayStart}:00 - {schedule.WorkDayEnd}:00)");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var timeSlot = await _context.TimeSlots.FindAsync(id);
                    if (timeSlot == null)
                        return NotFound();

                    timeSlot.ScheduleId = viewModel.ScheduleId;
                    timeSlot.StartTime = viewModel.StartTime;
                    timeSlot.EndTime = viewModel.StartTime.Add(TimeSpan.FromMinutes(30));
                    timeSlot.isBooked = viewModel.IsBooked;

                    _context.Update(timeSlot);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TimeSlots.Any(e => e.Id == viewModel.Id))
                        return NotFound();
                    throw;
                }
            }

            // отримання розкладу для повторного відображення форми 
            var schedules = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            var scheduleItems = schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Doctor?.User?.LastName} - {s.Day}"
            }).ToList();

            ViewBag.ScheduleId = new SelectList(scheduleItems, "Value", "Text", viewModel.ScheduleId);

            // словник з діапазонами робочого часу
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new
                {
                    Start = s.WorkDayStart,
                    End = s.WorkDayEnd
                }
            );

            ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);

            if (schedule != null)
            {
                ViewBag.TimeOptions = new SelectList(
                    GenerateTimeOptions(schedule.WorkDayStart, schedule.WorkDayEnd),
                    "Value", "Text",
                    $"{viewModel.StartTime.Hours:D2}:{viewModel.StartTime.Minutes:D2}:00");
            }

            return View(viewModel);
        }

        // GET: TimeSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Schedule)
                .ThenInclude(s => s.Doctor)
                .ThenInclude(d => d.User)
                .Select(ts => new TimeSlotViewModel
                {
                    Id = ts.Id,
                    ScheduleId = ts.ScheduleId,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    IsBooked = ts.isBooked,
                    ScheduleString = ts.Schedule.Doctor.User.LastName + " - " + ts.Schedule.Day.ToString()
                })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            // перевірка наявності запису
            var hasRelatedAppointment = await _context.Appointments
                .AnyAsync(a => a.TimeSlotId == id);

            if (hasRelatedAppointment)
            {
                TempData["ErrorMessage"] = "Нельзя удалить слот, связанный с приёмами.";
            }

            return View(timeSlot);
        }

        // POST: TimeSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeSlot = await _context.TimeSlots.FindAsync(id);
            if (timeSlot != null)
            {
                _context.TimeSlots.Remove(timeSlot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TimeSlotExists(int id)
        {
            return _context.TimeSlots.Any(e => e.Id == id);
        }

        private List<SelectListItem> GenerateTimeOptions(int startHour, int endHour)
        {
            var timeOptions = new List<SelectListItem>();

            for (int hour = startHour; hour < endHour; hour++)
            {
                timeOptions.Add(new SelectListItem
                {
                    Value = $"{hour:D2}:00:00",
                    Text = $"{hour:D2}:00 - {hour:D2}:30"
                });

                // додаємо 30-хвилинний слот, якщо це не остання година робочого дня
                if (hour < endHour - 1 || endHour == startHour + 1)
                {
                    timeOptions.Add(new SelectListItem
                    {
                        Value = $"{hour:D2}:30:00",
                        Text = $"{hour:D2}:30 - {(hour + 1):D2}:00"
                    });
                }
            }

            return timeOptions;
        }
    }
}