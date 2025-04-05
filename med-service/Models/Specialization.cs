using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class Specialization
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}