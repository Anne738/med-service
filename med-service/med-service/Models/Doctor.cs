using System.ComponentModel.DataAnnotations.Schema;

namespace med_service.Models
{
    public class Doctor : User
    {
        public Doctor()
        {
            Role = UserRole.Doctor;
        }

        [ForeignKey("Hospital")]
        public long HospitalId { get; set; }
        public Hospital Hospital { get; set; }

        [ForeignKey("Specialization")]
        public long SpecializationId { get; set; }
        public Specialization Specialization { get; set; }

        [ForeignKey("Schedule")]
        public long ScheduleId { get; set; }
        public List<Scgedule> Schedules { get; set; }

        public int ExperienceYears { get; set; }
        public string WorkingHours { get; set; }
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
}
