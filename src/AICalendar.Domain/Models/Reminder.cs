using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

public class Reminder
{
    public int Id { get; set; }
    
    [Required]
    public DateTime ReminderTime { get; set; }
    
    [MaxLength(500)]
    public string? Message { get; set; }
    
    public ReminderType Type { get; set; } = ReminderType.Email;
    
    public bool IsSent { get; set; }
    
    // Foreign key
    public int EventId { get; set; }
    public virtual Event Event { get; set; } = null!;
}

public enum ReminderType
{
    Email,
    Popup,
    SMS
}