using med_service.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static med_service.Models.Appointment; //Importing AppointmentStatus enum

namespace med_service.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }

        [Required (ErrorMessage = "The Status field is required")]
        [Display(Name = "lblStatus")]
        public AppointmentStatus Status { get; set; }

        [Required (ErrorMessage = "The Patient field is required")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Required (ErrorMessage = "The Doctor field is required")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required (ErrorMessage = "The Time Slot field is required")]
        [Display(Name = "Time Slot")]
        public int TimeSlotId { get; set; }

        [Required (ErrorMessage = "The Notes field is required")]
        [Display(Name = "Notes")]
        public string Notes { get; set; }


        //This will be used for editing, hence keeping only the ID in the ViewModel
        [ValidateNever]
        public TimeSlot TimeSlot { get; set; }
        //Full name of the Patient (FirstName + LastName)
        [ValidateNever]
        public string PatientName { get; set; }
        //Full name of the Doctor (FirstName + LastName)
        [ValidateNever]
        public string DoctorName { get; set; }
    }
}
