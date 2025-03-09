// Файл: Models/ViewModels/BookAppointmentViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;

namespace med_service.Models.ViewModels
{
    public class BookAppointmentViewModel
    {
        public int TimeSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public Schedule.DayOfWeek ScheduleDay { get; set; }
        public int PatientId { get; set; }
        public string Notes { get; set; }
        public List<SelectListItem> PatientsList { get; set; } = new List<SelectListItem>();
    }
}
