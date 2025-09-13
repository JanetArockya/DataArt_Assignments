using AICalendar.Domain.Models;

namespace AICalendar.Domain.Services;

public interface IEventService
{
    Task<Event?> GetEventAsync(int id);
    Task<Event?> GetEventByClientReferenceAsync(string clientReferenceId);
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Event>> GetEventsByAttendeeAsync(string attendeeEmail);
    Task<Event> CreateEventAsync(Event eventItem);
    Task<Event> UpdateEventAsync(Event eventItem);
    Task<Event> CreateOrUpdateEventAsync(Event eventItem);
    Task<bool> CancelEventAsync(int id);
    Task<bool> CancelEventByClientReferenceAsync(string clientReferenceId);
    Task<Event> RescheduleEventAsync(int id, DateTime newStartTime, DateTime newEndTime);
    Task<Event> AddAttendeeToEventAsync(int eventId, Attendee attendee);
    Task<Event> RemoveAttendeeFromEventAsync(int eventId, string attendeeEmail);
}