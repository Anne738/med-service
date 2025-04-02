using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class HospitalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "lblHospitalNameRequired")]
        [MinLength(3, ErrorMessage = "lblHospitalNameTooShort")]
        [StringLength(100, ErrorMessage = "lblHospitalNameTooLong")]
        [Display(Name = "lblHospitalName")]
        public string Name { get; set; }

        [Required(ErrorMessage = "lblHospitalAddressRequired")]
        [MinLength(5, ErrorMessage = "lblHospitalAddressTooShort")]
        [StringLength(200, ErrorMessage = "lblHospitalAddressTooLong")]
        [Display(Name = "lblHospitalAddress")]
        public string Address { get; set; }

        [Required(ErrorMessage = "lblHospitalContactRequired")]
        [RegularExpression(@"^\+[\d\s\-().]{6,20}$", ErrorMessage = "lblHospitalContactInvalid")]
        [Display(Name = "lblHospitalContact")]
        public string Contact { get; set; }
    }
}
