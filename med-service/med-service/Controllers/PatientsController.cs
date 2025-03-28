﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using med_service.ViewModels;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .ToListAsync();
            return View(patients);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            var viewModel = new PatientViewModel
            {
                DateOfBirth = DateTime.Today,
                UserList = new SelectList(users, "Id", "FullName")
            };
            return View(viewModel);
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(viewModel.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("UserId", "Користувач не знайдений");

                    var users = _context.Users.Select(u => new
                    {
                        u.Id,
                        FullName = $"{u.FirstName} {u.LastName}"
                    }).ToList();

                    viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                    return View(viewModel);
                }

                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == viewModel.UserId);

                if (existingPatient != null)
                {
                    ModelState.AddModelError("UserId", "Пацієнт з цим користувачем вже існує");

                    var users = _context.Users.Select(u => new
                    {
                        u.Id,
                        FullName = $"{u.FirstName} {u.LastName}"
                    }).ToList();

                    viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                    return View(viewModel);
                }

                var patient = new Patient
                {
                    UserId = viewModel.UserId,
                    DateOfBirth = viewModel.DateOfBirth
                };

                _context.Add(patient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var userList = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            viewModel.UserList = new SelectList(userList, "Id", "FullName", viewModel.UserId);
            return View(viewModel);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            var users = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            var viewModel = new PatientViewModel
            {
                Id = patient.Id,
                UserId = patient.UserId,
                DateOfBirth = patient.DateOfBirth,
                FullName = $"{patient.User?.FirstName} {patient.User?.LastName}",
                UserList = new SelectList(users, "Id", "FullName", patient.UserId)
            };

            return View(viewModel);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _context.Patients.FindAsync(id);
                    if (patient == null)
                    {
                        return NotFound();
                    }

                    if (patient.UserId != viewModel.UserId)
                    {
                        var existingPatient = await _context.Patients
                            .FirstOrDefaultAsync(p => p.UserId == viewModel.UserId && p.Id != id);

                        if (existingPatient != null)
                        {
                            ModelState.AddModelError("UserId", "Пацієнт з цим користувачем вже існує");

                            var users = _context.Users.Select(u => new
                            {
                                u.Id,
                                FullName = $"{u.FirstName} {u.LastName}"
                            }).ToList();

                            viewModel.UserList = new SelectList(users, "Id", "FullName", viewModel.UserId);
                            return View(viewModel);
                        }
                    }

                    patient.UserId = viewModel.UserId;
                    patient.DateOfBirth = viewModel.DateOfBirth;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(viewModel.Id))
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

            var userList = _context.Users.Select(u => new
            {
                u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            }).ToList();

            viewModel.UserList = new SelectList(userList, "Id", "FullName", viewModel.UserId);
            return View(viewModel);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                var hasAppointments = await _context.Appointments
                    .AnyAsync(a => a.PatientId == id);

                if (hasAppointments)
                {
                    TempData["Error"] = "Неможливо видалити пацієнта, оскільки у нього є записи на прийом";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Patients.Remove(patient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        // GET: Patients/GetPatientHistory/5
        public async Task<IActionResult> GetPatientHistory(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return NotFound();
            }

            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.TimeSlot).ThenInclude(ts => ts.Schedule)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.TimeSlot.StartTime)
                .ToListAsync();

            ViewBag.PatientName = $"{patient.User.FirstName} {patient.User.LastName}";
            ViewBag.PatientId = patientId;

            return View(appointments);
        }
    }
}
