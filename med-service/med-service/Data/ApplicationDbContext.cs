using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace med_service.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
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

            #region Appointment
            // primary key
            modelBuilder.Entity<Appointment>()
                .HasKey(a => a.Id);

            // Связь "один ко многим" между Patient и Appointment.
            // Один пациент может иметь несколько приёмов.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении пациента, его приёмы удаляются.

            // Связь "один ко многим" между Doctor и Appointment.
            // Один врач может иметь несколько приёмов.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // При удалении врача, приёмы не удаляются автоматически.
            #endregion

            #region Doctor
            // primary key
            modelBuilder.Entity<Doctor>()
                .HasKey(d => d.Id);

            // Связь "один ко многим" между Hospital и Doctor.
            // Один госпиталь может иметь несколько врачей.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Hospital)
                .WithMany(h => h.Doctors)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.Restrict); // Предотвращаем каскадное удаление врача при удалении госпиталя.

            // Связь между Doctor и Specialization.
            // Если в сущности Specialization нет коллекции врачей, используем WithMany без параметров.
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Specialization)
                .WithMany()
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь "один ко многим" между Doctor и Schedule.
            // Один врач может иметь несколько расписаний.
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Schedules)
                .WithOne(s => s.Doctor)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении врача, его расписания удаляются.

            // Если свойство ScheduleId не используется (избыточно), его игнорируем.
            modelBuilder.Entity<Doctor>()
                .Ignore(d => d.ScheduleId);
            #endregion

            #region Patient
            // primary key
            modelBuilder.Entity<Patient>()
                .HasKey(p => p.Id);

            // Связь "один к одному" между Patient и User (Identity).
            // Каждый пациент ассоциирован с одним пользователем.
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя, удаляется и пациент.
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

            // Связь "один ко многим" между Schedule и TimeSlot.
            // Одно расписание может иметь несколько доступных временных интервалов.
            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.AvailableSlots)
                .WithOne(ts => ts.Schedule)
                .HasForeignKey(ts => ts.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении расписания, его временные интервалы удаляются.
            #endregion

            #region TimeSlot
            // primary key
            modelBuilder.Entity<TimeSlot>()
                .HasKey(ts => ts.Id);

            // Связь между TimeSlot и Doctor.
            // Если нужен быстрый доступ к врачу для временного интервала, связываем напрямую.
            // Используем WithMany без параметров, если в Doctor не определена коллекция TimeSlot.
            modelBuilder.Entity<TimeSlot>()
                .HasOne(ts => ts.Doctor)
                .WithMany()
                .HasForeignKey(ts => ts.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Specialization
            // primary key
            modelBuilder.Entity<Specialization>()
                .HasKey(s => s.Id);
            #endregion
        }
    }
}
