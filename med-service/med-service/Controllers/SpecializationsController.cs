﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using Microsoft.AspNetCore.Authorization;

namespace med_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SpecializationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpecializationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Specializations
        public async Task<IActionResult> Index()
        {
            return View(await _context.Specializations.ToListAsync());
        }

        // GET: Specializations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (specialization == null)
            {
                return NotFound();
            }

            return View(specialization);
        }

        // GET: Specializations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Specializations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Specialization specialization)
        {
            if (ModelState.IsValid)
            {
                _context.Add(specialization);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        // GET: Specializations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations.FindAsync(id);
            if (specialization == null)
            {
                return NotFound();
            }
            return View(specialization);
        }

        // POST: Specializations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Specialization specialization)
        {
            if (id != specialization.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialization);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecializationExists(specialization.Id))
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
            return View(specialization);
        }

        // GET: Specializations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (specialization == null)
            {
                return NotFound();
            }

            return View(specialization);
        }

        // POST: Specializations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var specialization = await _context.Specializations.FindAsync(id);
            if (specialization != null)
            {
                _context.Specializations.Remove(specialization);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SpecializationExists(int id)
        {
            return _context.Specializations.Any(e => e.Id == id);
        }
    }
}
