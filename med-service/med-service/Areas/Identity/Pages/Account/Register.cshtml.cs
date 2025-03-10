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

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Випадаючі списки
        public SelectList Hospitals { get; set; }
        public SelectList Specializations { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email є обов'язковим полем")]
            [EmailAddress(ErrorMessage = "Введіть коректну email адресу")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Пароль є обов'язковим полем")]
            [StringLength(100, ErrorMessage = "Пароль має бути довжиною від {2} до {1} символів", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Підтвердження пароля")]
            [Compare("Password", ErrorMessage = "Паролі не співпадають")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Ім'я є обов'язковим полем")]
            [Display(Name = "Ім'я")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Прізвище є обов'язковим полем")]
            [Display(Name = "Прізвище")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Роль є обов'язковим полем")]
            [Display(Name = "Роль")]
            public User.UserRole Role { get; set; }

            // Сделать поле датой с nullable, чтобы избежать ошибок при пустом вводе
            [Display(Name = "Дата народження")]
            [DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Лікарня")]
            public int? HospitalId { get; set; }

            [Display(Name = "Спеціалізація")]
            public int? SpecializationId { get; set; }

            [Display(Name = "Стаж роботи (років)")]
            [Range(0, 70, ErrorMessage = "Стаж має бути від 0 до 70 років")]
            public int ExperienceYears { get; set; }
        }

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

            // Дополнительная проверка для поля Дата рождения, если роль - Пациент
            if (Input.Role == Models.User.UserRole.Patient && !Input.DateOfBirth.HasValue)
            {
                ModelState.AddModelError("Input.DateOfBirth", "Укажіть дату народження!");
            }

            // Если роль - Доктор, проверяем больницу и специализацию
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
                    _logger.LogInformation("Користувач створив новий аккаунт з паролем.");

                    try
                    {
                        if (user.Role == Models.User.UserRole.Patient)
                        {
                            var patient = new Patient
                            {
                                UserId = user.Id,
                                DateOfBirth = Input.DateOfBirth ?? DateTime.MinValue
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
                                ExperienceYears = Input.ExperienceYears,
                                HospitalId = Input.HospitalId.Value,
                                SpecializationId = Input.SpecializationId.Value
                            };
                            _context.Doctors.Add(doctor);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Створено запис лікаря для користувача {user.Id}");
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
