using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using med_service.Models;

namespace med_service.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Specialization> Specializations { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region User
            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();
            #endregion

            #region Appointment
            // primary key
            modelBuilder.Entity<Appointment>()
                .HasKey(a => a.Id);

            // one-to-many
            // Один пацієнт може мати кілька прийомів
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade); // При видаленні пацієнта, его прийоми видаляються

            // one-to-many
            // Один лікарь може мати декілька прийомів.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // При видаленні лікаря, прийоми не видаляються автоматично.

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.TimeSlot)
                .WithOne(ts => ts.Appointment)
                .HasForeignKey<Appointment>(a => a.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion

            #region Doctor
            // primary key
            modelBuilder.Entity<Doctor>()
                .HasKey(d => d.Id);

            // one-to-many
            // Один госпіталь може мати декілька лікарів.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Hospital)
                .WithMany(h => h.Doctors)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.Restrict); // Запобігаємо каскадне видалення лікаря при видаленні госпіталю.
                                                    
            // One-to-many relationship between Doctor and Specialization.
            // Якщо в сутності Specialization немає колекції лікарів, використовуємо WithMany без параметрів.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Specialization)
                .WithMany()
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // One-to-many relationship between Doctor and Schedule.
            // Один лікар може мати декілька розкладів.
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Schedules)
                .WithOne(s => s.Doctor)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade); // При видаленні лікаря, його розклади видаляються.
            #endregion
            
            #region Patient
            // primary key
            modelBuilder.Entity<Patient>()
                .HasKey(p => p.Id);
            // One-to-one relationship Patient and User (Identity).
            // Кожен пацієнт асоційований з одним користувачем.
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // При видаленні користувача, видаляється і пацієнт.
            #endregion
            
            #region Hospital
            // primary key
            modelBuilder.Entity<Hospital>()
                .HasKey(h => h.Id);
            #endregion
            
            #region Schedule
            // primary key
            modelBuilder.Entity<Schedule>()
                .HasKey(s => s.Id);
            // One-to-many Schedule TimeSlot.
            // Один розклад може мати декілька доступних часових інтервалів.
            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.AvailableSlots)
                .WithOne(ts => ts.Schedule)
                .HasForeignKey(ts => ts.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade); // При видаленні розкладу, його часові інтервали видаляються.
            #endregion
            
            /*#region TimeSlot
            // primary key
            modelBuilder.Entity<TimeSlot>()
                .HasKey(ts => ts.Id);
            // One-to-many TimeSlot Doctor.
            // Якщо потрібен швидкий доступ до лікаря для часового інтервалу, зв'язуємо напряму.
            // Використовуємо WithMany без параметрів, якщо в Doctor не визначена колекція TimeSlot.
            modelBuilder.Entity<TimeSlot>()
                .HasOne(ts => ts.Doctor)
                .WithMany()
                .HasForeignKey(ts => ts.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion*/

            #region Specialization
            // primary key
            modelBuilder.Entity<Specialization>()
                .HasKey(s => s.Id);
            #endregion
        }
    }
}
