using med_service.Models;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "lblEmailRequired")]
        [EmailAddress(ErrorMessage = "lblEmailInvalid")]
        [Display(Name = "lblEmail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "lblPasswordRequired")]
        [StringLength(100, ErrorMessage = "lblPasswordLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "lblPassword")]
        public string Password { get; set; }

        [Required(ErrorMessage = "lblConfirmPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "lblConfirmPassword")]
        [Compare("Password", ErrorMessage = "lblPasswordMismatch")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "lblFirstNameRequired")]
        [Display(Name = "lblFirstName")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "lblLastNameRequired")]
        [Display(Name = "lblLastName")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "lblRoleRequired")]
        [Display(Name = "lblRole")]
        public User.UserRole Role { get; set; }

        [Display(Name = "lblDateOfBirth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "lblHospital")]
        public int? HospitalId { get; set; }

        [Display(Name = "lblSpecialization")]
        public int? SpecializationId { get; set; }

        [Display(Name = "lblExperienceYears")]
        [Range(0, 70, ErrorMessage = "lblExperienceYearsRange")]
        public int? ExperienceYears { get; set; }
    }
}
