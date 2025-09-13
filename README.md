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

### 🚧 Next Steps (Homework 2-4)
- [ ] API style comparison and contract definition
- [ ] .NET 9 upgrade and C# 13 features implementation
- [ ] Local LLM integration and MCP server development

## Contributing

This is an educational project for the .NET course completion. The implementation follows clean architecture principles and best practices for enterprise applications.

## License

This project is part of the DataArt .NET course assignment.