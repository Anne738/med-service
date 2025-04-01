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

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Appointment.AppointmentStatus? status,
                                               string sortOrder, string currentFilter,
                                               string searchString, int? pageIndex)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DoctorSortParam"] = sortOrder == "Doctor" ? "doctor_desc" : "Doctor";
            ViewData["PatientSortParam"] = sortOrder == "Patient" ? "patient_desc" : "Patient";

            ViewBag.Statuses = Enum.GetValues(typeof(Appointment.AppointmentStatus))
                                   .Cast<Appointment.AppointmentStatus>()
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
        public async Task<IActionResult> Details(int? id)
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

            return View(viewModel);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Status,PatientId,DoctorId,TimeSlotId,Notes")] AppointmentViewModel appointmentViewModel)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(
                    selectedTimeSlotId: appointmentViewModel.TimeSlotId,
                    selectedDoctorId: appointmentViewModel.DoctorId,
                    selectedPatientId: appointmentViewModel.PatientId
                );
                return View(appointmentViewModel);
            }

            var appointment = new Appointment
            {
                Status = appointmentViewModel.Status,
                PatientId = appointmentViewModel.PatientId,
                DoctorId = appointmentViewModel.DoctorId,
                TimeSlotId = appointmentViewModel.TimeSlotId,
                Notes = appointmentViewModel.Notes
            };

            var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == appointmentViewModel.TimeSlotId);
            if (timeSlot != null)
            {
                timeSlot.isBooked = true;
                _context.Update(timeSlot);
            }
            else
            {
                ModelState.AddModelError("TimeSlotId", "The selected time slot is invalid.");
                PopulateDropdowns(
                    selectedTimeSlotId: appointmentViewModel.TimeSlotId,
                    selectedDoctorId: appointmentViewModel.DoctorId,
                    selectedPatientId: appointmentViewModel.PatientId
                );
                return View(appointmentViewModel);
            }

            _context.Add(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id); // No TimeSlot Include here

            if (appointment == null) return NotFound();

            var viewModel = new AppointmentViewModel
            {
                Id = appointment.Id,
                Status = appointment.Status,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                TimeSlotId = appointment.TimeSlotId,
                Notes = appointment.Notes //No TimeSlot object here
            };

            //Populate dropdowns for TimeSlot, Doctor, and Patient
            PopulateDropdowns(selectedTimeSlotId: appointment.TimeSlotId,
                              selectedDoctorId: appointment.DoctorId,
                              selectedPatientId: appointment.PatientId);

            return View(viewModel);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentViewModel appointmentViewModel)
        {
            if (id != appointmentViewModel.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                //Repopulate dropdowns for corrections
                PopulateDropdowns(appointmentViewModel.TimeSlotId, appointmentViewModel.DoctorId, appointmentViewModel.PatientId);
                return View(appointmentViewModel);
            }

            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAppointment == null)
            {
                return NotFound();
            }

            existingAppointment.Status = appointmentViewModel.Status;
            existingAppointment.PatientId = appointmentViewModel.PatientId;
            existingAppointment.DoctorId = appointmentViewModel.DoctorId;
            existingAppointment.TimeSlotId = appointmentViewModel.TimeSlotId;
            existingAppointment.Notes = appointmentViewModel.Notes;

            //Handle TimeSlot updates
            if (existingAppointment.TimeSlotId != appointmentViewModel.TimeSlotId)
            {
                var oldSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == existingAppointment.TimeSlotId);
                if (oldSlot != null)
                {
                    oldSlot.isBooked = false; //Free the old slot
                    _context.Update(oldSlot);
                }

                var newSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == appointmentViewModel.TimeSlotId);
                if (newSlot != null)
                {
                    newSlot.isBooked = true; //Reserve the new slot
                    _context.Update(newSlot);
                }
            }

            _context.Update(existingAppointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
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

            return View(viewModel);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        private void PopulateDropdowns(int? selectedTimeSlotId = null, int? selectedDoctorId = null, int? selectedPatientId = null)
        {
            //Include available slots or keep the current (selected) one
            var availableSlots = _context.TimeSlots
                .Where(ts => !ts.isBooked || ts.Id == selectedTimeSlotId) //Show unbooked and selected slot
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                        .ThenInclude(d => d.User)
                .ToList();

            var timeSlotItems = availableSlots.Select(ts => new SelectListItem
            {
                Value = ts.Id.ToString(),
                Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor?.User?.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}",
                Selected = ts.Id == selectedTimeSlotId //Preselect the correct TimeSlot
            }).ToList();

            //Populate ViewBag data for dropdowns
            ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
            ViewBag.DoctorId = new SelectList(
                _context.Doctors.Include(d => d.User),
                "Id",
                "User.LastName",
                selectedDoctorId //Preselect Doctor
            );
            ViewBag.PatientId = new SelectList(
                _context.Patients.Include(p => p.User),
                "Id",
                "User.LastName",
                selectedPatientId //Preselect Patient
            );
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // отримати історію прийомів пацієнта
        [HttpGet("patient/{patientId}/history")]
        public async Task<IActionResult> GetPatientHistory(int patientId, Appointment.AppointmentStatus? status)
        {
            var query = _context.Appointments
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .AsQueryable();



            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var history = await query.AsNoTracking().ToListAsync();

            return View("PatientHistory", history);
            //return history.Any() ? Ok(history) : NotFound("No past appointments found.");
        }

        // отримати історію прийомів лікаря

        [HttpGet("doctor/{doctorId}/history")]
        public async Task<IActionResult> GetDoctorHistory(int doctorId, Appointment.AppointmentStatus? status)
        {
            var query = _context.Appointments
                .Where(a => a.DoctorId == doctorId)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User) 
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var history = await query.AsNoTracking().ToListAsync();
            return View("DoctorHistory", history);
        }


        //return history.Any() ? Ok(history) : NotFound("No past appointments found.");
    }

}
