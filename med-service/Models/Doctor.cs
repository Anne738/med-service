using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace med_service.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [ValidateNever]
        public User User { get; set; }

        public int HospitalId { get; set; }
        [ValidateNever]
        public Hospital Hospital { get; set; }

        public int SpecializationId { get; set; }
        [ValidateNever]
        public Specialization Specialization { get; set; }

        public List<Schedule> Schedules { get; set; } = new List<Schedule>();

        public int ExperienceYears { get; set; }

        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
