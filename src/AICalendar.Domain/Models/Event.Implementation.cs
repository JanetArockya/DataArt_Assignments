namespace AICalendar.Domain.Models;

/// <summary>
/// Partial implementation of Event properties with C# 13 features
/// This file contains the actual property implementations with business logic
/// </summary>
public partial class Event
{
    private EventStatus _status = EventStatus.Confirmed;
    private DateTime _startTime;
    private DateTime _endTime;
    private string _title = string.Empty;

    /// <summary>
    /// C# 13 Partial Property: Status with validation and change tracking
    /// </summary>
    public partial EventStatus Status
    {
        get => _status;
        set
        {
            if (value != _status)
            {
                var oldStatus = _status;
                _status = value;
                OnStatusChanged(oldStatus, value);
            }
        }
    }

    /// <summary>
    /// C# 13 Partial Property: StartTime with validation
    /// </summary>
    public partial DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (value == default)
                throw new ArgumentException("Start time cannot be default value");
            
            if (_endTime != default && value >= _endTime)
                throw new ArgumentException("Start time must be before end time");
            
            _startTime = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// C# 13 Partial Property: EndTime with validation
    /// </summary>
    public partial DateTime EndTime
    {
        get => _endTime;
        set
        {
            if (value == default)
                throw new ArgumentException("End time cannot be default value");
            
            if (_startTime != default && value <= _startTime)
                throw new ArgumentException("End time must be after start time");
            
            _endTime = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// C# 13 Partial Property: Title with trimming and validation
    /// </summary>
    public partial string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Title cannot be null or empty");
            
            var trimmedValue = value.Trim();
            if (trimmedValue.Length > 200)
                trimmedValue = trimmedValue[..200];
            
            _title = trimmedValue;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event handler for status changes - demonstrates business logic separation
    /// </summary>
    private void OnStatusChanged(EventStatus oldStatus, EventStatus newStatus)
    {
        // Business logic for status changes
        if (newStatus == EventStatus.Cancelled && oldStatus != EventStatus.Cancelled)
        {
            // Logic for cancellation - notify attendees, cleanup, etc.
            foreach (var attendee in Attendees)
            {
                attendee.Status = AttendeeStatus.Declined;
            }
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
}