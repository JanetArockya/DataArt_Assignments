using System.ComponentModel.DataAnnotations;

namespace AICalendar.Domain.Models;

public class RecurrenceRule
{
    public int Id { get; set; }
    
    [Required]
    public RecurrenceFrequency Frequency { get; set; }
    
    public int Interval { get; set; } = 1;
    
    public DaysOfWeek? DaysOfWeek { get; set; }
    
    public int? DayOfMonth { get; set; }
    
    public int? WeekOfMonth { get; set; }
    
    public int? MonthOfYear { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public int? OccurrenceCount { get; set; }
    
    // Foreign key
    public int EventId { get; set; }
    public virtual Event Event { get; set; } = null!;
}

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

[Flags]
public enum DaysOfWeek
{
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64
}