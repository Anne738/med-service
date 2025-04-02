using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class PatientViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "lblUserRequired")]
        [Display(Name = "lblUser")]
        public string UserId { get; set; }

        [ValidateNever]
        public SelectList UserList { get; set; }

        [Required(ErrorMessage = "lblDateOfBirthRequired")]
        [Display(Name = "lblDateOfBirth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateOfBirth { get; set; }

        [ValidateNever]
        public string FullName { get; set; }
    }
}
