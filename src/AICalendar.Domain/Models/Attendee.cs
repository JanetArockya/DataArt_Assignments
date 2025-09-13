using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

public class Attendee
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    
    public AttendeeStatus Status { get; set; } = AttendeeStatus.Pending;
    
    public bool IsOrganizer { get; set; }
    
    // Foreign key
    public int EventId { get; set; }
    public virtual Event Event { get; set; } = null!;
}

public enum AttendeeStatus
{
    Pending,
    Accepted,
    Declined,
    Tentative
}