using med_service.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class DoctorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "UserRequired")]
        [Display(Name = "User")]
        public string UserId { get; set; }

        [ValidateNever]
        public User User { get; set; }

        [Required(ErrorMessage = "HospitalRequired")]
        [Display(Name = "Hospital")]
        public int HospitalId { get; set; }

        [ValidateNever]
        public Hospital Hospital { get; set; }

        [Required(ErrorMessage = "SpecializationRequired")]
        [Display(Name = "Specialization")]
        public int SpecializationId { get; set; }

        [ValidateNever]
        public Specialization Specialization { get; set; }

        [Required(ErrorMessage = "ExperienceRequired")]
        [Range(0, 70, ErrorMessage = "ExperienceRange")]
        [Display(Name = "ExperienceYears")]
        public int ExperienceYears { get; set; }

        [ValidateNever]
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();

        [ValidateNever]
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
