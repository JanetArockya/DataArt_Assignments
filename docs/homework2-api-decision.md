# API Style Comparison & Decision - Homework 2

## Domain Analysis for AI Calendar

The AI Calendar application has the following domain requirements:

### Core Entities
- **Events**: Calendar events with start/end times, attendees, reminders, recurrence
- **Attendees**: People invited to events with status tracking
- **Reminders**: Notifications for upcoming events
- **Recurrence Rules**: Complex patterns for repeating events

### Client Types
- **Web Browsers**: Modern SPAs (React, Angular, Vue)
- **Mobile Apps**: iOS and Android native/hybrid apps
- **Server-to-Server**: Integration with external calendar systems
- **AI/LLM Integration**: Natural language processing for event creation

### Network Patterns
- **Real-time Updates**: Calendar changes need to be reflected immediately
- **Bulk Operations**: Creating multiple events, batch invitations
- **Complex Queries**: Date ranges, attendee filtering, timezone conversions
- **File Uploads**: Attachments, calendar imports

## API Style Comparison Matrix

| Aspect | REST | GraphQL | gRPC |
|--------|------|---------|------|
| **Domain Fit** | ✅ Good - CRUD operations map well to calendar entities | 🟡 Excellent - Complex queries, selective field fetching | 🟡 Moderate - Strong typing helps, but HTTP/JSON clients need wrappers |
| **Client Types** | ✅ Universal support - all clients speak HTTP | ✅ Excellent browser support, mobile SDK available | ❌ Limited browser support, needs envoy proxy |
| **Network Efficiency** | 🟡 Over/under-fetching possible | ✅ Exact data fetching, single round-trip | ✅ Binary protocol, HTTP/2 streaming |
| **Ecosystem & Tooling** | ✅ Mature, extensive tooling (Swagger, Postman) | ✅ Rich tooling (GraphiQL, Apollo) | 🟡 Growing ecosystem, excellent for .NET |
| **Developer Experience** | ✅ Simple, well-understood | 🟡 Learning curve, but powerful | 🟡 Requires proto definitions, code generation |
| **Versioning** | ✅ URL/header versioning, well-established | 🟡 Schema evolution, deprecation warnings | ✅ Proto versioning, backward compatibility |
| **Security** | ✅ Standard HTTP security, rate limiting | 🟡 Query complexity attacks, needs rate limiting | ✅ Strong typing prevents many issues |
| **Real-time** | 🟡 SSE/WebSockets add-on | ✅ Built-in subscriptions | ✅ Bidirectional streaming |
| **Caching** | ✅ Standard HTTP caching | 🟡 More complex due to dynamic queries | 🟡 Custom caching solutions needed |
| **Error Handling** | ✅ Standard HTTP status codes | 🟡 Custom error extensions | ✅ Rich error details with google.rpc.Status |

## Decision: REST API

**Chosen API Style: REST**

### Justification

For the AI Calendar project, **REST** is the optimal choice because:

1. **Universal Client Support**: All client types (web, mobile, server-to-server, AI systems) can easily consume REST APIs
2. **Ecosystem Maturity**: Extensive tooling for documentation, testing, and monitoring
3. **Team Familiarity**: Most developers are comfortable with REST
4. **Simple Operations**: Calendar CRUD operations map naturally to REST verbs
5. **Easy Integration**: External calendar systems and AI/LLM services typically use REST
6. **Deployment Simplicity**: No need for special proxies or binary protocol handling

### Trade-offs Accepted

- **Network Efficiency**: Accepting some over-fetching for simplicity
- **Real-time Features**: Will implement WebSocket endpoints separately for live updates
- **Complex Queries**: Will design efficient query parameters and filtering

### Hybrid Approach

While REST is the primary API style, we'll implement:
- **WebSocket endpoints** for real-time calendar updates
- **GraphQL endpoint** (future consideration) for complex dashboard queries
- **HTTP/2** for improved performance

## REST API Design Principles

### Resource-Based URLs
```
GET    /api/events                 # List events
POST   /api/events                 # Create event
GET    /api/events/{id}            # Get specific event
PUT    /api/events/{id}            # Update event
DELETE /api/events/{id}            # Cancel event
PATCH  /api/events/{id}/reschedule # Reschedule event
```

### Query Parameters for Filtering
```
GET /api/events?startDate=2024-01-01&endDate=2024-12-31&timezone=UTC
GET /api/events?attendee=john@example.com&status=confirmed
GET /api/events?recurrence=true&location=Office
```

### Nested Resources for Relationships
```
GET    /api/events/{id}/attendees           # List event attendees
POST   /api/events/{id}/attendees           # Add attendee
DELETE /api/events/{id}/attendees/{email}   # Remove attendee
GET    /api/events/{id}/reminders           # List event reminders
POST   /api/events/{id}/reminders           # Add reminder
```

## Error Handling Strategy

### Standard HTTP Status Codes
- `200 OK` - Successful GET, PUT, PATCH
- `201 Created` - Successful POST
- `204 No Content` - Successful DELETE
- `400 Bad Request` - Invalid input data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Access denied
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Business rule violation (e.g., double booking)
- `422 Unprocessable Entity` - Validation errors
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server errors

### Error Response Format
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "The request contains invalid data",
    "details": [
      {
        "field": "endTime",
        "message": "End time must be after start time"
      }
    ],
    "traceId": "abc123"
  }
}
```

## Security Implementation

### Authentication & Authorization
- **JWT Bearer Tokens** for authentication
- **OAuth 2.0** for third-party integrations
- **Role-based access control** (organizer, attendee, viewer)

### Rate Limiting
- **Per-user limits**: 1000 requests/hour
- **Per-IP limits**: 10000 requests/hour
- **Burst limits**: 100 requests/minute

### Input Validation
- **Schema validation** for all request bodies
- **Query parameter validation** and sanitization
- **File upload restrictions** for attachments

## Performance Optimizations

### Caching Strategy
- **HTTP caching headers** for static data
- **ETags** for conditional requests
- **Redis caching** for frequently accessed data

### Pagination
```
GET /api/events?page=1&size=50&sort=startTime:asc
```

Response includes pagination metadata:
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "size": 50,
    "total": 500,
    "totalPages": 10,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## Versioning Strategy

### URL-based Versioning
- Current: `/api/v1/events`
- Future: `/api/v2/events`

### Deprecation Policy
- **6-month notice** for breaking changes
- **Sunset headers** in responses
- **Migration guides** in documentation

### Backward Compatibility
- **Additive changes only** within major versions
- **Optional fields** for new features
- **Default values** for missing fields

## Next Steps

1. ✅ API style comparison and decision documented
2. 🔄 Define OpenAPI 3.1 specification
3. 🔄 Implement enhanced server endpoints
4. 🔄 Add authentication and rate limiting
5. 🔄 Update documentation