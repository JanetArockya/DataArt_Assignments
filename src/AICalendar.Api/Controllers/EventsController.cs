using AICalendar.Api.Models;
using AICalendar.Domain.Models;
using AICalendar.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace AICalendar.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// List events with pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse<Event>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    public async Task<ActionResult<PaginationResponse<Event>>> GetAllEvents(
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? attendee = null,
        [FromQuery] EventStatus? status = null,
        [FromQuery] string? location = null,
        [FromQuery] string timeZone = "UTC",
        [FromQuery] string sort = "startTime:asc")
    {
        try
        {
            // Validate pagination parameters
            if (page < 1 || size < 1 || size > 100)
            {
                return BadRequest(CreateErrorResponse("INVALID_PAGINATION", 
                    "Page must be >= 1 and size must be between 1 and 100"));
            }

            // Validate date range
            if (startDate.HasValue && endDate.HasValue && endDate <= startDate)
            {
                return BadRequest(CreateErrorResponse("INVALID_DATE_RANGE", 
                    "End date must be after start date"));
            }

            IEnumerable<Event> events;

            // Apply filtering based on query parameters
            if (startDate.HasValue && endDate.HasValue)
            {
                events = await _eventService.GetEventsByDateRangeAsync(startDate.Value, endDate.Value);
            }
            else if (!string.IsNullOrEmpty(attendee))
            {
                events = await _eventService.GetEventsByAttendeeAsync(attendee);
            }
            else
            {
                events = await _eventService.GetAllEventsAsync();
            }

            // Apply additional filters
            if (status.HasValue)
            {
                events = events.Where(e => e.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(location))
            {
                events = events.Where(e => e.Location != null && 
                    e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            events = ApplySorting(events, sort);

            // Apply pagination
            var totalCount = events.Count();
            var pagedEvents = events.Skip((page - 1) * size).Take(size).ToList();

            var response = new PaginationResponse<Event>
            {
                Data = pagedEvents,
                Pagination = new PaginationMetadata
                {
                    Page = page,
                    Size = size,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / size),
                    HasNext = page * size < totalCount,
                    HasPrevious = page > 1
                }
            };

            // Add rate limit headers
            AddRateLimitHeaders();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Get events by date range
    /// </summary>
    [HttpGet("range")]
    [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<IEnumerable<Event>>> GetEventsByDateRange(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        [FromQuery] string timeZone = "UTC",
        [FromQuery] bool includeRecurring = true)
    {
        try
        {
            if (endDate <= startDate)
                return BadRequest(CreateErrorResponse("INVALID_DATE_RANGE", 
                    "End date must be after start date"));

            var events = await _eventService.GetEventsByDateRangeAsync(startDate, endDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for date range {StartDate} to {EndDate}", 
                startDate, endDate);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Get events for specific attendee
    /// </summary>
    [HttpGet("attendee/{email}")]
    [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<IEnumerable<Event>>> GetEventsByAttendee(
        string email,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] AttendeeStatus? status = null)
    {
        try
        {
            if (!IsValidEmail(email))
                return BadRequest(CreateErrorResponse("INVALID_EMAIL", "Invalid email format"));

            var events = await _eventService.GetEventsByAttendeeAsync(email);

            // Apply additional filtering
            if (startDate.HasValue)
                events = events.Where(e => e.StartTime >= startDate.Value);

            if (endDate.HasValue)
                events = events.Where(e => e.EndTime <= endDate.Value);

            if (status.HasValue)
                events = events.Where(e => e.Attendees.Any(a => a.Email == email && a.Status == status.Value));

            return Ok(events.OrderBy(e => e.StartTime));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for attendee {Email}", email);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Event), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<Event>> GetEvent(int id, [FromQuery] string timeZone = "UTC")
    {
        try
        {
            var eventItem = await _eventService.GetEventAsync(id);
            if (eventItem == null)
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", $"Event with ID {id} not found"));

            return Ok(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Event), 201)]
    [ProducesResponseType(typeof(Event), 200)] // For idempotent operations
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] Event eventItem)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(CreateValidationErrorResponse());

            // Check for idempotency
            if (!string.IsNullOrEmpty(eventItem.ClientReferenceId))
            {
                var existingEvent = await _eventService.GetEventByClientReferenceAsync(eventItem.ClientReferenceId);
                if (existingEvent != null)
                {
                    _logger.LogInformation("Returning existing event for client reference {ClientReferenceId}", 
                        eventItem.ClientReferenceId);
                    return Ok(existingEvent);
                }
            }

            var createdEvent = await _eventService.CreateEventAsync(eventItem);
            
            return CreatedAtAction(nameof(GetEvent), 
                new { id = createdEvent.Id }, createdEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid event data");
            return BadRequest(CreateErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Event), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<Event>> UpdateEvent(int id, [FromBody] Event eventItem)
    {
        try
        {
            if (id != eventItem.Id)
                return BadRequest(CreateErrorResponse("ID_MISMATCH", "Event ID in URL does not match request body"));

            if (!ModelState.IsValid)
                return BadRequest(CreateValidationErrorResponse());

            var updatedEvent = await _eventService.UpdateEventAsync(eventItem);
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid event data for update");
            
            if (ex.Message.Contains("not found"))
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", ex.Message));
            
            return BadRequest(CreateErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Cancel an event
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult> CancelEvent(int id)
    {
        try
        {
            var result = await _eventService.CancelEventAsync(id);
            if (!result)
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", $"Event with ID {id} not found"));

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Reschedule an event
    /// </summary>
    [HttpPatch("{id}/reschedule")]
    [ProducesResponseType(typeof(Event), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<Event>> RescheduleEvent(int id, [FromBody] RescheduleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(CreateValidationErrorResponse());

            var rescheduledEvent = await _eventService.RescheduleEventAsync(id, request.NewStartTime, request.NewEndTime);
            return Ok(rescheduledEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reschedule data");
            
            if (ex.Message.Contains("not found"))
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", ex.Message));
            
            return BadRequest(CreateErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Get event attendees
    /// </summary>
    [HttpGet("{id}/attendees")]
    [ProducesResponseType(typeof(IEnumerable<Attendee>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<IEnumerable<Attendee>>> GetEventAttendees(int id)
    {
        try
        {
            var eventItem = await _eventService.GetEventAsync(id);
            if (eventItem == null)
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", $"Event with ID {id} not found"));

            return Ok(eventItem.Attendees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendees for event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Add attendee to event
    /// </summary>
    [HttpPost("{id}/attendees")]
    [ProducesResponseType(typeof(Event), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<ActionResult<Event>> AddAttendee(int id, [FromBody] Attendee attendee)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(CreateValidationErrorResponse());

            var updatedEvent = await _eventService.AddAttendeeToEventAsync(id, attendee);
            return CreatedAtAction(nameof(GetEvent), new { id = updatedEvent.Id }, updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid attendee data");
            
            if (ex.Message.Contains("not found"))
                return NotFound(CreateErrorResponse("EVENT_NOT_FOUND", ex.Message));
            
            if (ex.Message.Contains("already exists"))
                return Conflict(CreateErrorResponse("ATTENDEE_EXISTS", ex.Message));
            
            return BadRequest(CreateErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attendee to event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    /// <summary>
    /// Remove attendee from event
    /// </summary>
    [HttpDelete("{id}/attendees/{email}")]
    [ProducesResponseType(typeof(Event), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<ActionResult<Event>> RemoveAttendee(int id, string email)
    {
        try
        {
            if (!IsValidEmail(email))
                return BadRequest(CreateErrorResponse("INVALID_EMAIL", "Invalid email format"));

            var updatedEvent = await _eventService.RemoveAttendeeFromEventAsync(id, email);
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid attendee removal request");
            return NotFound(CreateErrorResponse("NOT_FOUND", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing attendee from event {EventId}", id);
            return StatusCode(500, CreateErrorResponse("INTERNAL_ERROR", "Internal server error"));
        }
    }

    // Helper methods
    private static IEnumerable<Event> ApplySorting(IEnumerable<Event> events, string sort)
    {
        return sort.ToLower() switch
        {
            "starttime:desc" => events.OrderByDescending(e => e.StartTime),
            "createdat:asc" => events.OrderBy(e => e.CreatedAt),
            "createdat:desc" => events.OrderByDescending(e => e.CreatedAt),
            _ => events.OrderBy(e => e.StartTime)
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void AddRateLimitHeaders()
    {
        Response.Headers["X-RateLimit-Remaining"] = "999";
        Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString();
    }

    private ErrorResponse CreateErrorResponse(string code, string message, List<FieldError>? details = null)
    {
        return new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Code = code,
                Message = message,
                Details = details ?? new List<FieldError>(),
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            }
        };
    }

    private ErrorResponse CreateValidationErrorResponse()
    {
        var fieldErrors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new FieldError
            {
                Field = x.Key,
                Message = x.Value?.Errors.First().ErrorMessage ?? "Invalid value"
            })
            .ToList();

        return CreateErrorResponse("VALIDATION_ERROR", "The request contains invalid data", fieldErrors);
    }
}

public class RescheduleRequest
{
    [Required]
    public DateTime NewStartTime { get; set; }
    
    [Required]
    public DateTime NewEndTime { get; set; }
}