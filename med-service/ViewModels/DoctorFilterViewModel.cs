using med_service.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace med_service.ViewModels
{
    public class DoctorFilterViewModel
    {
        public string DoctorName { get; set; }
        public int? SpecializationId { get; set; }
        public int? HospitalId { get; set; }

        public List<SelectListItem> Specializations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Hospitals { get; set; } = new List<SelectListItem>();

        public List<Doctor> Doctors { get; set; } = new List<Doctor>();

        public bool IsSearched { get; set; }
    }
}
