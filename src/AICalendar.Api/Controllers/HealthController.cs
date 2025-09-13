using AICalendar.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AICalendar.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public ActionResult<HealthResponse> GetHealth()
    {
        try
        {
            var response = new HealthResponse
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var response = new HealthResponse
            {
                Status = "unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };

            return StatusCode(503, response);
        }
    }
}