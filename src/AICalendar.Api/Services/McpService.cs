using AICalendar.Domain.Models;
using AICalendar.Domain.Services;
using System.Text.Json;

namespace AICalendar.Api.Services;

/// <summary>
/// Implementation of Model Context Protocol server for calendar operations
/// </summary>
public class McpService : IMcpService
{
    private readonly IEventService _eventService;
    private readonly ILogger<McpService> _logger;
    private McpServerConfiguration _configuration = new();
    private bool _isInitialized = false;
    private readonly Dictionary<string, Func<Dictionary<string, object>, Task<object>>> _toolHandlers = new();

    public McpService(IEventService eventService, ILogger<McpService> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(McpServerConfiguration configuration)
    {
        try
        {
            _configuration = configuration;
            await RegisterCalendarToolsAsync();
            _isInitialized = true;

            _logger.LogInformation("MCP Server initialized with {ToolCount} tools", _configuration.Tools.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP server");
            return false;
        }
    }

    public async Task<List<McpTool>> GetAvailableToolsAsync()
    {
        await Task.CompletedTask;
        return _configuration.Tools;
    }

    public async Task<McpToolResponse> ExecuteToolAsync(McpToolRequest request)
    {
        try
        {
            if (!_isInitialized)
            {
                return new McpToolResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Error = "MCP server not initialized"
                };
            }

            if (!_toolHandlers.ContainsKey(request.ToolName))
            {
                return new McpToolResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Error = $"Tool '{request.ToolName}' not found"
                };
            }

            _logger.LogInformation("Executing MCP tool: {ToolName}", request.ToolName);

            var handler = _toolHandlers[request.ToolName];
            var result = await handler(request.Arguments);

            return new McpToolResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Result = result,
                Metadata = new Dictionary<string, object>
                {
                    ["tool"] = request.ToolName,
                    ["timestamp"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool: {ToolName}", request.ToolName);
            return new McpToolResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Error = $"Tool execution failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> RegisterToolAsync(McpTool tool)
    {
        try
        {
            if (!_configuration.Tools.Any(t => t.Name == tool.Name))
            {
                _configuration.Tools.Add(tool);
                _logger.LogInformation("Registered MCP tool: {ToolName}", tool.Name);
            }
            
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register MCP tool: {ToolName}", tool.Name);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        await Task.CompletedTask;
        return _isInitialized && _configuration.Tools.Any();
    }

    public async Task<bool> StartServerAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                await InitializeAsync(_configuration);
            }

            _logger.LogInformation("MCP Server started on port {Port}", _configuration.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MCP server");
            return false;
        }
    }

    public async Task<bool> StopServerAsync()
    {
        try
        {
            _isInitialized = false;
            _logger.LogInformation("MCP Server stopped");
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server");
            return false;
        }
    }

    private async Task RegisterCalendarToolsAsync()
    {
        // Register calendar.save_event tool
        var saveEventParameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "title": { "type": "string", "description": "Event title" },
                "description": { "type": "string", "description": "Event description" },
                "start_time": { "type": "string", "format": "date-time", "description": "Event start time" },
                "end_time": { "type": "string", "format": "date-time", "description": "Event end time" },
                "location": { "type": "string", "description": "Event location" }
            },
            "required": ["title", "start_time", "end_time"]
        }
        """);

        var saveEventTool = new McpTool
        {
            Name = "calendar.save_event",
            Description = "Save a new calendar event to the database",
            Parameters = saveEventParameters.RootElement
        };

        await RegisterToolAsync(saveEventTool);
        _toolHandlers["calendar.save_event"] = HandleSaveEventAsync;

        // Register calendar.update_event tool
        var updateEventParameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "id": { "type": "string", "description": "Event ID to update" },
                "title": { "type": "string", "description": "Updated event title" },
                "description": { "type": "string", "description": "Updated event description" },
                "start_time": { "type": "string", "format": "date-time", "description": "Updated start time" },
                "end_time": { "type": "string", "format": "date-time", "description": "Updated end time" },
                "location": { "type": "string", "description": "Updated location" },
                "status": { "type": "string", "description": "Event status" }
            },
            "required": ["id"]
        }
        """);

        var updateEventTool = new McpTool
        {
            Name = "calendar.update_event",
            Description = "Update an existing calendar event",
            Parameters = updateEventParameters.RootElement
        };

        await RegisterToolAsync(updateEventTool);
        _toolHandlers["calendar.update_event"] = HandleUpdateEventAsync;

        // Register calendar.cancel_event tool
        var cancelEventParameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "id": { "type": "string", "description": "Event ID to cancel" },
                "reason": { "type": "string", "description": "Reason for cancellation" }
            },
            "required": ["id"]
        }
        """);

        var cancelEventTool = new McpTool
        {
            Name = "calendar.cancel_event",
            Description = "Cancel or delete a calendar event",
            Parameters = cancelEventParameters.RootElement
        };

        await RegisterToolAsync(cancelEventTool);
        _toolHandlers["calendar.cancel_event"] = HandleCancelEventAsync;

        // Register calendar.find_events tool
        var findEventsParameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "start_date": { "type": "string", "format": "date", "description": "Search start date" },
                "end_date": { "type": "string", "format": "date", "description": "Search end date" },
                "title_contains": { "type": "string", "description": "Search in event titles" },
                "location_contains": { "type": "string", "description": "Search in event locations" },
                "status": { "type": "string", "description": "Event status filter" }
            }
        }
        """);

        var findEventsTool = new McpTool
        {
            Name = "calendar.find_events",
            Description = "Find calendar events by criteria",
            Parameters = findEventsParameters.RootElement
        };

        await RegisterToolAsync(findEventsTool);
        _toolHandlers["calendar.find_events"] = HandleFindEventsAsync;

        // Register calendar.check_availability tool
        var checkAvailabilityParameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "start_time": { "type": "string", "format": "date-time", "description": "Start time to check" },
                "end_time": { "type": "string", "format": "date-time", "description": "End time to check" },
                "exclude_event_id": { "type": "string", "description": "Event ID to exclude from availability check" }
            },
            "required": ["start_time", "end_time"]
        }
        """);

        var checkAvailabilityTool = new McpTool
        {
            Name = "calendar.check_availability",
            Description = "Check availability for a specific time period",
            Parameters = checkAvailabilityParameters.RootElement
        };

        await RegisterToolAsync(checkAvailabilityTool);
        _toolHandlers["calendar.check_availability"] = HandleCheckAvailabilityAsync;
    }

    private async Task<object> HandleSaveEventAsync(Dictionary<string, object> arguments)
    {
        var eventData = new Event
        {
            Title = arguments.GetValueOrDefault("title")?.ToString() ?? "Untitled Event",
            Description = arguments.GetValueOrDefault("description")?.ToString() ?? "",
            Location = arguments.GetValueOrDefault("location")?.ToString() ?? "",
            Status = EventStatus.Confirmed
        };

        if (arguments.TryGetValue("start_time", out var startTimeObj) && 
            DateTime.TryParse(startTimeObj.ToString(), out var startTime))
        {
            eventData.StartTime = startTime;
        }

        if (arguments.TryGetValue("end_time", out var endTimeObj) && 
            DateTime.TryParse(endTimeObj.ToString(), out var endTime))
        {
            eventData.EndTime = endTime;
        }

        var createdEvent = await _eventService.CreateEventAsync(eventData);
        
        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["event"] = createdEvent,
            ["message"] = "Event created successfully"
        };
    }

    private async Task<object> HandleUpdateEventAsync(Dictionary<string, object> arguments)
    {
        if (!arguments.TryGetValue("id", out var idObj) || idObj?.ToString() == null)
        {
            throw new ArgumentException("Event ID is required");
        }

        var eventId = idObj.ToString()!;
        var existingEvent = await _eventService.GetEventAsync(int.Parse(eventId));
        
        if (existingEvent == null)
        {
            throw new ArgumentException($"Event with ID {eventId} not found");
        }

        // Update only provided fields
        if (arguments.TryGetValue("title", out var title))
            existingEvent.Title = title.ToString() ?? existingEvent.Title;

        if (arguments.TryGetValue("description", out var description))
            existingEvent.Description = description.ToString() ?? existingEvent.Description;

        if (arguments.TryGetValue("location", out var location))
            existingEvent.Location = location.ToString() ?? existingEvent.Location;

        if (arguments.TryGetValue("start_time", out var startTime) && 
            DateTime.TryParse(startTime.ToString(), out var parsedStartTime))
            existingEvent.StartTime = parsedStartTime;

        if (arguments.TryGetValue("end_time", out var endTime) && 
            DateTime.TryParse(endTime.ToString(), out var parsedEndTime))
            existingEvent.EndTime = parsedEndTime;

        if (arguments.TryGetValue("status", out var status) && 
            Enum.TryParse<EventStatus>(status.ToString(), out var parsedStatus))
            existingEvent.Status = parsedStatus;

        var updatedEvent = await _eventService.UpdateEventAsync(existingEvent);
        
        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["event"] = updatedEvent,
            ["message"] = "Event updated successfully"
        };
    }

    private async Task<object> HandleCancelEventAsync(Dictionary<string, object> arguments)
    {
        if (!arguments.TryGetValue("id", out var idObj) || idObj?.ToString() == null)
        {
            throw new ArgumentException("Event ID is required");
        }

        var eventId = idObj.ToString()!;
        var reason = arguments.GetValueOrDefault("reason")?.ToString() ?? "Event cancelled";

        var success = await _eventService.CancelEventAsync(int.Parse(eventId));
        
        return new Dictionary<string, object>
        {
            ["success"] = success,
            ["message"] = success ? $"Event cancelled: {reason}" : "Failed to cancel event"
        };
    }

    private async Task<object> HandleFindEventsAsync(Dictionary<string, object> arguments)
    {
        var allEvents = await _eventService.GetAllEventsAsync();
        var filteredEvents = allEvents.AsEnumerable();

        if (arguments.TryGetValue("start_date", out var startDateObj) && 
            DateTime.TryParse(startDateObj.ToString(), out var startDate))
        {
            filteredEvents = filteredEvents.Where(e => e.StartTime.Date >= startDate.Date);
        }

        if (arguments.TryGetValue("end_date", out var endDateObj) && 
            DateTime.TryParse(endDateObj.ToString(), out var endDate))
        {
            filteredEvents = filteredEvents.Where(e => e.StartTime.Date <= endDate.Date);
        }

        if (arguments.TryGetValue("title_contains", out var titleFilter) && !string.IsNullOrEmpty(titleFilter.ToString()))
        {
            filteredEvents = filteredEvents.Where(e => e.Title.Contains(titleFilter.ToString()!, StringComparison.OrdinalIgnoreCase));
        }

        if (arguments.TryGetValue("location_contains", out var locationFilter) && !string.IsNullOrEmpty(locationFilter.ToString()))
        {
            filteredEvents = filteredEvents.Where(e => !string.IsNullOrEmpty(e.Location) && e.Location.Contains(locationFilter.ToString()!, StringComparison.OrdinalIgnoreCase));
        }

        if (arguments.TryGetValue("status", out var statusFilter) && 
            Enum.TryParse<EventStatus>(statusFilter.ToString(), out var status))
        {
            filteredEvents = filteredEvents.Where(e => e.Status == status);
        }

        var results = filteredEvents.ToList();
        
        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["events"] = results,
            ["count"] = results.Count
        };
    }

    private async Task<object> HandleCheckAvailabilityAsync(Dictionary<string, object> arguments)
    {
        if (!arguments.TryGetValue("start_time", out var startTimeObj) || 
            !DateTime.TryParse(startTimeObj.ToString(), out var startTime))
        {
            throw new ArgumentException("Valid start_time is required");
        }

        if (!arguments.TryGetValue("end_time", out var endTimeObj) || 
            !DateTime.TryParse(endTimeObj.ToString(), out var endTime))
        {
            throw new ArgumentException("Valid end_time is required");
        }

        var excludeEventId = arguments.GetValueOrDefault("exclude_event_id")?.ToString();
        var allEvents = await _eventService.GetAllEventsAsync();

        var conflictingEvents = allEvents.Where(e => 
            e.StartTime < endTime && 
            e.EndTime > startTime &&
            e.Status != EventStatus.Cancelled &&
            (excludeEventId == null || e.Id.ToString() != excludeEventId)
        ).ToList();

        var isAvailable = !conflictingEvents.Any();

        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["available"] = isAvailable,
            ["conflicts"] = conflictingEvents.Count,
            ["conflicting_events"] = conflictingEvents.Select(e => new Dictionary<string, object>
            {
                ["id"] = e.Id,
                ["title"] = e.Title,
                ["start_time"] = e.StartTime,
                ["end_time"] = e.EndTime
            }).ToList()
        };
    }
}