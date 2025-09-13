# Llama AI Model Research - Open Source LLM Integration

## Overview of Llama Models

**Llama (Large Language Model Meta AI)** is Meta's family of open-source large language models designed for various natural language processing tasks. The latest version, **Llama 3.3**, offers significant improvements in reasoning, code generation, and instruction following.

## Model Variants and Specifications

### Llama 3.3 70B Instruct
- **Parameters**: 70 billion
- **Context Length**: 128,000 tokens
- **Use Case**: Best for complex reasoning and instruction following
- **Hardware Requirements**: 40+ GB VRAM (A100/H100) or CPU with 128+ GB RAM

### Llama 3.2 (3B/1B)
- **Parameters**: 1B and 3B variants
- **Context Length**: 128,000 tokens
- **Use Case**: Edge deployment, mobile devices, lightweight applications
- **Hardware Requirements**: 4-8 GB VRAM or 8-16 GB CPU RAM

### Llama 3.1 8B Instruct
- **Parameters**: 8 billion
- **Context Length**: 128,000 tokens
- **Use Case**: Balanced performance and resource usage
- **Hardware Requirements**: 16+ GB VRAM or 32+ GB CPU RAM

## Running Llama Models Locally

### Option 1: Ollama (Recommended for Development)

**Installation:**
```bash
# Windows
winget install Ollama.Ollama

# macOS
brew install ollama

# Linux
curl -fsSL https://ollama.ai/install.sh | sh
```

**Running Models:**
```bash
# Pull and run Llama 3.2 3B (lightweight)
ollama pull llama3.2:3b
ollama run llama3.2:3b

# Pull and run Llama 3.1 8B (balanced)
ollama pull llama3.1:8b
ollama run llama3.1:8b

# Pull and run Llama 3.3 70B (requires significant resources)
ollama pull llama3.3:70b
ollama run llama3.3:70b
```

**API Integration:**
```bash
# Start Ollama server
ollama serve

# Test API endpoint
curl http://localhost:11434/api/generate -d '{
  "model": "llama3.2:3b",
  "prompt": "Create a meeting for tomorrow at 2 PM with John Doe"
}'
```

### Option 2: LM Studio
- **GUI Application**: User-friendly interface for model management
- **Model Hub**: Easy browsing and downloading of quantized models
- **Local API**: OpenAI-compatible API server
- **Cross-platform**: Windows, macOS, Linux support

### Option 3: Text Generation WebUI
- **Web Interface**: Browser-based model interaction
- **Advanced Features**: Model switching, parameter tuning, extensions
- **API Support**: OpenAI-compatible endpoints
- **Model Formats**: Supports GGUF, GPTQ, and other formats

## Integration with AI Calendar Backend

### Architecture for LLM Integration

```csharp
// Service interface for LLM integration
public interface ILlmService
{
    Task<IntentClassificationResult> ClassifyIntentAsync(string naturalLanguagePrompt);
    Task<EventExtractionResult> ExtractEventDetailsAsync(string prompt);
    Task<ToolSelectionResult> SelectToolAsync(string intent, Dictionary<string, object> entities);
}

// Implementation using local Ollama
public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmService> _logger;
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";

    public async Task<IntentClassificationResult> ClassifyIntentAsync(string prompt)
    {
        var classificationPrompt = $@"
Classify the following request into one of these intents:
- CREATE: Creating a new event
- UPDATE: Modifying an existing event  
- CANCEL: Cancelling an event
- LIST: Retrieving events
- RESCHEDULE: Changing event time

Request: {prompt}

Respond with only the intent name.";

        var response = await CallOllamaAsync(classificationPrompt);
        return new IntentClassificationResult(response.Trim().ToUpperInvariant());
    }

    public async Task<EventExtractionResult> ExtractEventDetailsAsync(string prompt)
    {
        var extractionPrompt = $@"
Extract event details from the following request and return as JSON:
{{
  ""title"": ""string"",
  ""startTime"": ""ISO 8601 datetime"",
  ""endTime"": ""ISO 8601 datetime"",
  ""location"": ""string or null"",
  ""attendees"": [{{""name"": ""string"", ""email"": ""string""}}],
  ""description"": ""string or null""
}}

Request: {prompt}

Return only valid JSON.";

        var response = await CallOllamaAsync(extractionPrompt);
        return JsonSerializer.Deserialize<EventExtractionResult>(response);
    }

    private async Task<string> CallOllamaAsync(string prompt)
    {
        var request = new
        {
            model = "llama3.2:3b",
            prompt = prompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(OllamaEndpoint, request);
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return result?.Response ?? string.Empty;
    }
}
```

