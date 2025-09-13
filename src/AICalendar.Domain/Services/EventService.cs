using AICalendar.Domain.Models;
using AICalendar.Domain.Repositories;

namespace AICalendar.Domain.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Event?> GetEventAsync(int id)
    {
        return await _eventRepository.GetByIdAsync(id);
    }

    public async Task<Event?> GetEventByClientReferenceAsync(string clientReferenceId)
    {
        return await _eventRepository.GetByClientReferenceIdAsync(clientReferenceId);
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _eventRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _eventRepository.GetEventsByDateRangeAsync(startDate, endDate);
    }

    public async Task<IEnumerable<Event>> GetEventsByAttendeeAsync(string attendeeEmail)
    {
        return await _eventRepository.GetEventsByAttendeeAsync(attendeeEmail);
    }

    public async Task<Event> CreateEventAsync(Event eventItem)
    {
        ValidateEvent(eventItem);
        return await _eventRepository.CreateAsync(eventItem);
    }

    public async Task<Event> UpdateEventAsync(Event eventItem)
    {
        ValidateEvent(eventItem);
        
        var existingEvent = await _eventRepository.GetByIdAsync(eventItem.Id);
        if (existingEvent == null)
            throw new ArgumentException($"Event with ID {eventItem.Id} not found.");

        return await _eventRepository.UpdateAsync(eventItem);
    }

    public async Task<Event> CreateOrUpdateEventAsync(Event eventItem)
    {
        ValidateEvent(eventItem);

        // Check if event exists by client reference ID
        if (!string.IsNullOrEmpty(eventItem.ClientReferenceId))
        {
            var existingEvent = await _eventRepository.GetByClientReferenceIdAsync(eventItem.ClientReferenceId);
            if (existingEvent != null)
            {
                // Update existing event (idempotency)
                eventItem.Id = existingEvent.Id;
                eventItem.CreatedAt = existingEvent.CreatedAt;
                return await _eventRepository.UpdateAsync(eventItem);
            }
        }

        // Create new event
        return await _eventRepository.CreateAsync(eventItem);
    }

    public async Task<bool> CancelEventAsync(int id)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem == null)
            return false;

        eventItem.Status = EventStatus.Cancelled;
        await _eventRepository.UpdateAsync(eventItem);
        return true;
    }

    public async Task<bool> CancelEventByClientReferenceAsync(string clientReferenceId)
    {
        var eventItem = await _eventRepository.GetByClientReferenceIdAsync(clientReferenceId);
        if (eventItem == null)
            return false;

        eventItem.Status = EventStatus.Cancelled;
        await _eventRepository.UpdateAsync(eventItem);
        return true;
    }

    public async Task<Event> RescheduleEventAsync(int id, DateTime newStartTime, DateTime newEndTime)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);
        if (eventItem == null)
            throw new ArgumentException($"Event with ID {id} not found.");

        if (newEndTime <= newStartTime)
            throw new ArgumentException("End time must be after start time.");

        eventItem.StartTime = newStartTime;
        eventItem.EndTime = newEndTime;

        return await _eventRepository.UpdateAsync(eventItem);
    }

    public async Task<Event> AddAttendeeToEventAsync(int eventId, Attendee attendee)
    {
        var eventItem = await _eventRepository.GetByIdAsync(eventId);
        if (eventItem == null)
            throw new ArgumentException($"Event with ID {eventId} not found.");

        // Check if attendee already exists
        if (eventItem.Attendees.Any(a => a.Email == attendee.Email))
            throw new ArgumentException($"Attendee with email {attendee.Email} already exists for this event.");

        attendee.EventId = eventId;
        eventItem.Attendees.Add(attendee);

        return await _eventRepository.UpdateAsync(eventItem);
    }

    public async Task<Event> RemoveAttendeeFromEventAsync(int eventId, string attendeeEmail)
    {
        var eventItem = await _eventRepository.GetByIdAsync(eventId);
        if (eventItem == null)
            throw new ArgumentException($"Event with ID {eventId} not found.");

        var attendee = eventItem.Attendees.FirstOrDefault(a => a.Email == attendeeEmail);
        if (attendee == null)
            throw new ArgumentException($"Attendee with email {attendeeEmail} not found for this event.");

        eventItem.Attendees.Remove(attendee);

        return await _eventRepository.UpdateAsync(eventItem);
    }

    private static void ValidateEvent(Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(eventItem.Title))
            throw new ArgumentException("Event title is required.");

        if (eventItem.EndTime <= eventItem.StartTime)
            throw new ArgumentException("End time must be after start time.");

        if (string.IsNullOrWhiteSpace(eventItem.TimeZone))
            throw new ArgumentException("Time zone is required.");

        // Trim and validate fields
        eventItem.Title = eventItem.Title.Trim();
        if (eventItem.Title.Length > 200)
            eventItem.Title = eventItem.Title[..200];

        if (!string.IsNullOrEmpty(eventItem.Description) && eventItem.Description.Length > 1000)
            eventItem.Description = eventItem.Description[..1000];

        if (!string.IsNullOrEmpty(eventItem.Location) && eventItem.Location.Length > 100)
            eventItem.Location = eventItem.Location[..100];
    }
}