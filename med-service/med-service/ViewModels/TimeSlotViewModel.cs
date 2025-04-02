using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class TimeSlotViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "lblScheduleRequired")]
        [Display(Name = "lblSchedule")]
        public int ScheduleId { get; set; }

        [ValidateNever]
        [Display(Name = "lblSchedule")]
        public string ScheduleString { get; set; }

        [Required(ErrorMessage = "lblStartTimeRequired")]
        [Display(Name = "lblStartTime")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "lblEndTime")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "lblIsBooked")]
        public bool IsBooked { get; set; } = false;

        [ValidateNever]
        [Display(Name = "lblTime")]
        public string TimeString => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
