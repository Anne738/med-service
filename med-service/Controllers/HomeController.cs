using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using med_service.Data;
using med_service.Models;
using med_service.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;


namespace med_service.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DoctorFilterViewModel
            {
                Specializations = await _db.Specializations
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToListAsync(),

                Hospitals = await _db.Hospitals
                    .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Name })
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            // Ensure the culture passed is valid
            var cultureInfo = new CultureInfo(culture);
            if (!new[] { "en-US", "uk-UA" }.Contains(cultureInfo.Name))
            {
                culture = "en-US";  // Fall back to English if an invalid culture is provided
            }

            // Set the culture cookie
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Redirect to the return URL (ensures the UI updates to the selected language)
            return LocalRedirect(returnUrl ?? "/");
        }


    }
}
