using AICalendar.Domain.Models;

namespace AICalendar.Domain.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<Event?> GetByClientReferenceIdAsync(string clientReferenceId);
    Task<IEnumerable<Event>> GetAllAsync();
    Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Event>> GetEventsByAttendeeAsync(string attendeeEmail);
    Task<Event> CreateAsync(Event eventItem);
    Task<Event> UpdateAsync(Event eventItem);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByClientReferenceIdAsync(string clientReferenceId);
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsByClientReferenceIdAsync(string clientReferenceId);
}