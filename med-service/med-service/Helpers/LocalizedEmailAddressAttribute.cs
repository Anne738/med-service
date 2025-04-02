using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using med_service.Resources;
using med_service.ViewModels;

namespace med_service.Helpers
{
    public class LocalizedEmailAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let [Required] handle null cases

            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(value))
            {
                //Get HttpContext from DI
                var httpContextAccessor = validationContext.GetService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor?.HttpContext;

                //Get localizer service for UserViewModel.resx
                var localizer = httpContext?.RequestServices.GetService<IStringLocalizer<UserViewModel>>();

                //Get localized error message or fallback
                var errorMessage = localizer?["lblEmailAddressInvalid"] ?? "Invalid email address format";

                return new ValidationResult(errorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
