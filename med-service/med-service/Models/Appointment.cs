using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace med_service.Models
{
    public class Appointment
    {
        public enum AppointmentStatus
        {
            PENDING,
            CONFIRMED,
            COMPLETED,
            CANCELED
        }
        public AppointmentStatus Status { get; set; }

        public long Id { get; set; }

        [ForeignKey("Patient")]
        public long PatientId { get; set; }
        public Patient Patient { get; set; }

        [ForeignKey("Doctor")]
        public long DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; }
    }
}