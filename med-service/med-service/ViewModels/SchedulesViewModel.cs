using med_service.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class ScheduleViewModel
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("День тижня")]
        public Schedule.DayOfWeek Day { get; set; }

        [Required]
        [DisplayName("ID лікаря")]
        public int DoctorId { get; set; }

        [DisplayName("Початок робочого дня")]
        [Range(0, 23, ErrorMessage = "Значення має бути між 0 та 23")]
        public int WorkDayStart { get; set; } = 8;

        [DisplayName("Кінець робочого дня")]
        [Range(0, 23, ErrorMessage = "Значення має бути між 0 та 23")]
        public int WorkDayEnd { get; set; } = 18;

        [DisplayName("Робочі часи")]
        public string WorkingHours => $"{WorkDayStart}:00 - {WorkDayEnd}:00";

        public string? DoctorFullName { get; set; }
    }
}

