﻿using System;
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
            // Get all appointments including related Patient and Doctor data
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // Pass all possible status values to the view for the filter dropdown
            ViewBag.Statuses = Enum.GetValues(typeof(Appointment.AppointmentStatus))
                                   .Cast<Appointment.AppointmentStatus>()
                                   .ToList();

            // If a status is selected, filter the appointments
            if (status.HasValue)
            {
                appointments = appointments.Where(a => a.Status == status.Value);
            }

            // Return the filtered list of appointments to the view
            return View(await appointments.ToListAsync());
        }


        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            ViewBag.DoctorId = new SelectList(_context.Doctors, "Id", "Id");
            ViewBag.PatientId = new SelectList(_context.Patients, "Id", "Id");
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Status,Id,PatientId,DoctorId,AppointmentDate,Notes")] Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                return View(appointment);
            }

            // Подтягиваем Doctor и Patient из базы по Id
            if (appointment.DoctorId > 0)
            {
                var doc = await _context.Doctors.FindAsync(appointment.DoctorId);
                if (doc == null)
                {
                    ModelState.AddModelError("DoctorId", "Доктор с указанным Id не найден.");
                    ViewBag.DoctorId = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                    ViewBag.PatientId = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                    return View(appointment);
                }
                appointment.Doctor = doc;
            }

            if (appointment.PatientId > 0)
            {
                var pat = await _context.Patients.FindAsync(appointment.PatientId);
                if (pat == null)
                {
                    ModelState.AddModelError("PatientId", "Пациент с указанным Id не найден.");
                    ViewBag.DoctorId = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                    ViewBag.PatientId = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                    return View(appointment);
                }
                appointment.Patient = pat;
            }

            _context.Add(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Status,Id,PatientId,DoctorId,AppointmentDate,Notes")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                return View(appointment);
            }

            // Подтягиваем Doctor и Patient из базы по Id
            if (appointment.DoctorId > 0)
            {
                var doc = await _context.Doctors.FindAsync(appointment.DoctorId);
                if (doc == null)
                {
                    ModelState.AddModelError("DoctorId", "Доктор с указанным Id не найден.");
                    ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                    ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                    return View(appointment);
                }
                appointment.Doctor = doc;
            }

            if (appointment.PatientId > 0)
            {
                var pat = await _context.Patients.FindAsync(appointment.PatientId);
                if (pat == null)
                {
                    ModelState.AddModelError("PatientId", "Пациент с указанным Id не найден.");
                    ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Id", appointment.DoctorId);
                    ViewData["PatientId"] = new SelectList(_context.Patients, "Id", "Id", appointment.PatientId);
                    return View(appointment);
                }
                appointment.Patient = pat;
            }

            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

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