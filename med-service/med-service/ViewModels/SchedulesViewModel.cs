using med_service.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class ScheduleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "lblScheduleDayRequired")]
        [Display(Name = "lblScheduleDay")]
        public Schedule.DayOfWeek Day { get; set; }

        [Required(ErrorMessage = "lblScheduleDoctorRequired")]
        [Display(Name = "lblScheduleDoctorId")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "lblScheduleFieldRequired")]
        [Range(0, 23, ErrorMessage = "lblScheduleHourRange")]
        [Display(Name = "lblScheduleWorkDayStart")]
        public int WorkDayStart { get; set; } = 8;

        [Required(ErrorMessage = "lblScheduleFieldRequired")]
        [Range(0, 23, ErrorMessage = "lblScheduleHourRange")]
        [Display(Name = "lblScheduleWorkDayEnd")]
        public int WorkDayEnd { get; set; } = 18;

        [Display(Name = "lblScheduleWorkingHours")]
        public string WorkingHours => $"{WorkDayStart}:00 - {WorkDayEnd}:00";

        public string? DoctorFullName { get; set; }
    }
}
