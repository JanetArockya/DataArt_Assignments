using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

/// <summary>
/// Main Event model with C# 13 Partial Properties
/// Core properties are implemented as partial properties in separate files
/// </summary>
public partial class Event
{
    public int Id { get; set; }
    
    // Title is implemented as partial property in Event.Implementation.cs
    // [Required] and [MaxLength(200)] validation handled in implementation
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    // StartTime and EndTime are implemented as partial properties with validation
    
    [MaxLength(100)]
    public string? Location { get; set; }
    
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";
    
    public bool IsAllDay { get; set; }
    
    // Status is implemented as partial property with change tracking
    
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