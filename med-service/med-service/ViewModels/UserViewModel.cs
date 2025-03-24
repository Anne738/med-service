using System.ComponentModel.DataAnnotations;
using static med_service.Models.User; //Importing UserRole enum

namespace med_service.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } //ID for editing and deleting

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Role")]
        public UserRole Role { get; set; } //Enum from the User model
    }
}
