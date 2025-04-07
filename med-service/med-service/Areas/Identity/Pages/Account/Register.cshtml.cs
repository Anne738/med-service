using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using med_service.Data;
using med_service.Models;
using Microsoft.Extensions.Logging;
using med_service.ViewModels;

namespace med_service.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _roleManager = roleManager;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Випадаючі списки
        public SelectList Hospitals { get; set; }
        public SelectList Specializations { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            await PrepareDropdownListsAsync();
        }

        private async Task PrepareDropdownListsAsync()
        {
            Hospitals = new SelectList(await _context.Hospitals.ToListAsync(), "Id", "Name");
            Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            _logger.LogInformation($"Registration attempt with role: {Input.Role}");

            if (Input.Role == Models.User.UserRole.Patient && !Input.DateOfBirth.HasValue)
            {
                ModelState.AddModelError("Input.DateOfBirth", "Укажіть дату народження!");
            }

            if (Input.Role == Models.User.UserRole.Doctor)
            {
                if (!Input.HospitalId.HasValue)
                {
                    ModelState.AddModelError("Input.HospitalId", "Виберіть лікарню");
                }
                if (!Input.SpecializationId.HasValue)
                {
                    ModelState.AddModelError("Input.SpecializationId", "Виберіть спеціалізацію");
                }
            }

            if (Input.Role != Models.User.UserRole.Doctor)
            {
                Input.ExperienceYears = null;
                Input.HospitalId = null;
                Input.SpecializationId = null;
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.Role = Input.Role;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Користувач створив новий аккаунт з паролем. Role: {user.Role}");

                    string roleName = user.Role.ToString();
                    var roleResult = await _userManager.AddToRoleAsync(user, roleName);

                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError($"Не вдалося додати роль користувача {roleName}");
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await _userManager.DeleteAsync(user);
                        await PrepareDropdownListsAsync();
                        return Page();
                    }

                    try
                    {
                        if (user.Role == Models.User.UserRole.Patient)
                        {
                            var patient = new Patient
                            {
                                UserId = user.Id,
                                DateOfBirth = Input.DateOfBirth ?? DateTime.Now
                            };
                            _context.Patients.Add(patient);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Створено запис пацієнта для користувача {user.Id}");
                        }
                        else if (user.Role == Models.User.UserRole.Doctor)
                        {
                            var doctor = new Doctor
                            {
                                UserId = user.Id,
                                ExperienceYears = Input.ExperienceYears ?? 0,
                                HospitalId = Input.HospitalId.Value,
                                SpecializationId = Input.SpecializationId.Value
                            };
                            _context.Doctors.Add(doctor);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Створено запис лікаря для користувача {user.Id}");
                        }
                        else if (user.Role == Models.User.UserRole.Admin)
                        {
                            _logger.LogInformation($"Створено адміністратора з ID {user.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Помилка при створенні запису для користувача {user.Id}: {ex.Message}");
                        ModelState.AddModelError(string.Empty, $"Помилка при створенні профілю: {ex.Message}");
                        await _userManager.DeleteAsync(user);
                        await PrepareDropdownListsAsync();
                        return Page();
                    }

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        null,
                        new { area = "Identity", userId = user.Id, code, returnUrl },
                        Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Підтвердіть вашу електронну пошту",
                        $"Будь ласка, підтвердіть ваш обліковий запис <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>натиснувши тут</a>."
                    );

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }

                    await _signInManager.SignInAsync(user, false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                }
            }

            await PrepareDropdownListsAsync();
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Не вдалося створити екземпляр '{nameof(User)}'. Переконайтеся, що він має конструктор без параметрів.");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("Стандартний інтерфейс потребує сховища користувачів з підтримкою електронної пошти.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}