### Natural Language Endpoint Implementation

```csharp
[HttpPost("natural-language")]
public async Task<ActionResult<Event>> ProcessNaturalLanguageRequest([FromBody] NaturalLanguageRequest request)
{
    try
    {
        // Step 1: Classify intent
        var intent = await _llmService.ClassifyIntentAsync(request.Prompt);
        
        if (intent.Intent != "CREATE")
        {
            return BadRequest(new { error = "Only event creation is currently supported" });
        }

        // Step 2: Extract event details
        var eventDetails = await _llmService.ExtractEventDetailsAsync(request.Prompt);
        
        // Step 3: Validate extracted data
        if (string.IsNullOrEmpty(eventDetails.Title))
        {
            return BadRequest(new { 
                error = "MISSING_FIELDS", 
                missing = new[] { "title" },
                message = "Event title is required but could not be extracted from the prompt"
            });
        }

        if (eventDetails.StartTime == default || eventDetails.EndTime == default)
        {
            return BadRequest(new { 
                error = "MISSING_FIELDS", 
                missing = new[] { "startTime", "endTime" },
                message = "Event start and end times are required"
            });
        }

        // Step 4: Create event via MCP tools
        var eventToCreate = new Event
        {
            Title = eventDetails.Title,
            StartTime = eventDetails.StartTime,
            EndTime = eventDetails.EndTime,
            Location = eventDetails.Location,
            Description = eventDetails.Description,
            ClientReferenceId = $"nlp-{Guid.NewGuid()}"
        };

        // Add attendees if extracted
        foreach (var attendee in eventDetails.Attendees ?? [])
        {
            eventToCreate.Attendees.Add(new Attendee
            {
                Name = attendee.Name,
                Email = attendee.Email,
                IsOrganizer = false
            });
        }

        var createdEvent = await _eventService.CreateEventAsync(eventToCreate);
        
        _logger.LogInformation("NLP Event Created: LLM Output: {Intent}, MCP Call: CreateEvent, DB ID: {EventId}", 
            intent.Intent, createdEvent.Id);

        return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing natural language request");
        return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to process request" });
    }
}
```

## Benefits for AI Calendar Project

### 1. Natural Language Interface
- **User-Friendly**: Users can create events using natural language
- **No Learning Curve**: Intuitive interaction without complex forms
- **Voice Integration**: Can be integrated with speech-to-text systems

### 2. Intelligent Entity Extraction
- **Date/Time Parsing**: "tomorrow at 2 PM" → structured datetime
- **Attendee Detection**: "with John and Mary" → attendee list
- **Location Recognition**: "in conference room A" → location field

### 3. Intent Classification
- **Multi-purpose**: Single endpoint for various calendar operations
- **Context Awareness**: Understanding complex requests
- **Ambiguity Resolution**: Asking for clarification when needed

### 4. Offline Capability
- **Privacy**: No data sent to external APIs
- **Reliability**: Works without internet connection
- **Cost Effective**: No per-request charges

## Implementation Considerations

### Hardware Requirements
- **Development**: Llama 3.2 3B (8 GB RAM)
- **Production**: Llama 3.1 8B (32 GB RAM) for better accuracy
- **Enterprise**: Llama 3.3 70B (128 GB RAM) for optimal performance

### Performance Optimization
- **Model Caching**: Keep model loaded in memory
- **Batch Processing**: Process multiple requests together
- **Quantization**: Use 4-bit or 8-bit quantized models
- **GPU Acceleration**: CUDA or ROCm for faster inference

### Error Handling
- **Validation**: Strict validation of LLM outputs
- **Fallbacks**: Default values for missing data
- **User Feedback**: Clear error messages for ambiguous requests
- **Retry Logic**: Handle model timeout or failure scenarios

### Security Considerations
- **Input Sanitization**: Prevent prompt injection attacks
- **Output Validation**: Ensure LLM responses are safe
- **Rate Limiting**: Prevent resource exhaustion
- **Audit Logging**: Track all LLM interactions

## Recommended Setup for Development

1. **Install Ollama** for easy model management
2. **Start with Llama 3.2 3B** for development and testing
3. **Implement basic intent classification** first
4. **Add entity extraction** for event details
5. **Integrate with existing Event API** endpoints
6. **Add comprehensive error handling** and validation
7. **Scale to larger models** based on accuracy requirements

## Future Enhancements

- **Multi-turn Conversations**: Handle follow-up questions
- **Calendar Context**: Consider existing events when scheduling
- **Smart Suggestions**: Propose optimal meeting times
- **Language Support**: Multi-language natural language processing
- **Voice Integration**: Speech-to-text and text-to-speech capabilities