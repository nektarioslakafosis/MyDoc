using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDoc.Models;

namespace MyDoc.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<DoctorUnavailablePeriod> DoctorUnavailablePeriods { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AppointmentRescheduleRequest> AppointmentRescheduleRequests { get; set; }
        public DbSet<AppointmentMessage> AppointmentMessages { get; set; }
        public DbSet<AppointmentIntake> AppointmentIntakes { get; set; }
        public DbSet<AppointmentFollowUpPlan> AppointmentFollowUpPlans { get; set; }
        public DbSet<AppointmentDocument> AppointmentDocuments { get; set; }
        public DbSet<AppointmentReminderLog> AppointmentReminderLogs { get; set; }
        public DbSet<AppointmentPrivateNote> AppointmentPrivateNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1)")
                .HasDatabaseName("IX_Appointments_DoctorId_AppointmentDate_ActiveStatuses");

            builder.Entity<Doctor>()
                .Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.Entity<Patient>()
                .Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.Entity<DoctorAvailability>()
                .HasOne(da => da.Doctor)
                .WithMany(d => d.Availabilities)
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DoctorAvailability>()
                .Property(da => da.StartTime)
                .HasConversion(
                    v => v.ToTimeSpan(),
                    v => TimeOnly.FromTimeSpan(v));

            builder.Entity<DoctorAvailability>()
                .Property(da => da.EndTime)
                .HasConversion(
                    v => v.ToTimeSpan(),
                    v => TimeOnly.FromTimeSpan(v));

            builder.Entity<DoctorUnavailablePeriod>()
                .HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DoctorUnavailablePeriod>()
                .Property(x => x.Date)
                .HasColumnType("date");

            builder.Entity<DoctorUnavailablePeriod>()
                .Property(x => x.StartTime)
                .HasConversion(
                    v => v.ToTimeSpan(),
                    v => TimeOnly.FromTimeSpan(v));

            builder.Entity<DoctorUnavailablePeriod>()
                .Property(x => x.EndTime)
                .HasConversion(
                    v => v.ToTimeSpan(),
                    v => TimeOnly.FromTimeSpan(v));

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            builder.Entity<AppointmentRescheduleRequest>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentRescheduleRequest>()
                .HasIndex(x => new { x.AppointmentId, x.CreatedAt });

            builder.Entity<AppointmentRescheduleRequest>()
                .HasIndex(x => new { x.AppointmentId, x.Status })
                .IsUnique()
                .HasFilter("[Status] = 0")
                .HasDatabaseName("IX_AppointmentRescheduleRequests_OnePendingPerAppointment");

            builder.Entity<AppointmentMessage>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentMessage>()
                .HasIndex(x => new { x.AppointmentId, x.CreatedAt });

            builder.Entity<AppointmentMessage>()
                .HasIndex(x => new { x.AppointmentId, x.ReadAt });

            builder.Entity<AppointmentIntake>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentIntake>()
                .HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasDatabaseName("IX_AppointmentIntakes_AppointmentId");

            builder.Entity<AppointmentFollowUpPlan>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentFollowUpPlan>()
                .HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasDatabaseName("IX_AppointmentFollowUpPlans_AppointmentId");

            builder.Entity<AppointmentDocument>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentDocument>()
                .HasIndex(x => new { x.AppointmentId, x.UploadedAt });

            builder.Entity<AppointmentDocument>()
                .HasIndex(x => new { x.UploadedByUserId, x.UploadedAt });

            builder.Entity<AppointmentReminderLog>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentReminderLog>()
                .HasIndex(x => new { x.AppointmentId, x.ReminderType })
                .IsUnique()
                .HasDatabaseName("IX_AppointmentReminderLogs_AppointmentId_ReminderType");

            builder.Entity<AppointmentReminderLog>()
                .HasIndex(x => x.SentAt);

            builder.Entity<AppointmentPrivateNote>()
                .HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentPrivateNote>()
                .HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AppointmentPrivateNote>()
                .HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasDatabaseName("IX_AppointmentPrivateNotes_AppointmentId");

            builder.Entity<AppointmentPrivateNote>()
                .HasIndex(x => new { x.DoctorId, x.UpdatedAt });
        }
    }
}