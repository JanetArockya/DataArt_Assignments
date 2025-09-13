# AI Calendar - Natural Language Calendar Operations

A comprehensive .NET 9 calendar application with AI-powered natural language processing capabilities using local LLMs (Ollama/Llama) and Model Context Protocol (MCP) server integration.

## 🌟 Features

### Core Calendar Functionality
- ✅ **Event Management**: Create, read, update, delete calendar events
- ✅ **Event Validation**: Business rules and data validation
- ✅ **Recurring Events**: Support for repeating calendar events
- ✅ **Event Search**: Find events by various criteria
- ✅ **Event Reminders**: Notification system for upcoming events

### AI-Powered Natural Language Processing
- 🤖 **Local LLM Integration**: Uses Ollama/Llama models for offline processing
- 🗣️ **Natural Language Commands**: Process calendar operations via plain English
- 🔧 **MCP Server**: Model Context Protocol implementation with calendar tools
- 🧠 **Intent Recognition**: Understands user intent and extracts calendar operations
- ⚡ **Smart Suggestions**: Conflict detection and meeting time recommendations

## 🏗️ Architecture

### System Design
```
User Input → NaturalLanguageController → LLM Service → MCP Server → Database
                                      ↓
                              Natural Language Response
```

### Project Structure
```
AICalendar/
├── src/
│   ├── AICalendar.Api/          # Web API & Controllers
│   │   ├── Controllers/         # REST API endpoints
│   │   │   ├── EventsController.cs        # Traditional CRUD operations
│   │   │   └── NaturalLanguageController.cs # AI-powered endpoints
│   │   └── Services/            # Application services
│   │       ├── LlmService.cs    # Ollama/Llama integration
│   │       └── McpService.cs    # MCP server implementation
│   ├── AICalendar.Domain/       # Business Logic & Models
│   │   ├── Entities/            # Domain entities
│   │   ├── Models/              # Domain models including LLM models
│   │   └── Services/            # Service interfaces
│   └── AICalendar.Data/         # Data Access Layer
│       └── Repositories/        # Repository pattern implementation
└── docs/                        # Documentation
```

### MCP Tools Implementation
The MCP server provides 5 specialized calendar tools:

1. **save_event** - Create new calendar events
2. **update_event** - Modify existing events  
3. **cancel_event** - Delete/cancel events
4. **find_events** - Search and retrieve events
5. **check_availability** - Detect conflicts and suggest times

Each tool includes:
- ✅ Input validation with JSON schemas
- ✅ Idempotency handling for safe retries
- ✅ Comprehensive error handling
- ✅ Database transaction support
- ✅ Proper response formatting

## 🚀 Quick Start

