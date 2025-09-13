using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

/// <summary>
/// Event model demonstrating C# 13 Partial Properties feature
/// This allows splitting property implementation across multiple files
/// while maintaining a clean public interface
/// </summary>
public partial class Event
{
    // Partial property for Status with custom validation logic
    public partial EventStatus Status { get; set; }
    
    // Partial property for StartTime with timezone validation
    public partial DateTime StartTime { get; set; }
    
    // Partial property for EndTime with validation
    public partial DateTime EndTime { get; set; }
    
    // Partial property for Title with trimming and validation
    public partial string Title { get; set; }
}