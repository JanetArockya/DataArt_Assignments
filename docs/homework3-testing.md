# Homework 3 Testing - AI Calendar API Endpoints

## Testing Strategy

This document outlines the testing approach for verifying that all required endpoints work correctly after the .NET 9 upgrade and C# 13 feature implementation.

## Required API Functionality Tests

### 1. List All People (Events with Attendees)
**Endpoint**: `GET /api/v1/events`
**Purpose**: Retrieve all events and their attendees

**Test Cases**:
- Empty database should return empty array
- Events with attendees should include attendee details
- Pagination should work correctly
- Filtering by attendee email should work

### 2. Book Meeting with One Person
**Endpoint**: `POST /api/v1/events`
**Purpose**: Create event with single attendee

**Test Data**:
```json
{
  "title": "One-on-One Meeting",
  "startTime": "2024-09-15T10:00:00Z",
  "endTime": "2024-09-15T11:00:00Z",
  "timeZone": "UTC",
  "attendees": [
    {
      "name": "John Doe",
      "email": "john.doe@company.com",
      "isOrganizer": true
    }
  ]
}
```

### 3. Book Meeting with Many People
**Endpoint**: `POST /api/v1/events`
**Purpose**: Create event with multiple attendees

**Test Data**:
```json
{
  "title": "Team Meeting",
  "startTime": "2024-09-15T14:00:00Z",
  "endTime": "2024-09-15T15:00:00Z",
  "timeZone": "UTC",
  "attendees": [
    {
      "name": "John Doe",
      "email": "john.doe@company.com",
      "isOrganizer": true
    },
    {
      "name": "Jane Smith",
      "email": "jane.smith@company.com",
      "isOrganizer": false
    },
    {
      "name": "Bob Johnson",
      "email": "bob.johnson@company.com",
      "isOrganizer": false
    }
  ]
}
```

### 4. Cancel Meeting
**Endpoint**: `DELETE /api/v1/events/{id}`
**Purpose**: Cancel an existing event

**Expected Behavior**:
- Event status should change to 'Cancelled'
- All attendees should be notified (via status change)
- Returns 204 No Content on success

### 5. Reschedule Meeting
**Endpoint**: `PATCH /api/v1/events/{id}/reschedule`
**Purpose**: Change event date/time

**Test Data**:
```json
{
  "newStartTime": "2024-09-15T16:00:00Z",
  "newEndTime": "2024-09-15T17:00:00Z"
}
```

## C# 13 Features Verification

### Partial Properties Implementation
The Event model now uses C# 13 partial properties for:

1. **Status Property**: With change tracking and business logic
2. **StartTime Property**: With validation ensuring it's before EndTime
3. **EndTime Property**: With validation ensuring it's after StartTime  
4. **Title Property**: With trimming and length validation

**Benefits Demonstrated**:
- **Separation of Concerns**: Property declarations vs implementations
- **Enhanced Validation**: Business logic embedded in property setters
- **Change Tracking**: Automatic UpdatedAt timestamp updates
- **Business Rules**: Status changes trigger attendee updates

## Manual Testing Instructions

### Prerequisites
1. Ensure .NET 9.0.305 is installed
2. Build the solution: `dotnet build`
3. Run the API: `dotnet run --project src/AICalendar.Api/AICalendar.Api.csproj`
4. API will be available at: `https://localhost:7001/api/v1`

### Test Sequence

#### 1. Health Check
```bash
curl https://localhost:7001/api/v1/health
```

**Expected Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-09-13T...",
  "version": "1.0.0"
}
```

#### 2. List Events (Initially Empty)
```bash
curl https://localhost:7001/api/v1/events
```

**Expected Response**:
```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "size": 50,
    "total": 0,
    "totalPages": 0,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

#### 3. Create One-on-One Meeting
```bash
curl -X POST https://localhost:7001/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "One-on-One Meeting",
    "startTime": "2024-09-15T10:00:00Z",
    "endTime": "2024-09-15T11:00:00Z",
    "timeZone": "UTC",
    "clientReferenceId": "meeting-1",
    "attendees": [
      {
        "name": "John Doe",
        "email": "john.doe@company.com",
        "isOrganizer": true
      }
    ]
  }'
```

#### 4. Create Team Meeting
```bash
curl -X POST https://localhost:7001/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Team Meeting",
    "startTime": "2024-09-15T14:00:00Z",
    "endTime": "2024-09-15T15:00:00Z",
    "timeZone": "UTC",
    "clientReferenceId": "meeting-2",
    "attendees": [
      {
        "name": "John Doe",
        "email": "john.doe@company.com",
        "isOrganizer": true
      },
      {
        "name": "Jane Smith",
        "email": "jane.smith@company.com",
        "isOrganizer": false
      },
      {
        "name": "Bob Johnson",
        "email": "bob.johnson@company.com",
        "isOrganizer": false
      }
    ]
  }'
```

#### 5. List Events by Attendee
```bash
curl "https://localhost:7001/api/v1/events/attendee/john.doe@company.com"
```

#### 6. Reschedule Meeting
```bash
# First get the event ID from previous responses, then:
curl -X PATCH https://localhost:7001/api/v1/events/1/reschedule \
  -H "Content-Type: application/json" \
  -d '{
    "newStartTime": "2024-09-15T16:00:00Z",
    "newEndTime": "2024-09-15T17:00:00Z"
  }'
```

#### 7. Cancel Meeting
```bash
curl -X DELETE https://localhost:7001/api/v1/events/1
```

#### 8. Verify Cancellation
```bash
curl https://localhost:7001/api/v1/events/1
```

**Expected**: Event should have status "Cancelled" and attendee statuses should be "Declined"

## Test Results Verification

### Success Criteria
- ✅ All endpoints return appropriate HTTP status codes
- ✅ JSON responses follow OpenAPI specification
- ✅ Event creation works for single and multiple attendees
- ✅ Event cancellation updates status correctly
- ✅ Event rescheduling validates new times
- ✅ Pagination and filtering work as expected
- ✅ C# 13 partial properties provide enhanced validation
- ✅ Error handling returns structured error responses

### Performance Verification
- Response times under 100ms for simple operations
- Proper handling of concurrent requests
- Memory usage remains stable during testing

### Data Integrity
- Events are persisted correctly in in-memory database
- Relationships between events and attendees maintained
- Validation rules enforced by partial properties

## Conclusion

The AI Calendar API successfully demonstrates:
1. **Complete CRUD operations** for calendar events
2. **Advanced C# 13 features** with practical business value
3. **Robust error handling** and validation
4. **RESTful API design** following OpenAPI 3.1 specification
5. **.NET 9 compatibility** with updated packages

All required functionality for Homework 3 is implemented and ready for testing.