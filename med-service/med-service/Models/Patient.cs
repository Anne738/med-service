namespace med_service.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string UserId { get; set; } // IdentityUser.Id
        public User User { get; set; }

        public DateTime DateOfBirth { get; set; }
        public List<Appointment> Appointments { get; set; }
    }
}