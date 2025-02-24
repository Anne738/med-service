using System.ComponentModel.DataAnnotations.Schema;

namespace med_service.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }

        public int HospitalId { get; set; }
        public Hospital Hospital { get; set; }

        public int SpecializationId { get; set; }
        public Specialization Specialization { get; set; }

        //public int ScheduleId { get; set; }
        public List<Schedule> Schedules { get; set; }

        public int ExperienceYears { get; set; }
        public string WorkingHours { get; set; }

        public List<Appointment> Appointments { get; set; }
    }
}

