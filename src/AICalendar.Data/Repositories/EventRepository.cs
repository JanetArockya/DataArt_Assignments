using AICalendar.Domain.Models;
using AICalendar.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly CalendarDbContext _context;

    public EventRepository(CalendarDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Reminders)
            .Include(e => e.RecurrenceRule)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event?> GetByClientReferenceIdAsync(string clientReferenceId)
    {
        return await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Reminders)
            .Include(e => e.RecurrenceRule)
            .FirstOrDefaultAsync(e => e.ClientReferenceId == clientReferenceId);
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Reminders)
            .Include(e => e.RecurrenceRule)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Reminders)
            .Include(e => e.RecurrenceRule)
            .Where(e => e.StartTime >= startDate && e.StartTime <= endDate)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByAttendeeAsync(string attendeeEmail)
    {
        return await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Reminders)
            .Include(e => e.RecurrenceRule)
            .Where(e => e.Attendees.Any(a => a.Email == attendeeEmail))
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        eventItem.CreatedAt = DateTime.UtcNow;
        eventItem.UpdatedAt = DateTime.UtcNow;
        
        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();
        return eventItem;
    }

    public async Task<Event> UpdateAsync(Event eventItem)
    {
        eventItem.UpdatedAt = DateTime.UtcNow;
        
        _context.Events.Update(eventItem);
        await _context.SaveChangesAsync();
        return eventItem;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
            return false;

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByClientReferenceIdAsync(string clientReferenceId)
    {
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.ClientReferenceId == clientReferenceId);
            
        if (eventItem == null)
            return false;

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Events.AnyAsync(e => e.Id == id);
    }

    public async Task<bool> ExistsByClientReferenceIdAsync(string clientReferenceId)
    {
        return await _context.Events.AnyAsync(e => e.ClientReferenceId == clientReferenceId);
    }
}