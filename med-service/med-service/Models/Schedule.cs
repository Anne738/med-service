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

        [ValidateNever]
        public Doctor Doctor { get; set; }

        [DisplayName("Начало рабочего дня")]
        [Range(0, 23)]
        public int WorkDayStart { get; set; } = 8;

        [DisplayName("Конец рабочего дня")]
        [Range(0, 23)]
        public int WorkDayEnd { get; set; } = 18;

        [NotMapped]
        [DisplayName("Рабочие часы")]
        public string WorkingHours
        {
            get { return $"{WorkDayStart}:00 - {WorkDayEnd}:00"; }
        }
    }
}