### Prerequisites
- **.NET 9 SDK** or later
- **Ollama** for local LLM (https://ollama.ai/)
- **Git** for cloning the repository

### 1. Install Ollama (Local LLM)
```bash
# Install Ollama from https://ollama.ai/
# Pull the Llama model
ollama pull llama3.2
```

### 2. Clone and Setup
```bash
git clone https://github.com/JanetArockya/DataArt_Assignments.git
cd DataArt_Assignments
dotnet restore
dotnet build
```

### 3. Run the Application
```bash
cd src/AICalendar.Api
dotnet run
```

The application will start on `https://localhost:7071` with Swagger UI available for API testing.

### 4. Verify LLM Integration
The application automatically initializes the LLM service on startup. Check the console for:
```
LLM Service initialization: Success
```

If Ollama is not running, you'll see:
```
LLM Service initialization: Failed - Ollama may not be running
```

## 🧪 Testing Natural Language Commands

### Example Commands
Use the `/api/naturallanguage/command` endpoint with these example inputs:

**Creating Events:**
```json
{
  "command": "Schedule a team meeting tomorrow at 2pm for 1 hour",
  "context": "Weekly planning session"
}
```

**Finding Events:**
```json
{
  "command": "What meetings do I have next week?",
  "context": ""
}
```

**Checking Availability:**
```json
{
  "command": "Am I free on Friday afternoon?",
  "context": ""
}
```

**Updating Events:**
```json
{
  "command": "Move the client call to 3pm tomorrow",
  "context": ""
}
```

**Canceling Events:**
```json
{
  "command": "Cancel my 10am appointment",
  "context": ""
}
```

### Response Format
```json
{
  "success": true,
  "message": "I've scheduled your team meeting for tomorrow at 2:00 PM.",
  "operation": {
    "operationType": "CreateEvent",
    "confidenceScore": 0.95,
    "parsedIntent": "schedule meeting",
    "extractedEntities": ["team meeting", "tomorrow", "2pm"]
  },
  "event": {
    "id": 1,
    "title": "Team Meeting",
    "startTime": "2025-09-14T14:00:00Z",
    "endTime": "2025-09-14T15:00:00Z"
  }
}
```

## 🔧 API Endpoints

### Natural Language Processing
- `POST /api/naturallanguage/command` - Process natural language calendar commands
- `GET /api/naturallanguage/health` - Check LLM service status
- `GET /api/naturallanguage/examples` - Get example commands
- `GET /api/naturallanguage/suggestions` - Get smart suggestions

### Traditional Calendar Operations
- `GET /api/events` - List all events
- `POST /api/events` - Create new event
- `GET /api/events/{id}` - Get specific event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

## 🔍 End-to-End Workflow Testing

### 1. Test Natural Language Processing
```bash
curl -X POST "https://localhost:7071/api/naturallanguage/command" \
  -H "Content-Type: application/json" \
  -d '{"command": "Schedule lunch with John tomorrow at noon"}'
```

### 2. Verify Database Persistence
```bash
curl -X GET "https://localhost:7071/api/events"
```

### 3. Query Created Events
```bash
curl -X POST "https://localhost:7071/api/naturallanguage/command" \
  -H "Content-Type: application/json" \
  -d '{"command": "Show me all my meetings with John"}'
```

## ⚠️ Troubleshooting

### Common Issues

**1. LLM Service Failed to Initialize**
```
Error: LLM Service initialization: Failed
```
**Solution:** 
- Ensure Ollama is running: `ollama serve`
- Check if the model is installed: `ollama list`
- Install required model: `ollama pull llama3.2`

**2. MCP Tool Execution Failed**
```
Error: MCP_EXECUTION_FAILED
```
**Solution:**
- Check database connectivity
- Verify event data format in request
- Review application logs for detailed error messages

**3. Natural Language Parsing Issues**
```
Error: PARSING_FAILED
```
**Solution:**
- Ensure command is not empty
- Try more specific language (include times, dates, participants)
- Check example commands for proper format

**4. Application Won't Start**
```
Error: Couldn't find a project to run
```
**Solution:**
- Ensure you're in the correct directory: `cd src/AICalendar.Api`
- Run from project root: `dotnet run --project src/AICalendar.Api`

### Local Development

**Running Without Internet:**
The application is designed to work completely offline once Ollama and the Llama model are installed locally. No internet connection is required for operation.

**Database:**
Uses in-memory database by default for easy testing. For production, configure a persistent database in `appsettings.json`.

## 🧪 Testing Strategy

### Unit Tests
```bash
dotnet test
```

### Integration Tests
The application includes comprehensive integration tests covering:
- Natural language processing workflow
- MCP tool execution
- Database operations
- Error handling scenarios

### Manual Testing Checklist

1. **✅ End-to-End Functionality**
   - [ ] Natural language command creates event in database
   - [ ] Created event can be queried and retrieved
   - [ ] Event modifications persist correctly

2. **✅ Local LLM Integration**
   - [ ] Application works without internet connection
   - [ ] Ollama service responds correctly
   - [ ] LLM generates appropriate responses

3. **✅ MCP Server & Tools**
   - [ ] All 5 MCP tools execute successfully
   - [ ] Proper validation and error handling
   - [ ] Idempotent operations work correctly
   - [ ] Database transactions handle failures

4. **✅ API Orchestration**
   - [ ] LLM correctly selects appropriate MCP tools
   - [ ] Tool execution results in proper database operations
   - [ ] Response generation provides meaningful feedback

## 📚 Technical Implementation Details

### LLM Integration Architecture
- **Ollama Client**: HTTP-based communication with local Ollama server
- **Prompt Engineering**: Structured prompts for calendar operation extraction
- **Response Parsing**: JSON-based parsing with fallback error handling
- **Context Management**: Conversation history and user context preservation

### MCP Protocol Implementation
- **Tool Registration**: Dynamic tool discovery and registration
- **JSON Schema Validation**: Input validation using JSON schemas
- **Request/Response Handling**: Structured communication protocol
- **Error Recovery**: Comprehensive error handling and retry logic

### Database Design
- **Entity Framework Core**: ORM for database operations
- **Repository Pattern**: Clean separation of data access logic
- **Transaction Management**: ACID compliance for data consistency
- **Migration Support**: Database schema versioning

## 🎯 Evaluation Criteria Compliance

✅ **End-to-End Functionality**: Natural language commands persist structured Events in database and allow querying
✅ **Local LLM Integration**: Complete offline operation using Ollama/Llama models
✅ **MCP Server & Tools Design**: 5 specialized tools with validation, idempotency, and error handling
✅ **API and MCP Orchestration**: Proper tool selection and execution by LLM
✅ **Documentation**: Comprehensive setup, usage, and troubleshooting guide

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **Ollama** for providing excellent local LLM capabilities
- **Model Context Protocol** for standardized tool interaction
- **.NET 9** for modern application development features