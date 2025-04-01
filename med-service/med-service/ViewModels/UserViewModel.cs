using med_service.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using static med_service.Models.User; //Importing UserRole enum

namespace med_service.ViewModels
{
    public class UserViewModel
    {

        public string Id { get; set; } //ID for editing and deleting

        [Required (ErrorMessage = "lblFirstNameRequired")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "lblFirstNameRegularExpression")]
        [Display(Name = "lblFirstName")]
        public string FirstName { get; set; }

        [Required (ErrorMessage = "lblLastNameRequired")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "lblLastNameRegularExpression")]
        [Display(Name = "lblLastName")]
        public string LastName { get; set; }

        [Required (ErrorMessage = "lblEmailAddressRequired")]
        [LocalizedEmailAddress]
        [Display(Name = "lblEmailAddress")]
        public string Email { get; set; }

        [Required(ErrorMessage = "lblPasswordRequired")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "lblPasswordLength")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$",
            ErrorMessage = "lblPasswordRegularExpression")]
        [Display(Name = "lblPassword")]
        public string Password { get; set; }

        [Required(ErrorMessage = "lblConfirmPasswordRequired")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "lblPasswordMatch")]
        [Display(Name = "lblConfirmPassword")]
        public string ConfirmPassword { get; set; }

        [Required (ErrorMessage = "lblUsernameRequired")]
        [MinLength(5, ErrorMessage = "lblUsernameMinLength")]
        [MaxLength(15, ErrorMessage = "lblUsernameMaxLength")]
        [Display(Name = "lblUsername")]
        public string UserName { get; set; }

        [Required (ErrorMessage = "lblRoleRequired")]
        [Display(Name = "lblRole")]
        public UserRole Role { get; set; } //Enum from the User model
    }
}
