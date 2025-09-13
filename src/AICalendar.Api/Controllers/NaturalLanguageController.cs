using Microsoft.AspNetCore.Mvc;
using AICalendar.Api.Models;
using AICalendar.Domain.Services;
using AICalendar.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace AICalendar.Api.Controllers;

/// <summary>
/// Controller for processing natural language calendar commands
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NaturalLanguageController : ControllerBase
{
        private readonly ILlmService _llmService;
        private readonly IMcpService _mcpService;
        private readonly IEventService _eventService;
        private readonly ILogger<NaturalLanguageController> _logger;

        public NaturalLanguageController(
            ILlmService llmService, 
            IMcpService mcpService,
            IEventService eventService,
            ILogger<NaturalLanguageController> logger)
        {
            _llmService = llmService;
            _mcpService = mcpService;
            _eventService = eventService;
            _logger = logger;
        }    /// <summary>
    /// Process natural language calendar command
    /// </summary>
    /// <param name="request">Natural language request</param>
    /// <returns>Processed calendar operation result</returns>
    /// <response code="200">Command processed successfully</response>
    /// <response code="400">Invalid input or processing error</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("command")]
    [ProducesResponseType(typeof(NaturalLanguageResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<NaturalLanguageResponse>> ProcessCommand([FromBody] NaturalLanguageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = "INVALID_COMMAND",
                        Message = "Command cannot be empty",
                        TraceId = HttpContext.TraceIdentifier
                    }
                });
            }

            _logger.LogInformation("Processing natural language command: {Command}", request.Command);

            // Step 1: Parse natural language input
            var parseResult = await _llmService.ProcessNaturalLanguageAsync(request.Command, request.Context);

            if (!parseResult.Success || parseResult.Operation == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = "PARSING_FAILED",
                        Message = parseResult.Message,
                        TraceId = HttpContext.TraceIdentifier
                    }
                });
            }

            // Step 2: Execute the calendar operation through MCP
            var mcpRequest = CreateMcpRequestFromOperation(parseResult.Operation);
            var mcpResult = await _mcpService.ExecuteToolAsync(mcpRequest);
            
            if (!mcpResult.Success)
            {
                _logger.LogWarning("MCP execution failed: {Error}", mcpResult.Error);
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetails
                    {
                        Code = "MCP_EXECUTION_FAILED",
                        Message = mcpResult.Error ?? "MCP tool execution failed",
                        TraceId = HttpContext.TraceIdentifier
                    }
                });
            }

            // Step 3: Generate natural language response
            var responseMessage = await _llmService.GenerateResponseAsync(parseResult.Operation, mcpResult.Success);

            var response = new NaturalLanguageResponse
            {
                Success = true,
                Message = responseMessage,
                Operation = parseResult.Operation,
                Event = mcpResult.Result as Event,
                ProcessingTime = parseResult.ProcessingTime,
                Suggestions = parseResult.Suggestions
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing natural language command: {Command}", request.Command);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorDetails
                {
                    Code = "PROCESSING_ERROR",
                    Message = "An error occurred while processing your request",
                    TraceId = HttpContext.TraceIdentifier
                }
            });
        }
    }

    /// <summary>
    /// Get suggestions for completing user input
    /// </summary>
    /// <param name="partialInput">Partial user input</param>
    /// <returns>List of completion suggestions</returns>
    /// <response code="200">Suggestions retrieved successfully</response>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(SuggestionsResponse), 200)]
    public async Task<ActionResult<SuggestionsResponse>> GetSuggestions([FromQuery] string partialInput)
    {
        try
        {
            var existingEvents = await _eventService.GetAllEventsAsync();
            var suggestions = await _llmService.GenerateEventSuggestionsAsync(partialInput, existingEvents.ToList());

            var response = new SuggestionsResponse
            {
                Suggestions = suggestions.Select(e => new EventSuggestion
                {
                    Title = e.Title,
                    Description = e.Description ?? string.Empty,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Confidence = 0.8 // Default confidence for suggestions
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions for input: {Input}", partialInput);
            return Ok(new SuggestionsResponse { Suggestions = new List<EventSuggestion>() });
        }
    }

    /// <summary>
    /// Check LLM service health and availability
    /// </summary>
    /// <returns>Health status of the LLM service</returns>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unavailable</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(LlmHealthResponse), 200)]
    [ProducesResponseType(typeof(LlmHealthResponse), 503)]
    public async Task<ActionResult<LlmHealthResponse>> CheckHealth()
    {
        try
        {
            var isHealthy = await _llmService.HealthCheckAsync();
            var response = new LlmHealthResponse
            {
                IsHealthy = isHealthy,
                Message = isHealthy ? "LLM service is available" : "LLM service is not responding",
                Timestamp = DateTime.UtcNow
            };

            return isHealthy ? Ok(response) : StatusCode(503, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking LLM health");
            return StatusCode(503, new LlmHealthResponse
            {
                IsHealthy = false,
                Message = "Health check failed",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get examples of natural language commands
    /// </summary>
    /// <returns>List of example commands</returns>
    /// <response code="200">Examples retrieved successfully</response>
    [HttpGet("examples")]
    [ProducesResponseType(typeof(ExamplesResponse), 200)]
    public ActionResult<ExamplesResponse> GetExamples()
    {
        var examples = new List<CommandExample>
        {
            new() { Command = "Schedule a meeting with John tomorrow at 3pm", Description = "Create a new event" },
            new() { Command = "Book a conference room for Friday 2-4pm", Description = "Create event with location" },
            new() { Command = "Cancel my 10am meeting", Description = "Delete an existing event" },
            new() { Command = "Move the team standup to 9:30am", Description = "Update event time" },
            new() { Command = "What meetings do I have next week?", Description = "Find events by date range" },
            new() { Command = "Am I free on Thursday afternoon?", Description = "Check availability" },
            new() { Command = "Suggest some time for a client call", Description = "Get meeting time recommendations" },
            new() { Command = "Schedule recurring daily standup at 9am", Description = "Create recurring event" }
        };

        return Ok(new ExamplesResponse { Examples = examples });
    }

    private McpToolRequest CreateMcpRequestFromOperation(CalendarOperation operation)
    {
        // Map CalendarOperation to appropriate MCP tool
        string toolName = operation.OperationType switch
        {
            CalendarOperationType.CreateEvent => "save_event",
            CalendarOperationType.UpdateEvent => "update_event",
            CalendarOperationType.DeleteEvent => "cancel_event",
            CalendarOperationType.FindEvent => "find_events",
            CalendarOperationType.CheckAvailability => "check_availability",
            _ => "find_events"
        };

        var arguments = new Dictionary<string, object>();
        
        if (operation.EventData != null)
        {
            arguments["event"] = operation.EventData;
        }

        if (operation.ExtractedEntities.Any())
        {
            arguments["query"] = string.Join(" ", operation.ExtractedEntities);
        }

        // Add parsed intent for context
        if (!string.IsNullOrEmpty(operation.ParsedIntent))
        {
            arguments["intent"] = operation.ParsedIntent;
        }

        return new McpToolRequest
        {
            ToolName = toolName,
            Arguments = arguments
        };
    }

    private List<Event> FilterEventsByOperation(List<Event> events, CalendarOperation operation)
    {
        // Simple filtering based on extracted entities and keywords
        var keywords = operation.ExtractedEntities.Concat(new[] { operation.ParsedIntent }).ToList();
        
        return events.Where(e => 
            keywords.Any(keyword => 
                e.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
            )
        ).ToList();
    }
}

// Request/Response DTOs
public class NaturalLanguageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Command { get; set; } = string.Empty;
    
    public Dictionary<string, object>? Context { get; set; }
}

public class NaturalLanguageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CalendarOperation? Operation { get; set; }
    public Event? Event { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

public class SuggestionsResponse
{
    public List<EventSuggestion> Suggestions { get; set; } = new();
}

public class EventSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double Confidence { get; set; }
}

public class LlmHealthResponse
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ExamplesResponse
{
    public List<CommandExample> Examples { get; set; } = new();
}

public class CommandExample
{
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}