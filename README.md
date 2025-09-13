# AI Calendar Assignment - DataArt

This repository contains a comprehensive AI Calendar application built as part of the .NET course assignment. The project is structured as a modular monolith using .NET 8 with a clean layered architecture.

## Project Structure

The solution follows a modular monolith architecture with clear separation of concerns:

```
AICalendar/
├── src/
│   ├── AICalendar.Api/          # API Layer - Controllers and endpoints
│   ├── AICalendar.Domain/       # Domain Layer - Business logic and models
│   └── AICalendar.Data/         # Data Layer - Database context and repositories
```

## Architecture Decision

We chose a **Modular Monolith** architecture for the following reasons:

- **Balanced Complexity**: Simpler than microservices but more maintainable than a traditional monolith
- **Clear Boundaries**: Well-defined modules with explicit interfaces
- **Scalability**: Can be easily refactored to microservices if needed
- **Development Speed**: Faster development and debugging compared to distributed systems
- **Deployment Simplicity**: Single deployment unit while maintaining modular design

## Layers Description

### 1. Domain Layer (`AICalendar.Domain`)
- Contains core business models: `Event`, `Attendee`, `Reminder`, `RecurrenceRule`
- Business services and interfaces
- Repository abstractions
- Domain logic and validation

### 2. Data Layer (`AICalendar.Data`)
- Entity Framework Core `CalendarDbContext`
- Repository implementations
- Database configurations
- Data persistence logic

### 3. API Layer (`AICalendar.Api`)
- REST API controllers
- HTTP endpoints for calendar operations
- Request/response DTOs
- API configuration and middleware

## Core Features

The AI Calendar supports:

### Event Management
- Create, update, and cancel events
- Event scheduling and rescheduling
- Time zone support
- All-day event support

### Attendee Management
- Add/remove attendees to events
- Attendee status tracking (Pending, Accepted, Declined, Tentative)
- Organizer designation

### Advanced Features
- Recurring events with flexible rules
- Reminders with multiple types (Email, Popup, SMS)
- Client reference ID for idempotency
- Date range queries
- Attendee-based event filtering

## Technology Stack

- **.NET 8** - Target framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with In-Memory database for development
- **Swagger/OpenAPI** - API documentation
- **C# 12** - Programming language with latest features

## Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Building the Project

```bash
# Clone the repository
git clone https://github.com/JanetArockya/DataArt_Assignments.git
cd DataArt_Assignments

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/AICalendar.Api/AICalendar.Api.csproj
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7001/swagger`

## API Endpoints

### Events
- `GET /api/events` - Get all events
- `GET /api/events/{id}` - Get event by ID
- `GET /api/events/range?startDate={date}&endDate={date}` - Get events in date range
- `GET /api/events/attendee/{email}` - Get events for specific attendee
- `POST /api/events` - Create new event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Cancel event
- `PATCH /api/events/{id}/reschedule` - Reschedule event
- `POST /api/events/{id}/attendees` - Add attendee to event
- `DELETE /api/events/{id}/attendees/{email}` - Remove attendee from event

## Development Progress

### ✅ Homework 1 - Initial Project Setup
- [x] Created public GitHub repository
- [x] Chose Modular Monolith architecture
- [x] Set up .NET 8 solution with three layers:
  - [x] API layer with controllers and endpoints
  - [x] Data layer with EF Core and repositories
  - [x] Domain layer with services and business logic
- [x] Implemented core models: Event, Attendee, Reminder, RecurrenceRule
- [x] Created repository pattern with proper abstractions
- [x] Set up dependency injection and service registration
- [x] Added Swagger for API documentation

### ✅ Homework 2 - API Design & Contract Definition

#### API Style Decision: REST
After thorough analysis of **gRPC vs GraphQL vs REST**, we chose **REST** for the AI Calendar API.

**Comparison Summary:**
- **REST**: ✅ Universal client support, mature ecosystem, simple CRUD operations
- **GraphQL**: � Great for complex queries but learning curve and security concerns
- **gRPC**: 🟡 Excellent performance but limited browser support

**Decision Factors:**
1. **Universal Compatibility**: All clients (web, mobile, AI systems) support REST
2. **Team Productivity**: Familiar technology stack reduces development time
3. **Ecosystem Maturity**: Rich tooling for documentation, testing, and monitoring
4. **Simple Domain**: Calendar CRUD operations map naturally to REST verbs

