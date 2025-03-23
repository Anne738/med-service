using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class PatientViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Виберіть користувача")]
        [Display(Name = "Користувач")]
        public string UserId { get; set; }

        [ValidateNever]
        public SelectList UserList { get; set; }

        [Required(ErrorMessage = "Вкажіть дату народження")]
        [Display(Name = "Дата народження")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [PastDateValidation(ErrorMessage = "Дата народження не може бути у майбутньому")]
        [AgeValidation(MinAge = 0, MaxAge = 120, ErrorMessage = "Вік має бути від 0 до 120 років")]
        public DateTime DateOfBirth { get; set; }

        [ValidateNever]
        public string FullName { get; set; }
    }

    // Валідатор для перевірки, що дата знаходиться в минулому
    public class PastDateValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime date)
            {
                return date <= DateTime.Today;
            }
            return false;
        }
    }

    // Валідатор для перевірки віку
    public class AgeValidationAttribute : ValidationAttribute
    {
        public int MinAge { get; set; } = 0;
        public int MaxAge { get; set; } = 120;

        public override bool IsValid(object value)
        {
            if (value is DateTime dateOfBirth)
            {
                var age = DateTime.Today.Year - dateOfBirth.Year;

                // Коригуємо вік, якщо день народження ще не настав у поточному році
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }

                return age >= MinAge && age <= MaxAge;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessageString, MinAge, MaxAge);
        }
    }
}
