using AICalendar.Domain.Models;

namespace AICalendar.Domain.Services;

/// <summary>
/// Service interface for Large Language Model integration with calendar operations
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Process natural language input and extract calendar operation intent
    /// </summary>
    /// <param name="naturalLanguageInput">User's natural language command</param>
    /// <param name="context">Optional context for better understanding</param>
    /// <returns>Parsed calendar operation with confidence score</returns>
    Task<LlmResponse> ProcessNaturalLanguageAsync(string naturalLanguageInput, Dictionary<string, object>? context = null);

    /// <summary>
    /// Execute a calendar operation using the MCP server
    /// </summary>
    /// <param name="operation">The calendar operation to execute</param>
    /// <returns>Result of the operation execution</returns>
    Task<LlmResponse> ExecuteOperationAsync(CalendarOperation operation);

    /// <summary>
    /// Generate smart suggestions for calendar events
    /// </summary>
    /// <param name="userInput">User's partial input or context</param>
    /// <param name="existingEvents">List of existing events for context</param>
    /// <returns>List of suggested events or improvements</returns>
    Task<List<Event>> GenerateEventSuggestionsAsync(string userInput, List<Event> existingEvents);

    /// <summary>
    /// Check for scheduling conflicts and provide alternatives
    /// </summary>
    /// <param name="proposedEvent">Event to check for conflicts</param>
    /// <param name="existingEvents">List of existing events</param>
    /// <returns>Conflict analysis and alternative suggestions</returns>
    Task<LlmResponse> CheckConflictsAsync(Event proposedEvent, List<Event> existingEvents);

    /// <summary>
    /// Generate natural language response for user
    /// </summary>
    /// <param name="operation">Calendar operation that was performed</param>
    /// <param name="result">Result of the operation</param>
    /// <returns>Human-friendly response message</returns>
    Task<string> GenerateResponseAsync(CalendarOperation operation, bool success, string? details = null);

    /// <summary>
    /// Initialize connection to local LLM (Ollama/Llama)
    /// </summary>
    /// <param name="configuration">LLM configuration settings</param>
    /// <returns>True if connection successful</returns>
    Task<bool> InitializeAsync(LlmConfiguration configuration);

    /// <summary>
    /// Check if LLM service is available and responsive
    /// </summary>
    /// <returns>Health status of the LLM service</returns>
    Task<bool> HealthCheckAsync();
}

/// <summary>
/// Service interface for Model Context Protocol server operations
/// </summary>
public interface IMcpService
{
    /// <summary>
    /// Initialize MCP server with available tools
    /// </summary>
    /// <param name="configuration">MCP server configuration</param>
    /// <returns>True if initialization successful</returns>
    Task<bool> InitializeAsync(McpServerConfiguration configuration);

    /// <summary>
    /// Get list of available MCP tools
    /// </summary>
    /// <returns>List of tools the MCP server provides</returns>
    Task<List<McpTool>> GetAvailableToolsAsync();

    /// <summary>
    /// Execute an MCP tool with provided arguments
    /// </summary>
    /// <param name="request">Tool execution request</param>
    /// <returns>Tool execution result</returns>
    Task<McpToolResponse> ExecuteToolAsync(McpToolRequest request);

    /// <summary>
    /// Register a new tool with the MCP server
    /// </summary>
    /// <param name="tool">Tool definition to register</param>
    /// <returns>True if registration successful</returns>
    Task<bool> RegisterToolAsync(McpTool tool);

    /// <summary>
    /// Check if MCP server is running and healthy
    /// </summary>
    /// <returns>Health status of MCP server</returns>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Start the MCP server
    /// </summary>
    /// <returns>True if server started successfully</returns>
    Task<bool> StartServerAsync();

    /// <summary>
    /// Stop the MCP server
    /// </summary>
    /// <returns>True if server stopped successfully</returns>
    Task<bool> StopServerAsync();
}