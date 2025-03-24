using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;

namespace med_service.Models
{
    public class Schedule
    {
        public enum DayOfWeek
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday
        }

        public int Id { get; set; }

        public List<TimeSlot> AvailableSlots { get; set; } = new List<TimeSlot>();

        public DayOfWeek Day { get; set; }

        public int DoctorId { get; set; }

        public Doctor Doctor { get; set; }

        public int WorkDayStart { get; set; } = 8;

        public int WorkDayEnd { get; set; } = 18;

        public string WorkingHours => $"{WorkDayStart}:00 - {WorkDayEnd}:00";
    }
}

