// ==========================================================================
// T022: AuditLogEntry Model
// ==========================================================================
// Model for audit log entries per FR-011 requirements
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models;

/// <summary>
/// Audit log entry for tracking CRUD operations.
/// Per FR-011: timestamp, tenantId, actorId, operationType, resourceType,
/// resourceId, httpStatus, responseTimeMs, and additional context.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the operation occurred (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation (partition key).
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Actor identifier (from oid claim in token).
    /// </summary>
    [JsonPropertyName("actorId")]
    [JsonProperty("actorId")]
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Type of actor.
    /// </summary>
    [JsonPropertyName("actorType")]
    [JsonProperty("actorType")]
    public ActorType ActorType { get; set; } = ActorType.User;

    /// <summary>
    /// Type of operation performed.
    /// </summary>
    [JsonPropertyName("operationType")]
    [JsonProperty("operationType")]
    public OperationType OperationType { get; set; }

    /// <summary>
    /// Alias for OperationType for backward compatibility.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public OperationType Operation { get => OperationType; set => OperationType = value; }

    /// <summary>
    /// Type of resource (User, Group, etc.).
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the affected user (for user operations).
    /// May be redacted for PII protection.
    /// </summary>
    [JsonPropertyName("userName")]
    [JsonProperty("userName")]
    public string? UserName { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    [JsonPropertyName("httpStatus")]
    [JsonProperty("httpStatus")]
    public int HttpStatus { get; set; }

    /// <summary>
    /// Alias for HttpStatus.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int StatusCode { get => HttpStatus; set => HttpStatus = value; }

    /// <summary>
    /// Alias for HttpStatus.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int HttpStatusCode { get => HttpStatus; set => HttpStatus = value; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTimeMs")]
    [JsonProperty("responseTimeMs")]
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Alias for ResponseTimeMs.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public long Duration { get => ResponseTimeMs; set => ResponseTimeMs = value; }

    /// <summary>
    /// Alias for ResponseTimeMs.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public long DurationMs { get => ResponseTimeMs; set => ResponseTimeMs = value; }

    /// <summary>
    /// Unique request ID for correlation.
    /// </summary>
    [JsonPropertyName("requestId")]
    [JsonProperty("requestId")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    [JsonProperty("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Adapter ID for provider operations.
    /// </summary>
    [JsonPropertyName("adapterId")]
    [JsonProperty("adapterId")]
    public string? AdapterId { get; set; }

    /// <summary>
    /// Sync direction for sync operations.
    /// </summary>
    [JsonPropertyName("syncDirection")]
    [JsonProperty("syncDirection")]
    public string? SyncDirection { get; set; }

    /// <summary>
    /// Previous value before the operation (for updates/deletes).
    /// Serialized JSON with PII redacted.
    /// </summary>
    [JsonPropertyName("oldValue")]
    [JsonProperty("oldValue")]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after the operation (for creates/updates).
    /// Serialized JSON with PII redacted.
    /// </summary>
    [JsonPropertyName("newValue")]
    [JsonProperty("newValue")]
    public string? NewValue { get; set; }

    /// <summary>
    /// Error code if the operation failed.
    /// </summary>
    [JsonPropertyName("errorCode")]
    [JsonProperty("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// SCIM error type if applicable.
    /// </summary>
    [JsonPropertyName("scimErrorType")]
    [JsonProperty("scimErrorType")]
    public string? ScimErrorType { get; set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    [JsonPropertyName("clientIpAddress")]
    [JsonProperty("clientIpAddress")]
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Alias for ClientIpAddress.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? IpAddress { get => ClientIpAddress; set => ClientIpAddress = value; }

    /// <summary>
    /// Alias for ClientIpAddress.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? ClientIp { get => ClientIpAddress; set => ClientIpAddress = value; }

    /// <summary>
    /// User agent of the client.
    /// </summary>
    [JsonPropertyName("userAgent")]
    [JsonProperty("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP method of the request.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    [JsonProperty("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path.
    /// </summary>
    [JsonPropertyName("requestPath")]
    [JsonProperty("requestPath")]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// TTL in seconds for automatic expiration.
    /// Default is 90 days per FR-016a.
    /// </summary>
    [JsonPropertyName("ttl")]
    [JsonProperty("ttl")]
    public int? Ttl { get; set; } = 7776000; // 90 days in seconds
}

/// <summary>
/// Type of actor performing the operation.
/// </summary>
public enum ActorType
{
    /// <summary>
    /// Human user.
    /// </summary>
    User,

    /// <summary>
    /// Service principal or application.
    /// </summary>
    ServicePrincipal,

    /// <summary>
    /// System (automated operation).
    /// </summary>
    System,

    /// <summary>
    /// Managed identity.
    /// </summary>
    ManagedIdentity
}

/// <summary>
/// Type of operation performed.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Create operation.
    /// </summary>
    Create,

    /// <summary>
    /// Read/Get operation.
    /// </summary>
    Read,

    /// <summary>
    /// Update operation (full replacement).
    /// </summary>
    Update,

    /// <summary>
    /// Patch operation (partial update).
    /// </summary>
    Patch,

    /// <summary>
    /// Delete operation.
    /// </summary>
    Delete,

    /// <summary>
    /// List/Query operation.
    /// </summary>
    List,

    /// <summary>
    /// Add member to group.
    /// </summary>
    AddMember,

    /// <summary>
    /// Remove member from group.
    /// </summary>
    RemoveMember,

    /// <summary>
    /// Sync operation.
    /// </summary>
    Sync,

    /// <summary>
    /// Transformation operation.
    /// </summary>
    Transform,

    /// <summary>
    /// Authentication attempt.
    /// </summary>
    Authenticate,

    /// <summary>
    /// Authorization check.
    /// </summary>
    Authorize,

    /// <summary>
    /// Configuration change.
    /// </summary>
    Configure,

    /// <summary>
    /// Health check.
    /// </summary>
    HealthCheck
}
