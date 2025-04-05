// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using med_service.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace med_service.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<User> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                try
                {
                    // Шукаємо користувача за Email для діагностики
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);

                    // Додамо діагностичну інформацію
                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, $"Діагностика: користувач з Email {Input.Email} не знайдений в базі даних.");
                        return Page();
                    }

                    ModelState.AddModelError(string.Empty,
                        $"Діагностика: Користувач знайдений. ID: {user.Id}, UserName: {user.UserName}, Email: {user.Email}, " +
                        $"NormalizedUserName: {user.NormalizedUserName}, NormalizedEmail: {user.NormalizedEmail}, " +
                        $"HasPassword: {await _signInManager.UserManager.HasPasswordAsync(user)}");

                    var result = await _signInManager.PasswordSignInAsync(user.UserName,
                                                                           Input.Password,
                                                                           Input.RememberMe,
                                                                           lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Користувач увійшов в систему.");
                        return LocalRedirect(returnUrl);
                    }
                    if (result.IsNotAllowed)
                    {
                        // Якщо помилка IsNotAllowed, можливо, Email не підтверджено
                        if (!user.EmailConfirmed)
                        {
                            ModelState.AddModelError(string.Empty, "Email не підтверджено. Будь ласка, підтвердіть Email, перейшовши за посиланням з листа.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Вхід не дозволено для даного користувача.");
                        }
                        return Page();
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Обліковий запис користувача заблоковано.");
                        return RedirectToPage("./Lockout");
                    }

                    ModelState.AddModelError(string.Empty, "Невірні облікові дані. Будь ласка, перевірте Email та пароль.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Помилка при вході: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError(string.Empty, $"Вкладена помилка: {ex.InnerException.Message}");
                    }
                }
            }

            return Page();
        }
    }
}
