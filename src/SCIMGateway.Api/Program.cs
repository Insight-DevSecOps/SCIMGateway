using SCIMGateway.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configure Services (Dependency Injection)
// ============================================================================

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Add configuration
builder.Services.Configure<ScimGatewayOptions>(
    builder.Configuration.GetSection("ScimGateway"));

// Add SCIM Gateway core services
builder.Services.AddScimGatewayCore();

// Add health checks
builder.Services.AddHealthChecks();

// Add controllers for SCIM endpoints
builder.Services.AddControllers();

// ============================================================================
// Build Application
// ============================================================================

var app = builder.Build();

// ============================================================================
// Configure Middleware Pipeline
// ============================================================================

// Health check endpoint (FR-053)
app.MapHealthChecks("/health");

// HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Routing
app.UseRouting();

// Authentication & Authorization (to be configured in Phase 2)
// app.UseAuthentication();
// app.UseAuthorization();

// Map controllers for SCIM endpoints
app.MapControllers();

// ============================================================================
// Run Application
// ============================================================================

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
