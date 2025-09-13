using AICalendar.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Data;

public class CalendarDbContext : DbContext
{
    public CalendarDbContext(DbContextOptions<CalendarDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Attendee> Attendees { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.TimeZone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientReferenceId).HasMaxLength(100);
            
            entity.HasIndex(e => e.ClientReferenceId).IsUnique();
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.EndTime);

            // Relationships
            entity.HasMany(e => e.Attendees)
                  .WithOne(a => a.Event)
                  .HasForeignKey(a => a.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Reminders)
                  .WithOne(r => r.Event)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RecurrenceRule)
                  .WithOne(r => r.Event)
                  .HasForeignKey<RecurrenceRule>(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Attendee configuration
        modelBuilder.Entity<Attendee>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Email).IsRequired().HasMaxLength(254);
            
            entity.HasIndex(a => new { a.EventId, a.Email }).IsUnique();
        });

        // Reminder configuration
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Message).HasMaxLength(500);
            
            entity.HasIndex(r => r.ReminderTime);
        });

        // RecurrenceRule configuration
        modelBuilder.Entity<RecurrenceRule>(entity =>
        {
            entity.HasKey(r => r.Id);
        });
    }
}