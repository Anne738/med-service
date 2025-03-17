using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace med_service.Controllers
{
    public class TimetablesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TimetablesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Timetables/Doctor/5
        public async Task<IActionResult> Doctor(int id, int? selectedDay)
        {
            if (id == 0)
                return NotFound();

            var doctor = await _db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            var schedules = await _db.Schedules
                .Include(s => s.AvailableSlots)
                .Where(s => s.DoctorId == id)
                .ToListAsync();

            // За замовчуванням вибираємо понеділок
            Schedule.DayOfWeek selectedWeekDay = Schedule.DayOfWeek.Monday;
            if (selectedDay.HasValue && Enum.IsDefined(typeof(Schedule.DayOfWeek), selectedDay.Value))
            {
                selectedWeekDay = (Schedule.DayOfWeek)selectedDay.Value;
            }

            DateTime selectedDate = GetNextAvailableDate(selectedWeekDay);

            ViewBag.DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}";
            ViewBag.DoctorId = doctor.Id;
            ViewBag.SelectedDay = selectedWeekDay;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd"); // для HTML

            return View(schedules);
        }




        // GET: Timetables/Book/5
        public async Task<IActionResult> Book(int id)
        {
            if (id == 0)
                return NotFound();

            var timeSlot = await _db.TimeSlots
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            if (timeSlot == null || timeSlot.isBooked)
                return NotFound();

            var patients = await _db.Patients
                .Include(p => p.User)
                .ToListAsync();

            var viewModel = new BookAppointmentViewModel
            {
                TimeSlotId = timeSlot.Id,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                DoctorId = timeSlot.Schedule.DoctorId,
                DoctorName = $"{timeSlot.Schedule.Doctor.User.FirstName} {timeSlot.Schedule.Doctor.User.LastName}",
                ScheduleDay = timeSlot.Schedule.Day,
                PatientsList = patients.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.User.FirstName} {p.User.LastName}"
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]

        public async Task<IActionResult> Book(string day, int hour, int minute, int doctorId)
        {
            if (string.IsNullOrEmpty(day) || doctorId == 0)
                return NotFound();

            if (!Enum.TryParse<Schedule.DayOfWeek>(day, true, out var scheduleDay))
                return NotFound();

            // Отримуємо найближчу відповідну дату
            DateTime appointmentDate = GetNextAvailableDate(scheduleDay);

            var doctor = await _db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                return NotFound();

            var schedule = await _db.Schedules
                .Include(s => s.AvailableSlots)
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.Day == scheduleDay);

            if (schedule == null)
            {
                schedule = new Schedule
                {
                    Day = scheduleDay,
                    DoctorId = doctorId,
                    Doctor = doctor
                };
                _db.Schedules.Add(schedule);
                await _db.SaveChangesAsync();
            }

            var startTime = new TimeSpan(hour, minute, 0);
            var slot = schedule.AvailableSlots.FirstOrDefault(ts =>
                ts.StartTime.Hours == hour && ts.StartTime.Minutes == minute);

            if (slot == null)
            {
                slot = new TimeSlot
                {
                    StartTime = startTime,
                    EndTime = startTime.Add(TimeSpan.FromMinutes(30)),
                    ScheduleId = schedule.Id
                };
                _db.TimeSlots.Add(slot);
                await _db.SaveChangesAsync();
            }

            if (slot.isBooked)
            {
                TempData["Error"] = "Цей слот вже зайнятий.";
                return RedirectToAction("Doctor", new { id = doctorId });
            }

            var patients = await _db.Patients.Include(p => p.User).ToListAsync();
            var viewModel = new BookAppointmentViewModel
            {
                TimeSlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                DoctorId = doctorId,
                DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                ScheduleDay = scheduleDay,
                PatientsList = patients.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.User.FirstName} {p.User.LastName}"
                }).ToList()
            };

            ViewBag.AppointmentDate = appointmentDate.ToString("yyyy-MM-dd");

            return View("Book", viewModel);
        }





        // POST: Timetables/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var timeSlot = await _db.TimeSlots
                    .Include(ts => ts.Schedule)
                    .FirstOrDefaultAsync(ts => ts.Id == model.TimeSlotId);

                if (timeSlot == null || timeSlot.isBooked)
                {
                    ModelState.AddModelError("", "Извините, данный слот уже занят");
                    var patients = await _db.Patients.Include(p => p.User).ToListAsync();
                    model.PatientsList = patients.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.User.FirstName} {p.User.LastName}"
                    }).ToList();
                    return View(model);
                }

                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = timeSlot.Schedule.DoctorId,
                    TimeSlotId = timeSlot.Id,
                    Status = Appointment.AppointmentStatus.PENDING,
                    Notes = model.Notes
                };

                _db.Appointments.Add(appointment);
                timeSlot.isBooked = true;
                await _db.SaveChangesAsync();

                return RedirectToAction("Details", "Appointments", new { id = appointment.Id });
            }

            var patientList = await _db.Patients.Include(p => p.User).ToListAsync();
            model.PatientsList = patientList.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.User.FirstName} {p.User.LastName}"
            }).ToList();
            return View(model);
        }

        private DateTime GetNextAvailableDate(Schedule.DayOfWeek targetDay)
        {
            DateTime today = DateTime.Today;
            DayOfWeek systemDayOfWeek = (DayOfWeek)targetDay; // Явне перетворення

            int daysToAdd = ((int)systemDayOfWeek - (int)today.DayOfWeek + 8) % 7;
            return today.AddDays(daysToAdd == 0 ? 7 : daysToAdd);
        }



    }



    // ViewModel для страницы записи на приём
    public class BookAppointmentViewModel
    {
        public int TimeSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public Schedule.DayOfWeek ScheduleDay { get; set; }
        public int PatientId { get; set; }
        public string Notes { get; set; }
        public List<SelectListItem> PatientsList { get; set; } = new List<SelectListItem>();
    }
}
