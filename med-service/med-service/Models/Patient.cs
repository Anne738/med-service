using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace med_service.Models
{
    public class Patient
    {
        public Patient()
        {
            Appointments = new List<Appointment>();
        }
        public int Id { get; set; }
        public string UserId { get; set; } // IdentityUser.Id

        [ValidateNever]
        public User User { get; set; }

        public DateTime DateOfBirth { get; set; }
        public List<Appointment> Appointments { get; set; }
    }
}