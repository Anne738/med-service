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
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Appointment.AppointmentStatus? status)
        {
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.TimeSlot)
                .AsQueryable();

            ViewBag.Statuses = Enum.GetValues(typeof(Appointment.AppointmentStatus))
                                   .Cast<Appointment.AppointmentStatus>()
                                   .ToList();

            if (status.HasValue)
            {
                appointments = appointments.Where(a => a.Status == status.Value);
            }

            return View(await appointments.ToListAsync());
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            var availableSlots = _context.TimeSlots
                .Where(ts => !ts.isBooked)
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                        .ThenInclude(d => d.User)
                .ToList();

            // Создаем список с полным описанием слота: день недели + имя врача + время
            var timeSlotItems = availableSlots.Select(ts => new SelectListItem
            {
                Value = ts.Id.ToString(),
                Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor?.User?.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}"
            }).ToList();

            ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
            ViewBag.DoctorId = new SelectList(_context.Doctors.Include(d => d.User), "Id", "User.LastName");
            ViewBag.PatientId = new SelectList(_context.Patients.Include(p => p.User), "Id", "User.LastName");
            return View();
        }


        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Status,Id,PatientId,DoctorId,TimeSlotId,Notes")] Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                var availableSlots = _context.TimeSlots
                    .Where(ts => !ts.isBooked)
                    .Include(ts => ts.Schedule)
                        .ThenInclude(s => s.Doctor)
                            .ThenInclude(d => d.User)
                    .ToList();

                var timeSlotItems = availableSlots.Select(ts => new SelectListItem
                {
                    Value = ts.Id.ToString(),
                    Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor?.User?.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}",
                    Selected = ts.Id == appointment.TimeSlotId
                }).ToList();

                ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
                ViewBag.DoctorId = new SelectList(_context.Doctors.Include(d => d.User), "Id", "User.LastName", appointment.DoctorId);
                ViewBag.PatientId = new SelectList(_context.Patients.Include(p => p.User), "Id", "User.LastName", appointment.PatientId);
                return View(appointment);
            }

            // Обновляем состояние временного слота
            var timeSlot = await _context.TimeSlots.FindAsync(appointment.TimeSlotId);
            if (timeSlot != null)
            {
                timeSlot.isBooked = true;
                _context.Update(timeSlot);
            }

            _context.Add(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var availableSlots = _context.TimeSlots
                .Where(ts => !ts.isBooked || ts.Id == appointment.TimeSlotId)
                .Include(ts => ts.Schedule)
                    .ThenInclude(s => s.Doctor)
                        .ThenInclude(d => d.User)
                .ToList();

            var timeSlotItems = availableSlots.Select(ts => new SelectListItem
            {
                Value = ts.Id.ToString(),
                Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor?.User?.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}",
                Selected = ts.Id == appointment.TimeSlotId
            }).ToList();

            ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Include(d => d.User), "Id", "User.LastName", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients.Include(p => p.User), "Id", "User.LastName", appointment.PatientId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Status,Id,PatientId,DoctorId,TimeSlotId,Notes")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var availableSlots = _context.TimeSlots
                    .Where(ts => !ts.isBooked || ts.Id == appointment.TimeSlotId)
                    .Include(ts => ts.Schedule)
                        .ThenInclude(s => s.Doctor)
                            .ThenInclude(d => d.User)
                    .ToList();

                var timeSlotItems = availableSlots.Select(ts => new SelectListItem
                {
                    Value = ts.Id.ToString(),
                    Text = $"{ts.Schedule.Day} - {ts.Schedule.Doctor?.User?.LastName} - {ts.StartTime:hh\\:mm} - {ts.EndTime:hh\\:mm}",
                    Selected = ts.Id == appointment.TimeSlotId
                }).ToList();

                ViewBag.TimeSlotId = new SelectList(timeSlotItems, "Value", "Text");
                ViewData["DoctorId"] = new SelectList(_context.Doctors.Include(d => d.User), "Id", "User.LastName", appointment.DoctorId);
                ViewData["PatientId"] = new SelectList(_context.Patients.Include(p => p.User), "Id", "User.LastName", appointment.PatientId);
                return View(appointment);
            }

            try
            {
                // Получаем текущие данные из БД
                var existingAppointment = await _context.Appointments.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (existingAppointment != null && existingAppointment.TimeSlotId != appointment.TimeSlotId)
                {
                    // Освобождаем предыдущий слот
                    var oldSlot = await _context.TimeSlots.FindAsync(existingAppointment.TimeSlotId);
                    if (oldSlot != null)
                    {
                        oldSlot.isBooked = false;
                        _context.Update(oldSlot);
                    }

                    // Бронируем новый слот
                    var newSlot = await _context.TimeSlots.FindAsync(appointment.TimeSlotId);
                    if (newSlot != null)
                    {
                        newSlot.isBooked = true;
                        _context.Update(newSlot);
                    }
                }

                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }


        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}