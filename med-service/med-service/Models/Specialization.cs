using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class Specialization
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}