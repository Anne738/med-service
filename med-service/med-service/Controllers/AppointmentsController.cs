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
using Microsoft.AspNetCore.Identity;
using med_service.Helpers;
using System.Numerics;
using Microsoft.Extensions.Localization;
using static med_service.Models.Appointment;

namespace med_service.Controllers
{

    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<AppointmentsController> _localizer;

        public AppointmentsController(ApplicationDbContext context, IStringLocalizer<AppointmentsController> localizer, UserManager<User> userManager)
        {
            _context = context;
            _localizer = localizer;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(Appointment.AppointmentStatus? status,
                                               string sortOrder, string currentFilter,
                                               string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DoctorSortParam"] = sortOrder == "Doctor" ? "doctor_desc" : "Doctor";
            ViewData["PatientSortParam"] = sortOrder == "Patient" ? "patient_desc" : "Patient";

            ViewBag.Statuses = Enum.GetValues(typeof(Appointment.AppointmentStatus))
                       .Cast<Appointment.AppointmentStatus>()
                       .Where(s => s != Appointment.AppointmentStatus.CANCELED) //Exclude CANCELED
                       .ToList();

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                .AsQueryable();

            if (status.HasValue)
            {
                appointments = appointments.Where(a => a.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                appointments = appointments.Where(a =>
                    (a.Patient.User.FirstName + " " + a.Patient.User.LastName).Contains(searchString) ||
                    (a.Doctor.User.FirstName + " " + a.Doctor.User.LastName).Contains(searchString) ||
                    a.Notes.Contains(searchString)
                );
            }

            var appointmentQuery = appointments.Select(a => new AppointmentViewModel
            {
                Id = a.Id,
                Status = a.Status,
                PatientId = a.PatientId,
                PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.User.FirstName + " " + a.Doctor.User.LastName,
                TimeSlotId = a.TimeSlotId,
                Notes = a.Notes
            });

            appointmentQuery = sortOrder switch
            {
                "Doctor" => appointmentQuery.OrderBy(a => a.DoctorName),
                "doctor_desc" => appointmentQuery.OrderByDescending(a => a.DoctorName),
                "Patient" => appointmentQuery.OrderBy(a => a.PatientName),
                "patient_desc" => appointmentQuery.OrderByDescending(a => a.PatientName),
                _ => appointmentQuery.OrderBy(a => a.Status)
            };

            int pageSize = 7;
            var paginatedList = await PaginatedList<AppointmentViewModel>.CreateAsync(appointmentQuery, pageIndex ?? 1, pageSize);

            var paginationInfo = new PaginationViewModel
            {
                PageIndex = paginatedList.PageIndex,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                ActionName = nameof(Index),
                ControllerName = "Appointments"
            };

            ViewBag.PaginationInfo = paginationInfo;

            return View(paginatedList.Items);
        }

        // GET: Appointments/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                    .ThenInclude(ts => ts.Schedule) // Ensure Schedule (and Day) is included
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var viewModel = new AppointmentViewModel
            {
                Id = appointment.Id,
                Status = appointment.Status,
                PatientId = appointment.PatientId,
                PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                DoctorId = appointment.DoctorId,
                DoctorName = $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                TimeSlotId = appointment.TimeSlotId,
                Notes = appointment.Notes,
                TimeSlot = appointment.TimeSlot //Pass the entire TimeSlot object, including Schedule and Day
            };

            return PartialView("~/Views/Appointments/_Details.cshtml", viewModel);
        }

        // GET: Appointments/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return PartialView("~/Views/Appointments/_Create.cshtml", new AppointmentViewModel());
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Status,PatientId,TimeSlotId,Notes")] AppointmentViewModel appointmentViewModel)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(selectedTimeSlotId: appointmentViewModel.TimeSlotId, selectedPatientId: appointmentViewModel.PatientId);
                return PartialView("~/Views/Appointments/_Create.cshtml", appointmentViewModel);
            }

            // Fetch the selected TimeSlot and its associated doctor
            var timeSlot = await _context.TimeSlots
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(ts => ts.Id == appointmentViewModel.TimeSlotId);

            if (timeSlot == null || timeSlot.Schedule?.Doctor == null)
            {
                ModelState.AddModelError("TimeSlotId", "The selected time slot is invalid or does not have an associated doctor.");
                PopulateDropdowns(selectedTimeSlotId: appointmentViewModel.TimeSlotId, selectedPatientId: appointmentViewModel.PatientId);
                return PartialView("~/Views/Appointments/_Create.cshtml", appointmentViewModel);
            }

