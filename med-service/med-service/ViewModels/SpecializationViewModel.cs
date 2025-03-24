using med_service.Resources;
using System.ComponentModel.DataAnnotations;

namespace med_service.ViewModels
{
    public class SpecializationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "NameRequired")]
        [StringLength(100, ErrorMessage = "NameLength")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "DescriptionLength")]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}
