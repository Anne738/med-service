// Файл: Models/ViewModels/BookAppointmentViewModel.cs
using med_service.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class BookAppointmentViewModel
    {
        public int TimeSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public Schedule.DayOfWeek ScheduleDay { get; set; }

        [Required(ErrorMessage = "lblPatientRequired")]
        public int PatientId { get; set; }

        [ValidateNever]
        public string PatientName { get; set; }

        [Required(ErrorMessage = "lblNotesRequired")]
        public string Notes { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> PatientsList { get; set; } = new List<SelectListItem>();
    }
}
