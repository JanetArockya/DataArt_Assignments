using AICalendar.Domain.Models;
using AICalendar.Domain.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Diagnostics;

namespace AICalendar.Api.Services;

/// <summary>
/// Implementation of LLM service using Ollama/Llama for calendar operations
/// </summary>
public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmService> _logger;
    private LlmConfiguration _configuration = new();
    private bool _isInitialized = false;

    public LlmService(HttpClient httpClient, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(LlmConfiguration configuration)
    {
        try
        {
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = _configuration.Timeout;

            // Test connection to Ollama
            var healthCheck = await HealthCheckAsync();
            _isInitialized = healthCheck;

            _logger.LogInformation("LLM Service initialized. Status: {Status}", healthCheck ? "Connected" : "Failed");
            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LLM Service");
            return false;
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM health check failed");
            return false;
        }
    }

    public async Task<LlmResponse> ProcessNaturalLanguageAsync(string naturalLanguageInput, Dictionary<string, object>? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!_isInitialized)
            {
                return new LlmResponse
                {
                    Success = false,
                    Message = "LLM service not initialized. Please ensure Ollama is running.",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            var prompt = BuildCalendarPrompt(naturalLanguageInput, context);
            var ollamaRequest = new
            {
                model = _configuration.ModelName,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = _configuration.Temperature,
                    num_predict = _configuration.MaxTokens
                }
            };

            var jsonContent = JsonSerializer.Serialize(ollamaRequest);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                return new LlmResponse
                {
                    Success = false,
                    Message = $"LLM request failed: {response.StatusCode}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            var generatedText = ollamaResponse.GetProperty("response").GetString() ?? "";
            var operation = ParseLlmResponse(generatedText, naturalLanguageInput);

            return new LlmResponse
            {
                Success = true,
                Message = "Successfully processed natural language input",
                Operation = operation,
                ProcessingTime = stopwatch.Elapsed,
                Context = context ?? new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing natural language input: {Input}", naturalLanguageInput);
            return new LlmResponse
            {
                Success = false,
                Message = $"Processing error: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<LlmResponse> ExecuteOperationAsync(CalendarOperation operation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // This would integrate with the MCP server for database operations
            // For now, we'll simulate the execution and return success
            await Task.Delay(10); // Small delay to make it properly async
            
            var executionMessage = operation.OperationType switch
            {
                CalendarOperationType.CreateEvent => $"Created event: {operation.EventData?.Title}",
                CalendarOperationType.UpdateEvent => $"Updated event: {operation.EventData?.Title}",
                CalendarOperationType.DeleteEvent => $"Deleted event: {operation.EventData?.Title}",
                CalendarOperationType.FindEvent => $"Found events matching criteria",
                CalendarOperationType.CheckAvailability => $"Checked availability for requested time",
                CalendarOperationType.SuggestMeetingTime => $"Generated meeting time suggestions",
                _ => "Operation completed"
            };

            return new LlmResponse
            {
                Success = true,
                Message = executionMessage,
                Operation = operation,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing calendar operation: {OperationType}", operation.OperationType);
            return new LlmResponse
            {
                Success = false,
                Message = $"Execution error: {ex.Message}",
                Operation = operation,
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<List<Event>> GenerateEventSuggestionsAsync(string userInput, List<Event> existingEvents)
    {
        try
        {
            await Task.CompletedTask; // Placeholder for async operation

            // Simplified suggestion generation
            var suggestions = new List<Event>
            {
                new Event
                {
                    Title = "Suggested Meeting",
                    Description = $"Auto-suggested based on: {userInput}",
                    StartTime = DateTime.Now.AddDays(1).Date.AddHours(10),
                    EndTime = DateTime.Now.AddDays(1).Date.AddHours(11),
                    Status = EventStatus.Tentative
                }
            };

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event suggestions");
            return new List<Event>();
        }
    }

    public async Task<LlmResponse> CheckConflictsAsync(Event proposedEvent, List<Event> existingEvents)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Task.CompletedTask; // Placeholder for async operation

            var conflicts = existingEvents.Where(e => 
                e.StartTime < proposedEvent.EndTime && 
                e.EndTime > proposedEvent.StartTime &&
                e.Status != EventStatus.Cancelled).ToList();

            var hasConflicts = conflicts.Any();
            var message = hasConflicts 
                ? $"Found {conflicts.Count} scheduling conflicts"
                : "No scheduling conflicts detected";

            var suggestions = new List<string>();
            if (hasConflicts)
            {
                suggestions.Add($"Move to {proposedEvent.StartTime.AddHours(1):HH:mm}");
                suggestions.Add($"Schedule for tomorrow at {proposedEvent.StartTime:HH:mm}");
                suggestions.Add("Reduce meeting duration to 30 minutes");
            }

            return new LlmResponse
            {
                Success = true,
                Message = message,
                Suggestions = suggestions,
                ProcessingTime = stopwatch.Elapsed,
                Context = new Dictionary<string, object>
                {
                    ["conflicts"] = conflicts.Count,
                    ["hasConflicts"] = hasConflicts
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking conflicts");
            return new LlmResponse
            {
                Success = false,
                Message = $"Conflict check error: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<string> GenerateResponseAsync(CalendarOperation operation, bool success, string? details = null)
    {
        try
        {
            await Task.CompletedTask; // Placeholder for async operation

            if (!success)
            {
                return $"I encountered an issue while {GetOperationDescription(operation.OperationType).ToLower()}. {details}";
            }

            var responses = operation.OperationType switch
            {
                CalendarOperationType.CreateEvent => new[]
                {
                    $"Perfect! I've scheduled '{operation.EventData?.Title}' for {operation.EventData?.StartTime:MMM dd, yyyy at h:mm tt}.",
                    $"Great! Your event '{operation.EventData?.Title}' has been added to your calendar.",
                    $"Done! I've created the event '{operation.EventData?.Title}' as requested."
                },
                CalendarOperationType.UpdateEvent => new[]
                {
                    $"Updated! I've modified '{operation.EventData?.Title}' as requested.",
                    $"Changes saved! Your event '{operation.EventData?.Title}' has been updated.",
                    $"Perfect! The event '{operation.EventData?.Title}' has been updated successfully."
                },
                CalendarOperationType.DeleteEvent => new[]
                {
                    $"Removed! I've deleted '{operation.EventData?.Title}' from your calendar.",
                    $"Done! The event '{operation.EventData?.Title}' has been cancelled.",
                    $"Success! I've removed '{operation.EventData?.Title}' as requested."
                },
                _ => new[] { "Operation completed successfully!" }
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return "I've completed your request.";
        }
    }

    private string BuildCalendarPrompt(string userInput, Dictionary<string, object>? context)
    {
        var contextInfo = context != null && context.Any() 
            ? $"Context: {JsonSerializer.Serialize(context)}\n" 
            : "";

        return $@"
You are an AI calendar assistant. Parse the following natural language input and extract calendar operation details.

{contextInfo}User Input: ""{userInput}""

Extract and return in this exact JSON format:
{{
    ""operation_type"": ""CreateEvent|UpdateEvent|DeleteEvent|FindEvent|CheckAvailability|SuggestMeetingTime"",
    ""confidence"": 0.0-1.0,
    ""title"": ""event title"",
    ""description"": ""event description"",
    ""start_time"": ""YYYY-MM-DDTHH:mm:ss"",
    ""end_time"": ""YYYY-MM-DDTHH:mm:ss"",
    ""location"": ""event location"",
    ""attendees"": [""email1"", ""email2""],
    ""extracted_entities"": [""entity1"", ""entity2""],
    ""intent"": ""parsed intent description""
}}

Current time: {DateTime.Now:yyyy-MM-ddTHH:mm:ss}
Today is: {DateTime.Now:dddd, MMMM dd, yyyy}

Parse the input and provide structured JSON response:";
    }

    private CalendarOperation ParseLlmResponse(string llmResponse, string originalInput)
    {
        try
        {
            // Extract JSON from the response
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                var operation = new CalendarOperation
                {
                    OriginalInput = originalInput,
                    ConfidenceScore = parsed.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5
                };

                // Parse operation type
                if (parsed.TryGetProperty("operation_type", out var opType))
                {
                    Enum.TryParse<CalendarOperationType>(opType.GetString(), out var parsedOpType);
                    operation.OperationType = parsedOpType;
                }

                // Parse intent
                if (parsed.TryGetProperty("intent", out var intent))
                {
                    operation.ParsedIntent = intent.GetString() ?? "";
                }

                // Parse extracted entities
                if (parsed.TryGetProperty("extracted_entities", out var entities))
                {
                    operation.ExtractedEntities = entities.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                // Create event data if it's an event operation
                if (operation.OperationType != CalendarOperationType.Unknown)
                {
                    operation.EventData = new Event
                    {
                        Title = parsed.TryGetProperty("title", out var title) ? title.GetString() ?? "Untitled Event" : "Untitled Event",
                        Description = parsed.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        Location = parsed.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : ""
                    };

                    // Parse times
                    if (parsed.TryGetProperty("start_time", out var startTime) && 
                        DateTime.TryParse(startTime.GetString(), out var parsedStartTime))
                    {
                        operation.EventData.StartTime = parsedStartTime;
                    }

                    if (parsed.TryGetProperty("end_time", out var endTime) && 
                        DateTime.TryParse(endTime.GetString(), out var parsedEndTime))
                    {
                        operation.EventData.EndTime = parsedEndTime;
                    }
                }

                return operation;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response, using fallback");
        }

        // Fallback parsing
        return new CalendarOperation
        {
            OriginalInput = originalInput,
            OperationType = CalendarOperationType.Unknown,
            ConfidenceScore = 0.1,
            ParsedIntent = "Unable to parse request clearly",
            EventData = new Event
            {
                Title = "Parsed Event",
                Description = originalInput,
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2)
            }
        };
    }

    private string GetOperationDescription(CalendarOperationType operationType)
    {
        return operationType switch
        {
            CalendarOperationType.CreateEvent => "creating the event",
            CalendarOperationType.UpdateEvent => "updating the event",
            CalendarOperationType.DeleteEvent => "deleting the event",
            CalendarOperationType.FindEvent => "finding events",
            CalendarOperationType.CheckAvailability => "checking availability",
            CalendarOperationType.SuggestMeetingTime => "suggesting meeting times",
            _ => "processing your request"
        };
    }
}