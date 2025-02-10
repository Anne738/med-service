namespace med_service.Models
{
    public class Patient : User
    {
        public Patient()
        {
            Role = UserRole.Patient;
        }

        public DateTime DateOfBirth { get; set; }
        public List<Appointment> Appointments { get; set; }
    }
}