            // Automatically assign the DoctorId based on TimeSlot
            var doctorId = timeSlot.Schedule.Doctor.Id;

            var appointment = new Appointment
            {
                Status = appointmentViewModel.Status,
                PatientId = appointmentViewModel.PatientId,
                DoctorId = doctorId, // Set the DoctorId automatically
                TimeSlotId = appointmentViewModel.TimeSlotId,
                Notes = appointmentViewModel.Notes
            };

            // Mark the TimeSlot as booked
            timeSlot.isBooked = true;
            _context.Update(timeSlot);

            // Save the appointment
            _context.Add(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.TimeSlot)
                    .ThenInclude(ts => ts.Schedule)
                        .ThenInclude(s => s.Doctor)
                            .ThenInclude(d => d.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var viewModel = new AppointmentViewModel
            {
                Id = appointment.Id,
                Status = appointment.Status,
                PatientId = appointment.PatientId,
                TimeSlotId = appointment.TimeSlotId,
                DoctorName = appointment.TimeSlot?.Schedule?.Doctor?.User != null
                    ? $"{appointment.TimeSlot.Schedule.Doctor.User.FirstName} {appointment.TimeSlot.Schedule.Doctor.User.LastName}"
                    : "Unavailable", // Dynamically derive doctor's full name
                Notes = appointment.Notes
            };

            // Populate dropdowns (no manual selection for DoctorId)
            PopulateDropdowns(appointment.TimeSlotId, appointment.PatientId);

            return PartialView("~/Views/Appointments/_Edit.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, AppointmentViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new { success = false, message = "Invalid appointment ID." });
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.TimeSlotId, model.PatientId);
                return PartialView("~/Views/Appointments/_Edit.cshtml", model);
            }

