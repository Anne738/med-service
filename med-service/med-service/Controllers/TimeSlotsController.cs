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
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TimeSlots.Include(t => t.Schedule);
            return View(await applicationDbContext.ToListAsync());
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
            // Получаем расписания с данными врачей для выпадающего списка
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

            // Передаем словарь с диапазонами рабочего времени для каждого расписания
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new {
                    Start = s.WorkDayStart,
                    End = s.WorkDayEnd
                }
            );

            ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);

            // Временные опции будут загружаться динамически в зависимости от выбранного расписания
            ViewBag.TimeOptions = new SelectList(Enumerable.Empty<SelectListItem>());

            return View();
        }

        // POST: TimeSlots/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleId,StartTime")] TimeSlot timeSlot)
        {
            // Проверка на дубликат с учетом точного формата времени
            var existingSlot = await _context.TimeSlots
                .FirstOrDefaultAsync(ts =>
                    ts.ScheduleId == timeSlot.ScheduleId &&
                    ts.StartTime.Hours == timeSlot.StartTime.Hours &&
                    ts.StartTime.Minutes == timeSlot.StartTime.Minutes);

            if (existingSlot != null)
            {
                ModelState.AddModelError("StartTime",
                    $"Временной слот для этого расписания уже существует ({timeSlot.StartTime.Hours:D2}:{timeSlot.StartTime.Minutes:D2})");
            }

            // Проверка, что слот находится в рамках рабочего времени
            var schedule = await _context.Schedules.FindAsync(timeSlot.ScheduleId);
            if (schedule != null)
            {
                if (timeSlot.StartTime.Hours < schedule.WorkDayStart ||
                    timeSlot.StartTime.Hours >= schedule.WorkDayEnd ||
                    (timeSlot.StartTime.Hours == schedule.WorkDayEnd - 1 && timeSlot.StartTime.Minutes > 0))
                {
                    ModelState.AddModelError("StartTime",
                        $"Время слота должно быть в рамках рабочего времени ({schedule.WorkDayStart}:00 - {schedule.WorkDayEnd}:00)");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Устанавливаем EndTime (+30 минут)
                    timeSlot.EndTime = timeSlot.StartTime.Add(TimeSpan.FromMinutes(30));

                    _context.Add(timeSlot);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении: " + ex.Message);
                }
            }

            // Повторно настраиваем списки при ошибке
            var schedules = _context.Schedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            var scheduleItems = schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Doctor?.User?.LastName} - {s.Day}",
                Selected = s.Id == timeSlot.ScheduleId
            }).ToList();

            ViewBag.ScheduleId = new SelectList(scheduleItems, "Value", "Text");

            // Если выбрано расписание, генерируем варианты времени для него
            if (schedule != null)
            {
                ViewBag.TimeOptions = new SelectList(
                    GenerateTimeOptions(schedule.WorkDayStart, schedule.WorkDayEnd),
                    "Value", "Text");

                // Передаем словарь с диапазонами рабочего времени для каждого расписания
                var schedulesWorkHours = schedules.ToDictionary(
                    s => s.Id,
                    s => new {
                        Start = s.WorkDayStart,
                        End = s.WorkDayEnd
                    }
                );

                ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);
            }

            return View(timeSlot);
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            // Получаем расписания с данными врачей для выпадающего списка
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

            // Словарь с диапазонами рабочего времени для каждого расписания
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new {
                    Start = s.WorkDayStart,
                    End = s.WorkDayEnd
                }
            );

            ViewBag.SchedulesWorkHours = JsonSerializer.Serialize(schedulesWorkHours);

            // Генерируем временные опции для выбранного расписания
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

            return View(timeSlot);
        }

        // POST: TimeSlots/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ScheduleId,StartTime,EndTime,isBooked")] TimeSlot timeSlot)
        {
            if (id != timeSlot.Id)
                return NotFound();

            // Проверка, что слот находится в рамках рабочего времени
            var schedule = await _context.Schedules.FindAsync(timeSlot.ScheduleId);
            if (schedule != null)
            {
                if (timeSlot.StartTime.Hours < schedule.WorkDayStart ||
                    timeSlot.StartTime.Hours >= schedule.WorkDayEnd ||
                    (timeSlot.StartTime.Hours == schedule.WorkDayEnd - 1 && timeSlot.StartTime.Minutes > 0))
                {
                    ModelState.AddModelError("StartTime",
                        $"Время слота должно быть в рамках рабочего времени ({schedule.WorkDayStart}:00 - {schedule.WorkDayEnd}:00)");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    timeSlot.EndTime = timeSlot.StartTime.Add(TimeSpan.FromMinutes(30));

                    _context.Update(timeSlot);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TimeSlots.Any(e => e.Id == timeSlot.Id))
                        return NotFound();
                    throw;
                }
            }

            // Получаем расписания для повторного отображения формы
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

            // Словарь с диапазонами рабочего времени
            var schedulesWorkHours = schedules.ToDictionary(
                s => s.Id,
                s => new {
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
                    $"{timeSlot.StartTime.Hours:D2}:{timeSlot.StartTime.Minutes:D2}:00");
            }

            return View(timeSlot);
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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            // Проверка наличия связанных записей
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

                // Добавляем 30-минутный слот, если это не последний час рабочего дня
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