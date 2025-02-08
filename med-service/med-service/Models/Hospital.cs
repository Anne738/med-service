using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class Hospital
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}