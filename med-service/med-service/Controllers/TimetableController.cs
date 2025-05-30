﻿using med_service.Data;
using med_service.Models;
using med_service.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace med_service.Controllers
{
    public class TimetablesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<TimetablesController> _localizer;

        public TimetablesController(ApplicationDbContext db, UserManager<User> userManager, IStringLocalizer<TimetablesController> localizer)
        {
            _db = db;
            _userManager = userManager;
            _localizer = localizer;
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
                TempData["Error"] = _localizer["lblPleaseLogin"];
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
                    TempData["Error"] = _localizer["lblOnlyPatientsOrAdmins"];
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
                TempData["Error"] = _localizer["lblSlotBooked"];
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
                    TempData["FormError"] = _localizer["lblPleaseLogin"];
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
                        TempData["FormError"] = _localizer["lblOnlyPatientsOrAdmins"];
                        return RedirectToAction("Doctor", new { id = model.DoctorId });
                    }

                    // Перевіряємо, чи PatientId відповідає поточному користувачу
                    if (model.PatientId != patient.Id)
                    {
                        ModelState.AddModelError("PatientId", _localizer["lblOnlyOwnAccount"]);
                        TempData["FormError"] = _localizer["lblOnlyOwnAccount"];
                    }
                }

                // Перевіряємо базові валідації моделі
                if (!ModelState.IsValid)
                {
                    var errorMessages = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                    TempData["FormError"] = _localizer["lblValidationError"] + ": " + errorMessages;
                }
                else
                {
                    var timeSlot = await _db.TimeSlots
                        .Include(ts => ts.Schedule)
                        .FirstOrDefaultAsync(ts => ts.Id == model.TimeSlotId);

                    if (timeSlot == null)
                    {
                        ModelState.AddModelError("TimeSlotId", _localizer["lblSlotNotFound"]);
                        TempData["FormError"] = _localizer["lblSlotNotFound"];
                    }
                    else
                    {
                        // Перевіряємо, чи обрано пацієнта
                        if (model.PatientId <= 0)
                        {
                            ModelState.AddModelError("PatientId", _localizer["lblSelectPatient"]);
                            TempData["FormError"] = _localizer["lblSelectPatient"];
                        }
                        else
                        {
                            try
                            {
                                // Перевіряємо, чи існує вже запис на цей слот
                                var existingAppointment = await _db.Appointments
                                    .FirstOrDefaultAsync(a => a.TimeSlotId == timeSlot.Id);

                                if (existingAppointment != null)
                                {
                                    // Якщо запис існує, оновлюємо його
                                    existingAppointment.PatientId = model.PatientId;
                                    existingAppointment.DoctorId = timeSlot.Schedule.DoctorId;
                                    existingAppointment.Status = Appointment.AppointmentStatus.PENDING;
                                    existingAppointment.Notes = model.Notes;
                                    _db.Appointments.Update(existingAppointment);
                                }
                                else
                                {
                                    // Створюємо новий запис
                                    var appointment = new Appointment
                                    {
                                        PatientId = model.PatientId,
                                        DoctorId = timeSlot.Schedule.DoctorId,
                                        TimeSlotId = timeSlot.Id,
                                        Status = Appointment.AppointmentStatus.PENDING,
                                        Notes = model.Notes
                                    };
                                    _db.Appointments.Add(appointment);
                                }

                                // Позначаємо слот як зайнятий
                                timeSlot.isBooked = true;
                                _db.TimeSlots.Update(timeSlot);

                                await _db.SaveChangesAsync();

                                // Перенаправляем пациента на список его записей, а админа - на детали записи
                                if (isAdmin)
                                {
                                    var latestAppointment = await _db.Appointments
                                        .Where(a => a.TimeSlotId == timeSlot.Id)
                                        .FirstOrDefaultAsync();
                                    return RedirectToAction("Index", "Appointments");
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
                                ModelState.AddModelError("", _localizer["lblSaveError"] + ex.Message);
                                TempData["FormError"] = _localizer["lblSaveError"] + ex.Message;

                                if (ex.InnerException != null)
                                {
                                    TempData["FormError"] += $" {_localizer["lblDetails"]}: {ex.InnerException.Message}";
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
                ModelState.AddModelError("", _localizer["lblGeneralError"] + ex.Message);
                TempData["FormError"] = _localizer["lblGeneralError"] + ex.Message;

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
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); 
            }

            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            bool isDoctor = await _userManager.IsInRoleAsync(currentUser, "Doctor");

            if (isAdmin)
            {
                return RedirectToAction("Index", "Appointments");
            }

            if (isDoctor)
            {
                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == currentUser.Id);

                if (doctor == null)
                {
                    TempData["Error"] = _localizer["lblDoctorProfileNotFound"];
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
                var patient = await _db.Patients
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

                if (patient == null)
                {
                    TempData["Error"] = _localizer["lblPatientProfileRequired"];
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
            model.Specializations = await _db.Specializations
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            model.Hospitals = await _db.Hospitals
                .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Name })
                .ToListAsync();

            var doctorsQuery = _db.Doctors
                .Include(d => d.User)
                .Include(d => d.Hospital)
                .Include(d => d.Specialization)
                .AsQueryable();

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

            model.Doctors = await doctorsQuery.ToListAsync();
            model.IsSearched = true;

            if (model.Doctors.Count == 1)
            {
                return RedirectToAction("Doctor", new { id = model.Doctors[0].Id });
            }

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
}
