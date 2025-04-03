using med_service.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static med_service.Models.Appointment; //Importing AppointmentStatus enum

namespace med_service.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }

        [Required (ErrorMessage = "lblStatusRequired")]
        [Display(Name = "lblStatus")]
        public AppointmentStatus Status { get; set; }

        [Required (ErrorMessage = "lblPatientRequired")]
        [Display(Name = "lblPatient")]
        public int PatientId { get; set; }

        [Display(Name = "lblDoctor")]
        public int DoctorId { get; set; }

        [Required (ErrorMessage = "lblTimeSlotRequired")]
        [Display(Name = "lblTimeSlot")]
        public int TimeSlotId { get; set; }

        [Required (ErrorMessage = "lblNotesRequired")]
        [Display(Name = "lblNotes")]
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
