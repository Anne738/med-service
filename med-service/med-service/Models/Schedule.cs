using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class Schedule
    {
        public enum DayOfWeek
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday
        }

        [Key]
        public long Id { get; set; }
        public List<string> AvailableSlots { get; set; } = new List<string>();
        public DayOfWeek Day { get; set; }

        //Foreign key - doctor
    }

}
