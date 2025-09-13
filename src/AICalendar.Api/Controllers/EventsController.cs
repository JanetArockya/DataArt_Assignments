using AICalendar.Domain.Models;
using AICalendar.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Get all events
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAllEvents()
    {
        try
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all events");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        try
        {
            var eventItem = await _eventService.GetEventAsync(id);
            if (eventItem == null)
                return NotFound($"Event with ID {id} not found");

            return Ok(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get events by date range
    /// </summary>
    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<Event>>> GetEventsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
                return BadRequest("End date must be after start date");

            var events = await _eventService.GetEventsByDateRangeAsync(startDate, endDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for date range {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get events by attendee email
    /// </summary>
    [HttpGet("attendee/{email}")]
    public async Task<ActionResult<IEnumerable<Event>>> GetEventsByAttendee(string email)
    {
        try
        {
            var events = await _eventService.GetEventsByAttendeeAsync(email);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for attendee {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] Event eventItem)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdEvent = await _eventService.CreateEventAsync(eventItem);
            return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid event data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Event>> UpdateEvent(int id, [FromBody] Event eventItem)
    {
        try
        {
            if (id != eventItem.Id)
                return BadRequest("Event ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedEvent = await _eventService.UpdateEventAsync(eventItem);
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid event data for update");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel an event
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelEvent(int id)
    {
        try
        {
            var result = await _eventService.CancelEventAsync(id);
            if (!result)
                return NotFound($"Event with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reschedule an event
    /// </summary>
    [HttpPatch("{id}/reschedule")]
    public async Task<ActionResult<Event>> RescheduleEvent(int id, [FromBody] RescheduleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rescheduledEvent = await _eventService.RescheduleEventAsync(id, request.NewStartTime, request.NewEndTime);
            return Ok(rescheduledEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reschedule data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add attendee to event
    /// </summary>
    [HttpPost("{id}/attendees")]
    public async Task<ActionResult<Event>> AddAttendee(int id, [FromBody] Attendee attendee)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedEvent = await _eventService.AddAttendeeToEventAsync(id, attendee);
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid attendee data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attendee to event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove attendee from event
    /// </summary>
    [HttpDelete("{id}/attendees/{email}")]
    public async Task<ActionResult<Event>> RemoveAttendee(int id, string email)
    {
        try
        {
            var updatedEvent = await _eventService.RemoveAttendeeFromEventAsync(id, email);
            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid attendee removal request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing attendee from event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class RescheduleRequest
{
    public DateTime NewStartTime { get; set; }
    public DateTime NewEndTime { get; set; }
}