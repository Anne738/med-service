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

        public int Id { get; set; }
        public List<TimeSlot> AvailableSlots { get; set; }
        public DayOfWeek Day { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
    }

}
