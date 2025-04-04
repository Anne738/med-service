using med_service.Data;
using med_service.Models;
using med_service.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace med_service.Controllers
{
    public class TimetablesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public TimetablesController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
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

        // GET: Timetables/Book
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Book(string day, int hour, int minute, int doctorId)
        {
            if (string.IsNullOrEmpty(day) || doctorId == 0)
                return NotFound();

            // Отримуємо поточного користувача
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "Будь ласка, увійдіть в систему для запису на прийом";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Doctor", new { id = doctorId }) });
            }

            // Перевіряємо, чи користувач є адміністратором
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Якщо не адмін, перевіряємо чи є користувач пацієнтом
            Patient? patient = null;
            if (!isAdmin)
            {
                patient = await _db.Patients
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

                if (patient == null)
                {
                    TempData["Error"] = "Тільки пацієнти або адміністратори можуть записатися на прийом";
                    return RedirectToAction("Doctor", new { id = doctorId });
                }
            }

            // Парсинг рядка з днем тижня в enum
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
                // Перезавантажуємо розклад, щоб отримати новий слот у колекції
                await _db.Entry(schedule).ReloadAsync();
                await _db.Entry(schedule).Collection(s => s.AvailableSlots).LoadAsync();
            }

            if (slot.isBooked)
            {
                TempData["Error"] = "Цей слот вже зайнятий.";
                return RedirectToAction("Doctor", new { id = doctorId });
            }

            var viewModel = new BookAppointmentViewModel
            {
                TimeSlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                DoctorId = doctorId,
                DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                ScheduleDay = scheduleDay,
                IsAdmin = isAdmin
            };

            // Якщо адміністратор, надаємо список пацієнтів для вибору
            if (isAdmin)
            {
                var patients = await _db.Patients
                    .Include(p => p.User)
                    .ToListAsync();

                viewModel.PatientsList = patients.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.User.FirstName} {p.User.LastName}"
                }).ToList();
            }
            else
            {
                // Якщо звичайний пацієнт, встановлюємо поточного пацієнта
                viewModel.PatientId = patient.Id;
                viewModel.PatientName = $"{currentUser.FirstName} {currentUser.LastName}";
            }

            ViewBag.AppointmentDate = appointmentDate.ToString("yyyy-MM-dd");

            return PartialView("~/Views/Timetables/_Book.cshtml", viewModel);
        }

        // POST: Timetables/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["FormError"] = "Будь ласка, увійдіть в систему для запису на прийом";
                    return RedirectToAction("Login", "Account");
                }

                // Перевіряємо, чи користувач є адміністратором
                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Якщо не адмін, перевіряємо чи є користувач пацієнтом і чи відповідає обраний пацієнт поточному користувачу
                if (!isAdmin)
                {
                    var patient = await _db.Patients
                        .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

                    if (patient == null)
                    {
                        TempData["FormError"] = "Тільки пацієнти або адміністратори можуть записатися на прийом";
                        return RedirectToAction("Doctor", new { id = model.DoctorId });
                    }

                    // Перевіряємо, чи PatientId відповідає поточному користувачу
                    if (model.PatientId != patient.Id)
                    {
                        ModelState.AddModelError("PatientId", "Ви можете записатися на прийом тільки для свого облікового запису");
                        TempData["FormError"] = "Ви можете записатися на прийом тільки для свого облікового запису";
                    }
                }

                // Перевіряємо базові валідації моделі
                if (!ModelState.IsValid)
                {
                    var errorMessages = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                    TempData["FormError"] = $"Помилка валідації: {errorMessages}";
                }
                else
                {
                    var timeSlot = await _db.TimeSlots
                        .Include(ts => ts.Schedule)
                        .FirstOrDefaultAsync(ts => ts.Id == model.TimeSlotId);

                    if (timeSlot == null)
                    {
                        ModelState.AddModelError("TimeSlotId", "Вказаний часовий слот не знайдено");
                        TempData["FormError"] = "Вказаний часовий слот не знайдено";
                    }
                    else if (timeSlot.isBooked)
                    {
                        ModelState.AddModelError("TimeSlotId", "Вибачте, цей слот вже зайнятий");
                        TempData["FormError"] = "Вибачте, цей слот вже зайнятий";
                    }
                    else
                    {
                        // Перевіряємо, чи обрано пацієнта
                        if (model.PatientId <= 0)
                        {
                            ModelState.AddModelError("PatientId", "Необхідно вибрати пацієнта");
                            TempData["FormError"] = "Необхідно вибрати пацієнта";
                        }
                        else
                        {
                            try
                            {
                                // Всі перевірки пройдені, створюємо запис
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

                                // Перенаправляем пациента на список его записей, а админа - на детали записи
                                if (isAdmin)
                                {
                                    return RedirectToAction("Details", "Appointments", new { id = appointment.Id });
                                }
                                else
                                {
                                    // Перенаправляем на страницу с записями пациента
                                    return RedirectToAction("MyAppointments");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Логування помилки при збереженні
                                ModelState.AddModelError("", $"Помилка при збереженні: {ex.Message}");
                                TempData["FormError"] = $"Помилка при збереженні: {ex.Message}";

                                if (ex.InnerException != null)
                                {
                                    TempData["FormError"] += $" Деталі: {ex.InnerException.Message}";
                                }
                            }
                        }
                    }
                }

                // Якщо дійшли до цього місця, значить виникла помилка і потрібно перезавантажити форму
                // Заповнюємо дані для форми знову
                if (isAdmin)
                {
                    if (model.IsAdmin && model.PatientId > 0)
                    {
                        var selectedPatient = await _db.Patients
                            .Include(p => p.User)
                            .FirstOrDefaultAsync(p => p.Id == model.PatientId);

                        if (selectedPatient != null)
                        {
                            model.PatientName = $"{selectedPatient.User.FirstName} {selectedPatient.User.LastName}";
                        }
                    }

                    var patients = await _db.Patients
                        .Include(p => p.User)
                        .ToListAsync();

                    model.PatientsList = patients.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.User.FirstName} {p.User.LastName}"
                    }).ToList();
                    model.IsAdmin = true;
                }
                else
                {
                    model.PatientName = $"{currentUser.FirstName} {currentUser.LastName}";
                }

                return PartialView("~/Views/Timetables/_Book.cshtml", model);
            }
            catch (Exception ex)
            {
                // Загальна обробка винятків
                ModelState.AddModelError("", $"Помилка при обробці запиту: {ex.Message}");
                TempData["FormError"] = $"Помилка при обробці запиту: {ex.Message}";

                // Підготовка даних для повторного відображення форми
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                    if (isAdmin)
                    {
                        var patients = await _db.Patients
                            .Include(p => p.User)
                            .ToListAsync();

                        model.PatientsList = patients.Select(p => new SelectListItem
                        {
                            Value = p.Id.ToString(),
                            Text = $"{p.User.FirstName} {p.User.LastName}"
                        }).ToList();
                        model.IsAdmin = true;
                    }
                    else if (currentUser != null)
                    {
                        model.PatientName = $"{currentUser.FirstName} {currentUser.LastName}";
                    }
                }
                catch
                {
                    // Ігноруємо помилки при спробі відновити дані моделі
                }

                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> MyAppointments()
        {
            // Получаем текущего пользователя
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Перенаправляет на страницу входа
            }

            // Проверяем роли пользователя
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            bool isDoctor = await _userManager.IsInRoleAsync(currentUser, "Doctor");

            if (isAdmin)
            {
                // Админов перенаправляем на полный список записей
                return RedirectToAction("Index", "Appointments");
            }

            if (isDoctor)
            {
                // Для докторов показываем их записи
                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == currentUser.Id);

                if (doctor == null)
                {
                    TempData["Error"] = "Профіль лікаря не знайдено";
                    return RedirectToAction("Index", "Home");
                }

                var doctorAppointments = await _db.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Include(a => a.TimeSlot).ThenInclude(ts => ts.Schedule)
                    .Where(a => a.DoctorId == doctor.Id)
                    .OrderByDescending(a => a.TimeSlot.StartTime)
                    .ToListAsync();

                ViewBag.UserRole = "Doctor";
                return View(doctorAppointments);
            }
            else
            {
                // Для пациентов показываем их записи
                var patient = await _db.Patients
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

                if (patient == null)
                {
                    TempData["Error"] = "Для перегляду записів ви повинні мати профіль пацієнта";
                    return RedirectToAction("Index", "Home");
                }

                var patientAppointments = await _db.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .Include(a => a.TimeSlot).ThenInclude(ts => ts.Schedule)
                    .Where(a => a.PatientId == patient.Id)
                    .OrderByDescending(a => a.TimeSlot.StartTime)
                    .ToListAsync();

                ViewBag.UserRole = "Patient";
                return View(patientAppointments);
            }
        }

        // GET: Timetables/FindDoctor
        public async Task<IActionResult> FindDoctor(DoctorFilterViewModel model)
        {
            // Заполним списки для фильтров
            model.Specializations = await _db.Specializations
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            model.Hospitals = await _db.Hospitals
                .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Name })
                .ToListAsync();

            // Получаем запрос на выборку докторов
            var doctorsQuery = _db.Doctors
                .Include(d => d.User)
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .AsQueryable();

            // Применяем фильтры
            if (model.SpecializationId.HasValue)
            {
                doctorsQuery = doctorsQuery.Where(d => d.SpecializationId == model.SpecializationId.Value);
            }

            if (model.HospitalId.HasValue)
            {
                doctorsQuery = doctorsQuery.Where(d => d.HospitalId == model.HospitalId.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.DoctorName))
            {
                doctorsQuery = doctorsQuery.Where(d =>
                    d.User.FirstName.Contains(model.DoctorName) ||
                    d.User.LastName.Contains(model.DoctorName));
            }

            // Выполняем запрос
            model.Doctors = await doctorsQuery.ToListAsync();
            model.IsSearched = true;

            // Если найден только один доктор, сразу перенаправляем на его расписание
            if (model.Doctors.Count == 1)
            {
                return RedirectToAction("Doctor", new { id = model.Doctors[0].Id });
            }

            // Возвращаем результаты на главную страницу
            return View("~/Views/Home/Index.cshtml", model);
        }
    

        private DateTime GetNextAvailableDate(Schedule.DayOfWeek targetDay)
        {
            DateTime today = DateTime.Today;
            DayOfWeek systemDayOfWeek = (DayOfWeek)targetDay; // Явне перетворення

            int daysToAdd = ((int)systemDayOfWeek - (int)today.DayOfWeek + 8) % 7;
            return today.AddDays(daysToAdd == 0 ? 7 : daysToAdd);
        }

    }


    public class BookAppointmentViewModel
    {
        public int TimeSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public Schedule.DayOfWeek ScheduleDay { get; set; }
        public int PatientId { get; set; }

        [ValidateNever]
        public string PatientName { get; set; }
        public string Notes { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> PatientsList { get; set; } = new List<SelectListItem>();
    }

}
