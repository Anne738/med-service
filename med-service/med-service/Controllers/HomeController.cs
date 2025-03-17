using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using med_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace med_service.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult DiagnoseLocalization()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var result = new Dictionary<string, object>();

            // 1. Проверка названия сборки
            result["AssemblyName"] = assembly.GetName().Name;

            // 2. Проверка всех встроенных ресурсов в сборке
            result["EmbeddedResources"] = assembly.GetManifestResourceNames();

            // 3. Проверка текущих культур
            result["CurrentCulture"] = CultureInfo.CurrentCulture.Name;
            result["CurrentUICulture"] = CultureInfo.CurrentUICulture.Name;

            // 4. Проверка физического пути к файлам ресурсов
            var resourcesDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
            var viewResourcesDir = Path.Combine(resourcesDir, "Views", "Home");
            result["ResourcesDirExists"] = Directory.Exists(resourcesDir);
            result["ViewResourcesDirExists"] = Directory.Exists(viewResourcesDir);

            if (Directory.Exists(viewResourcesDir))
            {
                result["ResourceFiles"] = Directory.GetFiles(viewResourcesDir).Select(Path.GetFileName).ToList();
            }

            // 5. Проверка как IStringLocalizer ищет ресурсы
            var factory = HttpContext.RequestServices.GetRequiredService<IStringLocalizerFactory>();
            var type = typeof(HomeController);
            try
            {
                var localizer = factory.Create(type);
                var allStrings = localizer.GetAllStrings(true).ToList();
                result["LocalizerStrings"] = allStrings.Select(s => new { s.Name, s.Value }).ToList();
            }
            catch (Exception ex)
            {
                result["LocalizerError"] = ex.Message;
                result["LocalizerErrorStack"] = ex.StackTrace;
            }

            return Json(result);
        }

        public IActionResult DiagnoseDeep()
        {
            var result = new Dictionary<string, object>();

            // Проверяем ожидаемые имена ресурсов
            var baseName = "med_service.Resources.Views.Home.Index";
            var altName1 = "med_service.Views.Home.Index";
            var altName2 = "med_service.Views.Home.Index.en-US";

            // Пробуем напрямую найти ресурсы с разными названиями
            var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Views", "Home"));

            result["Files"] = files.Select(Path.GetFileName).ToList();

            // Проверяем содержимое файла ресурсов
            try
            {
                var content = System.IO.File.ReadAllText(files.First(f => f.EndsWith("en-US.resx")));
                result["FileContent"] = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content;
            }
            catch (Exception ex)
            {
                result["FileReadError"] = ex.Message;
            }

            // Смотрим, какие провайдеры локализации зарегистрированы
            var locProviders = HttpContext.RequestServices
                .GetServices<Microsoft.Extensions.Localization.IStringLocalizerFactory>()
                .Select(f => f.GetType().FullName)
                .ToList();

            result["LocalizerFactories"] = locProviders;

            // Проверим, как настроен ResourcesPath
            var locOptions = HttpContext.RequestServices
                .GetService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Localization.LocalizationOptions>>();

            if (locOptions != null)
            {
                result["ResourcesPath"] = locOptions.Value.ResourcesPath;
            }

            return Json(result);
        }


        public IActionResult Index()
        {
            return View();
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
    }
}
