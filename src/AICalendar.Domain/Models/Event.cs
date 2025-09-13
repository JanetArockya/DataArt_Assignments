using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

public class Event
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    
    [Required]
    public DateTime EndTime { get; set; }
    
    [MaxLength(100)]
    public string? Location { get; set; }
    
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";
    
    public bool IsAllDay { get; set; }
    
    public EventStatus Status { get; set; } = EventStatus.Confirmed;
    
    [MaxLength(100)]
    public string? ClientReferenceId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Attendee> Attendees { get; set; } = new List<Attendee>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual RecurrenceRule? RecurrenceRule { get; set; }
}

public enum EventStatus
{
    Tentative,
    Confirmed,
    Cancelled
}