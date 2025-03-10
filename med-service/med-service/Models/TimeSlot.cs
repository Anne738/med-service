using static med_service.Models.Schedule;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace med_service.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }

        public int ScheduleId { get; set; }
        [ValidateNever]
        public Schedule Schedule { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool isBooked { get; set; } = false;

        [ValidateNever]
        public Appointment Appointment { get; set; }
    }
}