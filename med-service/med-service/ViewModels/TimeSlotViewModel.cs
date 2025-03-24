using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class TimeSlotViewModel
    {
        public int Id { get; set; }

        //[Required(ErrorMessageResourceType = typeof(Resources.SharedResource), ErrorMessageResourceName = "RequiredField")]
        [DisplayName("Schedule")]
        public int ScheduleId { get; set; }

        [ValidateNever]
        [DisplayName("Schedule")]
        public string ScheduleString { get; set; }

        //[Required(ErrorMessageResourceType = typeof(Resources.SharedResource), ErrorMessageResourceName = "RequiredField")]
        [DisplayName("StartTime")]
        public TimeSpan StartTime { get; set; }

        [DisplayName("EndTime")]
        public TimeSpan EndTime { get; set; }

        [DisplayName("IsBooked")]
        public bool IsBooked { get; set; } = false;

        [ValidateNever]
        [DisplayName("Time")]
        public string TimeString => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
