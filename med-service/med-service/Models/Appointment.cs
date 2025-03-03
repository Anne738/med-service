using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
        [ValidateNever]
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        [ValidateNever]
        public Doctor Doctor { get; set; }

        ///public int TimeSlotId { get; set; }
        //public TimeSlot TimeSlot { get; set; }

        public string Notes { get; set; }
    }
}