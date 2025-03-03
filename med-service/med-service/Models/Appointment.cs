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

        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public int TimeslotId { get; set; }
        public TimeSlot TimeSlotId { get; set; }

        public string Notes { get; set; }
    }
}