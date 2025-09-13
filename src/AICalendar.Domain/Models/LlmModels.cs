using System.Text.Json;

namespace AICalendar.Domain.Models;

/// <summary>
/// Represents a calendar operation parsed from natural language input
/// </summary>
public class CalendarOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public CalendarOperationType OperationType { get; set; }
    public Event? EventData { get; set; }
    public double ConfidenceScore { get; set; }
    public string OriginalInput { get; set; } = string.Empty;
    public string ParsedIntent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> ExtractedEntities { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of calendar operations that can be performed via natural language
/// </summary>
public enum CalendarOperationType
{
    CreateEvent,
    UpdateEvent,
    DeleteEvent,
    FindEvent,
    CheckAvailability,
    SuggestMeetingTime,
    Unknown
}

/// <summary>
/// Response from LLM processing containing operation details and execution status
/// </summary>
public class LlmResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CalendarOperation? Operation { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Configuration for LLM processing
/// </summary>
public class LlmConfiguration
{
    public string ModelName { get; set; } = "llama3.1:latest";
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public double Temperature { get; set; } = 0.1;
    public int MaxTokens { get; set; } = 1000;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableMcp { get; set; } = true;
    public string McpServerUrl { get; set; } = "http://localhost:8080";
}

/// <summary>
/// Model Context Protocol tool definition for calendar operations
/// </summary>
public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}

/// <summary>
/// MCP tool execution request
/// </summary>
public class McpToolRequest
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// MCP tool execution response
/// </summary>
public class McpToolResponse
{
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// MCP server configuration
/// </summary>
public class McpServerConfiguration
{
    public string ServerName { get; set; } = "AICalendar";
    public string Version { get; set; } = "1.0.0";
    public int Port { get; set; } = 8080;
    public List<McpTool> Tools { get; set; } = new();
    public Dictionary<string, object> Capabilities { get; set; } = new();
}