using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace med_service.Models
{
    public class User : IdentityUser
    {
        public enum UserRole
        {
            Admin,
            Doctor,
            Patient
        }
        public UserRole Role { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}