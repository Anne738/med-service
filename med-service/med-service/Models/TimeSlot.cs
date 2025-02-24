using static med_service.Models.Schedule;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace med_service.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }

        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
    }
}