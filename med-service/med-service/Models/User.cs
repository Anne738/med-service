using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class User
    {
        public enum UserRole
        {
            Admin,
            Doctor,
            Patient
        }
        public UserRole Role { get; set; }

        [Key]
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
    }
}