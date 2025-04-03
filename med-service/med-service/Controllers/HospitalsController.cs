using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using med_service.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HospitalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<HospitalsController> _localizer;

        public HospitalsController(ApplicationDbContext context, IStringLocalizer<HospitalsController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: Hospitals
        public async Task<IActionResult> Index()
        {
            var hospitals = await _context.Hospitals.ToListAsync();
            var vmList = hospitals.Select(h => new HospitalViewModel
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                Contact = h.Contact
            }).ToList();

            return View(vmList);
        }

        // GET: Hospitals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var hospital = await _context.Hospitals.FirstOrDefaultAsync(m => m.Id == id);
            if (hospital == null) return NotFound();

            var model = new HospitalViewModel
            {
                Id = hospital.Id,
                Name = hospital.Name,
                Address = hospital.Address,
                Contact = hospital.Contact
            };

            return PartialView("~/Views/Hospitals/_Details.cshtml", model);
        }

        // GET: Hospitals/Create
        public IActionResult Create()
        {
            return PartialView("~/Views/Hospitals/_Create.cshtml", new HospitalViewModel());
        }

        // POST: Hospitals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HospitalViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hospital = new Hospital
                {
                    Name = model.Name,
                    Address = model.Address,
                    Contact = model.Contact
                };

                _context.Add(hospital);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return PartialView("~/Views/Hospitals/_Create.cshtml", model);
        }

        // GET: Hospitals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();

            var model = new HospitalViewModel
            {
                Id = hospital.Id,
                Name = hospital.Name,
                Address = hospital.Address,
                Contact = hospital.Contact
            };

            return PartialView("~/Views/Hospitals/_Edit.cshtml", model);
        }

        // POST: Hospitals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HospitalViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var hospital = await _context.Hospitals.FindAsync(id);
                    if (hospital == null) return NotFound();

                    hospital.Name = model.Name;
                    hospital.Address = model.Address;
                    hospital.Contact = model.Contact;

                    _context.Update(hospital);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HospitalExists(model.Id)) return NotFound();
                    else throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", _localizer["UpdateError"]);
                }
            }

            return PartialView("~/Views/Hospitals/_Edit.cshtml", model);
        }

        // GET: Hospitals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var hospital = await _context.Hospitals.FirstOrDefaultAsync(m => m.Id == id);
            if (hospital == null) return NotFound();

            var model = new HospitalViewModel
            {
                Id = hospital.Id,
                Name = hospital.Name,
                Address = hospital.Address,
                Contact = hospital.Contact
            };

            return PartialView("~/Views/Hospitals/_Delete.cshtml", model);
        }

        // POST: Hospitals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital != null)
            {
                _context.Hospitals.Remove(hospital);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HospitalExists(int id)
        {
            return _context.Hospitals.Any(e => e.Id == id);
        }
    }
}