#### Contract Overview
- **API Version**: v1.0.0 with URL-based versioning (`/api/v1/`)
- **Contract Format**: OpenAPI 3.1 specification
- **Location**: [`/contracts/openapi.yaml`](./contracts/openapi.yaml)
- **Breaking Change Policy**: 6-month deprecation notice with migration guides

#### Core API Endpoints

##### Event Management
```http
GET    /api/v1/events                    # List events with pagination & filtering
POST   /api/v1/events                    # Create event (idempotent via clientReferenceId)
GET    /api/v1/events/{id}               # Get specific event
PUT    /api/v1/events/{id}               # Update event
DELETE /api/v1/events/{id}               # Cancel event
PATCH  /api/v1/events/{id}/reschedule    # Reschedule event
```

##### Attendee Management
```http
GET    /api/v1/events/{id}/attendees           # List event attendees
POST   /api/v1/events/{id}/attendees           # Add attendee
DELETE /api/v1/events/{id}/attendees/{email}   # Remove attendee
```

##### Specialized Queries
```http
GET /api/v1/events/range?startDate={date}&endDate={date}  # Date range query
GET /api/v1/events/attendee/{email}                       # Events by attendee
GET /api/v1/health                                        # Health check
```

#### Example API Calls

**Create Event:**
```bash
curl -X POST https://localhost:7001/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Team Standup",
    "startTime": "2024-09-14T09:00:00Z",
    "endTime": "2024-09-14T09:30:00Z",
    "timeZone": "America/New_York",
    "clientReferenceId": "standup-2024-09-14",
    "attendees": [
      {"name": "John Doe", "email": "john@company.com", "isOrganizer": true}
    ]
  }'
```

**Get Events by Date Range:**
```bash
curl "https://localhost:7001/api/v1/events/range?startDate=2024-09-01T00:00:00Z&endDate=2024-09-30T23:59:59Z&timeZone=UTC"
```

**List Events with Pagination:**
```bash
curl "https://localhost:7001/api/v1/events?page=1&size=50&sort=startTime:asc&attendee=john@company.com"
```

#### Versioning & Deprecation Policy
- **Current Version**: v1.0.0
- **Versioning Strategy**: URL-based (`/api/v1/`, `/api/v2/`)
- **Breaking Changes**: Require new major version
- **Deprecation Timeline**: 6 months minimum support
- **Migration Support**: Detailed guides and transition periods

#### Security Implementation
- **Authentication**: JWT Bearer tokens
- **Authorization**: Role-based access control
- **Rate Limiting**: 1000 requests/hour per user, 10000/hour per IP
- **Input Validation**: Comprehensive schema validation
- **Error Handling**: Structured error responses with trace IDs

#### Local Development Setup
```bash
# Clone repository
git clone https://github.com/JanetArockya/DataArt_Assignments.git
cd DataArt_Assignments

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API
dotnet run --project src/AICalendar.Api/AICalendar.Api.csproj
```

**API URLs:**
- Development: `https://localhost:7001/api/v1`
- Swagger UI: `https://localhost:7001/swagger`
- Health Check: `https://localhost:7001/api/v1/health`

#### Performance Features
- **Pagination**: Page-based with metadata (page, size, total, hasNext/Previous)
- **Filtering**: Date ranges, attendees, status, location
- **Sorting**: Multiple sort criteria supported
- **Caching**: HTTP headers and conditional requests
- **Rate Limiting**: Configurable limits with headers

#### Known Limitations
- **Real-time Updates**: Currently polling-based; WebSocket support planned
- **Bulk Operations**: Limited batch support; individual API calls required
- **File Attachments**: Not yet implemented
- **Advanced Recurrence**: Basic patterns only; complex rules in development
- **Multi-tenant**: Single tenant support; isolation features planned

### 🚧 Next Steps (Homework 3-4)
- [ ] .NET 9 upgrade and C# 13 features implementation
- [ ] Local LLM integration and MCP server development
- [ ] End-to-end testing and validation

## Contributing

This is an educational project for the .NET course completion. The implementation follows clean architecture principles and best practices for enterprise applications.

## License

This project is part of the DataArt .NET course assignment.