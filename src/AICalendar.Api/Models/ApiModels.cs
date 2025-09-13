namespace AICalendar.Api.Models;

public class PaginationResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int Size { get; set; }
    public long Total { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

public class ErrorResponse
{
    public ErrorDetails Error { get; set; } = new();
}

public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<FieldError> Details { get; set; } = new();
    public string TraceId { get; set; } = string.Empty;
}

public class FieldError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}