            // Fetch the existing appointment
            var appointment = await _context.Appointments
                .Include(a => a.TimeSlot) // Include TimeSlot for update logic
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." });
            }

            // Handle status change to CANCELED
            if (model.Status == Appointment.AppointmentStatus.CANCELED)
            {
                if (appointment.TimeSlot != null)
                {
                    var associatedTimeSlot = await _context.TimeSlots
                        .FirstOrDefaultAsync(ts => ts.Id == appointment.TimeSlotId);

                    if (associatedTimeSlot != null)
                    {
                        associatedTimeSlot.isBooked = false; // Mark TimeSlot as available
                        _context.Update(associatedTimeSlot);
                    }
                }

                // Delete the appointment since it's canceled
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index)); // Return immediately after deletion
            }

            // Check if TimeSlotId has changed
            bool timeSlotChanged = appointment.TimeSlotId != model.TimeSlotId;

            if (timeSlotChanged)
            {
                // Reset the old TimeSlot's isBooked flag
                var oldTimeSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == appointment.TimeSlotId);
                if (oldTimeSlot != null)
                {
                    oldTimeSlot.isBooked = false; // Un-book the old TimeSlot
                    _context.Update(oldTimeSlot);
                }

                // Set the new TimeSlot's isBooked flag
                var newTimeSlot = await _context.TimeSlots
                    .Include(ts => ts.Schedule) // Include Schedule to derive DoctorId
                    .ThenInclude(s => s.Doctor)
                    .FirstOrDefaultAsync(ts => ts.Id == model.TimeSlotId);

                if (newTimeSlot != null && newTimeSlot.Schedule?.Doctor != null)
                {
                    newTimeSlot.isBooked = true; // Mark the new TimeSlot as booked
                    appointment.DoctorId = newTimeSlot.Schedule.Doctor.Id; // Safely derive DoctorId from TimeSlot's Schedule
                    _context.Update(newTimeSlot);
                }
                else
                {
                    ModelState.AddModelError("TimeSlotId", "The selected time slot is invalid or does not have an associated doctor.");
                    PopulateDropdowns(model.TimeSlotId, model.PatientId);
                    return PartialView("~/Views/Appointments/_Edit.cshtml", model);
                }
            }

            // Update appointment fields
            appointment.Status = model.Status;
            appointment.PatientId = model.PatientId;
            appointment.TimeSlotId = model.TimeSlotId;
            appointment.Notes = model.Notes;

            // Save changes
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var viewModel = new AppointmentViewModel
            {
                Id = appointment.Id,
                Status = appointment.Status,
                PatientId = appointment.PatientId,
                PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
                DoctorId = appointment.DoctorId,
                DoctorName = $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                TimeSlotId = appointment.TimeSlotId,
                Notes = appointment.Notes,
                TimeSlot = appointment.TimeSlot
            };

            return PartialView("~/Views/Appointments/_Delete.cshtml", viewModel);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                //Free the TimeSlot
                var slot = await _context.TimeSlots.FindAsync(appointment.TimeSlotId);
                if (slot != null)
                {
                    slot.isBooked = false; //Mark slot as available
                    _context.Update(slot);
                }

                //Remove the Appointment from the database
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index)); //Redirect to the list of appointments
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return Json(new { success = false, message = _localizer["AppointmentNotFound"].Value });
            }

            if (!Enum.TryParse<Appointment.AppointmentStatus>(status, out var newStatus))
            {
                return Json(new { success = false, message = _localizer["InvalidStatus"].Value });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = _localizer["Unauthorized"].Value });
            }

            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            bool isDoctor = await _userManager.IsInRoleAsync(currentUser, "Doctor");

            if (!isAdmin)
            {
                if (isDoctor)
                {
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUser.Id);
                    if (doctor == null || doctor.Id != appointment.DoctorId)
                    {
                        return Json(new { success = false, message = _localizer["AccessDenied"].Value });
                    }
                }
                else
                {
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                    if (patient == null || patient.Id != appointment.PatientId)
                    {
                        return Json(new { success = false, message = _localizer["AccessDenied"].Value });
                    }

                    if (newStatus != Appointment.AppointmentStatus.CANCELED)
                    {
                        return Json(new { success = false, message = _localizer["OnlyCancelAllowed"].Value });
                    }
                }
            }

            if (newStatus == Appointment.AppointmentStatus.CANCELED && appointment.TimeSlot != null)
            {
                appointment.TimeSlot.isBooked = false;
            }

            appointment.Status = newStatus;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = GetStatusUpdateMessage(newStatus)
            });
        }


        private string GetStatusUpdateMessage(Appointment.AppointmentStatus status)
        {
            return status switch
            {
                Appointment.AppointmentStatus.CONFIRMED => _localizer["StatusConfirmedMessage"].Value,
                Appointment.AppointmentStatus.COMPLETED => _localizer["StatusCompletedMessage"].Value,
                Appointment.AppointmentStatus.CANCELED => _localizer["StatusCanceledMessage"].Value,
                _ => _localizer["StatusUpdatedMessage"].Value
            };
        }

        private void PopulateDropdowns(int? selectedTimeSlotId = null, int? selectedPatientId = null)
        {
            var availableSlots = _context.TimeSlots
                .Where(ts => !ts.isBooked || ts.Id == selectedTimeSlotId)
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                        .ThenInclude(d => d.User)
                .ToList();

            var timeSlotItems = availableSlots.Select(ts => new SelectListItem
            {
                Value = ts.Id.ToString(),
                Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor.User.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}",
                Selected = ts.Id == selectedTimeSlotId
            }).ToList();

            ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
            ViewBag.PatientId = new SelectList(
                _context.Patients.Include(p => p.User),
                "Id",
                "User.LastName",
                selectedPatientId
            );
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // отримати історію записів пацієнта

        //GET: Patients/PatientHistory/5
        [Authorize(Roles = "Patient")]
        [HttpGet("Patient/MyHistory")]
        public async Task<IActionResult> PatientHistory()
        {
            var userId = _userManager.GetUserId(User); //отримуємо ID залогіненого юзера

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
            {
                return NotFound();
            }

            var appointments = _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.TimeSlot).ThenInclude(ts => ts.Schedule)
                .Where(a => a.PatientId == patient.Id)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .AsQueryable();

            ViewBag.PatientName = $"{patient.User.FirstName} {patient.User.LastName}";
            ViewBag.PatientId = patient.Id;

            //return View(appointments);
            var history = await appointments.AsNoTracking().ToListAsync();
            return View("PatientHistory", history);
        }

        // отримати історію прийомів лікаря
        [Authorize(Roles = "Doctor")]
        [HttpGet("Doctor/MyHistory")]
        public async Task<IActionResult> MyHistory(Appointment.AppointmentStatus? status)
        {
            var userId = _userManager.GetUserId(User); // отримуємо ID залогіненого користувача

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
            {
                return NotFound();
            }

            var query = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var history = await query.AsNoTracking().ToListAsync();

            ViewBag.DoctorName = $"{doctor.User.FirstName} {doctor.User.LastName}";
            ViewBag.DoctorId = doctor.Id;

            return View("DoctorHistory", history);
        }

    }

}
