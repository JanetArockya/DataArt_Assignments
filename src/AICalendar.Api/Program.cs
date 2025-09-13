using AICalendar.Data;
using AICalendar.Data.Repositories;
using AICalendar.Domain.Repositories;
using AICalendar.Domain.Services;
using AICalendar.Domain.Models;
using AICalendar.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseInMemoryDatabase("CalendarDb"));

// Add repositories and services
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IMcpService, McpService>();
builder.Services.AddHttpClient<LlmService>();

// Configure LLM settings
builder.Services.Configure<LlmConfiguration>(options =>
{
    options.BaseUrl = "http://localhost:11434"; // Default Ollama port
    options.ModelName = "llama3.2"; // Default model
    options.MaxTokens = 4096;
    options.Temperature = 0.1f;
    options.Timeout = TimeSpan.FromSeconds(30);
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database and LLM service
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Initialize LLM service
    var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
    var config = new LlmConfiguration
    {
        BaseUrl = "http://localhost:11434",
        ModelName = "llama3.2",
        MaxTokens = 4096,
        Temperature = 0.1f,
        Timeout = TimeSpan.FromSeconds(30)
    };
    
    var initialized = await llmService.InitializeAsync(config);
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("LLM Service initialization: {Status}", initialized ? "Success" : "Failed - Ollama may not be running");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
