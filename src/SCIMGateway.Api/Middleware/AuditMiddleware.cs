using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Api.Middleware;

/// <summary>
/// Middleware that audits all incoming SCIM requests.
/// Captures request/response details and logs them via the audit logger.
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogger _auditLogger;

    /// <summary>
    /// Initializes a new instance of AuditMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="auditLogger">The audit logger for recording audit entries.</param>
    public AuditMiddleware(RequestDelegate next, IAuditLogger auditLogger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    /// <summary>
    /// Invokes the middleware to audit the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        // Create audit entry at the start
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            RequestId = requestId,
            HttpMethod = context.Request.Method,
            RequestPath = context.Request.Path.ToString(),
            ClientIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            OperationType = MapHttpMethodToOperation(context.Request.Method)
        };

        // Extract tenant from route or header if available
        if (context.Request.RouteValues.TryGetValue("tenantId", out var tenantId))
        {
            entry.TenantId = tenantId?.ToString() ?? string.Empty;
        }

        // Extract resource type from path
        var resourceType = ExtractResourceType(context.Request.Path);
        if (resourceType != null)
        {
            entry.ResourceType = resourceType;
        }

        // Extract resource ID from path if present
        var resourceId = ExtractResourceId(context.Request.Path);
        if (resourceId != null)
        {
            entry.ResourceId = resourceId;
        }

        // Extract correlation ID from header
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            entry.CorrelationId = correlationId.ToString();
        }

        // Extract actor from claims
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            entry.ActorId = context.User.FindFirst("sub")?.Value 
                         ?? context.User.FindFirst("client_id")?.Value
                         ?? context.User.FindFirst("oid")?.Value
                         ?? string.Empty;
            entry.ActorType = context.User.HasClaim("client_id", entry.ActorId ?? "") 
                            ? ActorType.ServicePrincipal : ActorType.User;
        }

        Exception? caughtException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Complete the audit entry
            entry.HttpStatus = context.Response.StatusCode;
            entry.ResponseTimeMs = (long)stopwatch.Elapsed.TotalMilliseconds;

            if (caughtException != null)
            {
                entry.ErrorMessage = caughtException.Message;
                entry.ErrorCode = "INTERNAL_ERROR";
            }
            else if (context.Response.StatusCode >= 400)
            {
                entry.ErrorCode = $"HTTP_{context.Response.StatusCode}";
            }

            // Log the audit entry asynchronously (fire and forget for performance)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditLogger.LogAsync(entry);
                }
                catch
                {
                    // Swallow audit logging failures to not impact request processing
                }
            });
        }
    }

    /// <summary>
    /// Maps an HTTP method to an audit operation type.
    /// </summary>
    private static OperationType MapHttpMethodToOperation(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "POST" => OperationType.Create,
            "GET" => OperationType.Read,
            "PUT" => OperationType.Update,
            "PATCH" => OperationType.Patch,
            "DELETE" => OperationType.Delete,
            _ => OperationType.Read
        };
    }

    /// <summary>
    /// Extracts the resource type from the request path.
    /// </summary>
    private static string? ExtractResourceType(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments == null || segments.Length == 0)
            return null;

        // Look for SCIM resource types in the path
        foreach (var segment in segments)
        {
            var lower = segment.ToLowerInvariant();
            if (lower is "users" or "groups" or "schemas" or "resourcetypes" or "serviceproviderconfig" or "bulk")
            {
                return segment;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the resource ID from the request path.
    /// </summary>
    private static string? ExtractResourceId(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments == null || segments.Length < 2)
            return null;

        // Find the segment after a resource type
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var lower = segments[i].ToLowerInvariant();
            if (lower is "users" or "groups")
            {
                // Next segment is likely the ID
                var nextSegment = segments[i + 1];
                // Skip if it's another known endpoint
                if (nextSegment.ToLowerInvariant() is not ".search")
                {
                    return nextSegment;
                }
            }
        }

        return null;
    }
}
