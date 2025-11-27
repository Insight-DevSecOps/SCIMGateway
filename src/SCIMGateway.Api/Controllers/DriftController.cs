// ==========================================================================
// T132-T133: Drift and Conflict API Endpoints
// ==========================================================================
// T132: GET /api/drift - query drift reports
// T133: GET /api/conflicts - query conflict reports
// Also includes POST /api/drift/{driftId}/reconcile for manual reconciliation
// ==========================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Authentication;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.SyncEngine;

namespace SCIMGateway.Api.Controllers;

/// <summary>
/// Query parameters for drift reports.
/// </summary>
public class DriftQueryParameters
{
    /// <summary>
    /// Provider identifier filter.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Filter by reconciled status.
    /// </summary>
    public bool? Reconciled { get; set; }

    /// <summary>
    /// Filter by drift type.
    /// </summary>
    public DriftType? DriftType { get; set; }

    /// <summary>
    /// Filter by resource type (User or Group).
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Filter by minimum severity.
    /// </summary>
    public DriftSeverity? MinSeverity { get; set; }

    /// <summary>
    /// Start date for filtering.
    /// </summary>
    public DateTime? Since { get; set; }

    /// <summary>
    /// End date for filtering.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field.
    /// </summary>
    public string? SortBy { get; set; } = "timestamp";

    /// <summary>
    /// Sort order (asc or desc).
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Query parameters for conflict reports.
/// </summary>
public class ConflictQueryParameters
{
    /// <summary>
    /// Provider identifier filter.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Filter by resolved status.
    /// </summary>
    public bool? Resolved { get; set; }

    /// <summary>
    /// Filter by conflict type.
    /// </summary>
    public ConflictType? ConflictType { get; set; }

    /// <summary>
    /// Filter by resource type (User or Group).
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Filter by minimum severity.
    /// </summary>
    public ConflictSeverity? MinSeverity { get; set; }

    /// <summary>
    /// Filter by sync blocked status.
    /// </summary>
    public bool? SyncBlocked { get; set; }

    /// <summary>
    /// Start date for filtering.
    /// </summary>
    public DateTime? Since { get; set; }

    /// <summary>
    /// End date for filtering.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field.
    /// </summary>
    public string? SortBy { get; set; } = "timestamp";

    /// <summary>
    /// Sort order (asc or desc).
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Request body for manual drift reconciliation.
/// </summary>
public class ReconcileDriftRequest
{
    /// <summary>
    /// Direction to apply for reconciliation.
    /// </summary>
    public SyncDirection Direction { get; set; }

    /// <summary>
    /// Notes about the reconciliation decision.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to apply the change immediately.
    /// </summary>
    public bool ApplyImmediately { get; set; } = true;
}

/// <summary>
/// Request body for manual conflict resolution.
/// </summary>
public class ResolveConflictRequest
{
    /// <summary>
    /// Resolution strategy to apply.
    /// </summary>
    public ConflictResolution Resolution { get; set; }

    /// <summary>
    /// Notes about the resolution decision.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Paged response for drift reports.
/// </summary>
public class DriftReportsResponse
{
    /// <summary>
    /// Total count of matching reports.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Drift reports for this page.
    /// </summary>
    public List<DriftReport> Reports { get; set; } = new();

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public DriftSummary? Summary { get; set; }
}

/// <summary>
/// Summary statistics for drift reports.
/// </summary>
public class DriftSummary
{
    /// <summary>
    /// Total drift reports.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Reconciled drift reports.
    /// </summary>
    public int Reconciled { get; set; }

    /// <summary>
    /// Pending drift reports.
    /// </summary>
    public int Pending { get; set; }

    /// <summary>
    /// Count by drift type.
    /// </summary>
    public Dictionary<string, int> ByType { get; set; } = new();

    /// <summary>
    /// Count by severity.
    /// </summary>
    public Dictionary<string, int> BySeverity { get; set; } = new();

    /// <summary>
    /// Count by resource type.
    /// </summary>
    public Dictionary<string, int> ByResourceType { get; set; } = new();
}

/// <summary>
/// Paged response for conflict reports.
/// </summary>
public class ConflictReportsResponse
{
    /// <summary>
    /// Total count of matching reports.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Conflict reports for this page.
    /// </summary>
    public List<ConflictReport> Reports { get; set; } = new();

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public ConflictSummary? Summary { get; set; }
}

/// <summary>
/// Summary statistics for conflict reports.
/// </summary>
public class ConflictSummary
{
    /// <summary>
    /// Total conflict reports.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Resolved conflict reports.
    /// </summary>
    public int Resolved { get; set; }

    /// <summary>
    /// Pending conflict reports.
    /// </summary>
    public int Pending { get; set; }

    /// <summary>
    /// Conflicts with sync blocked.
    /// </summary>
    public int SyncBlocked { get; set; }

    /// <summary>
    /// Count by conflict type.
    /// </summary>
    public Dictionary<string, int> ByType { get; set; } = new();

    /// <summary>
    /// Count by severity.
    /// </summary>
    public Dictionary<string, int> BySeverity { get; set; } = new();
}

/// <summary>
/// Controller for drift and conflict reporting APIs.
/// T132: GET /api/drift - query drift reports
/// T133: GET /api/conflicts - query conflict reports
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class DriftController : ControllerBase
{
    private readonly ILogger<DriftController> _logger;
    private readonly IReconciler _reconciler;
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly ITenantResolver _tenantResolver;

    // In-memory storage for drift/conflict reports (in production, use persistent storage)
    private static readonly Dictionary<string, List<DriftReport>> _driftReports = new();
    private static readonly Dictionary<string, List<ConflictReport>> _conflictReports = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DriftController"/> class.
    /// </summary>
    public DriftController(
        ILogger<DriftController> logger,
        IReconciler reconciler,
        ISyncStateRepository syncStateRepository,
        ITenantResolver tenantResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
        _syncStateRepository = syncStateRepository ?? throw new ArgumentNullException(nameof(syncStateRepository));
        _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
    }

    /// <summary>
    /// T132: Gets drift reports for the current tenant.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>Paged list of drift reports.</returns>
    [HttpGet("drift")]
    [ProducesResponseType(typeof(DriftReportsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DriftReportsResponse>> GetDriftReports([FromQuery] DriftQueryParameters query)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        _logger.LogInformation("Getting drift reports for tenant {TenantId}", tenantId);

        // Get pending drift reports from reconciler
        var pendingReports = await _reconciler.GetPendingDriftReportsAsync(tenantId, query.ProviderId);

        // Also get from in-memory storage (in production, query from database)
        List<DriftReport> allReports;
        lock (_lock)
        {
            var key = GetTenantKey(tenantId);
            allReports = _driftReports.GetValueOrDefault(key, new List<DriftReport>()).ToList();
        }

        // Merge pending reports
        var reportIds = allReports.Select(r => r.Id).ToHashSet();
        foreach (var pending in pendingReports)
        {
            if (!reportIds.Contains(pending.Id))
            {
                allReports.Add(pending);
            }
        }

        // Apply filters
        var filtered = allReports.AsQueryable();

        if (!string.IsNullOrEmpty(query.ProviderId))
        {
            filtered = filtered.Where(r => r.ProviderId == query.ProviderId);
        }

        if (query.Reconciled.HasValue)
        {
            filtered = filtered.Where(r => r.Reconciled == query.Reconciled.Value);
        }

        if (query.DriftType.HasValue)
        {
            filtered = filtered.Where(r => r.DriftType == query.DriftType.Value);
        }

        if (!string.IsNullOrEmpty(query.ResourceType))
        {
            filtered = filtered.Where(r => r.ResourceType.Equals(query.ResourceType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinSeverity.HasValue)
        {
            filtered = filtered.Where(r => r.Severity >= query.MinSeverity.Value);
        }

        if (query.Since.HasValue)
        {
            filtered = filtered.Where(r => r.Timestamp >= query.Since.Value);
        }

        if (query.Until.HasValue)
        {
            filtered = filtered.Where(r => r.Timestamp <= query.Until.Value);
        }

        var filteredList = filtered.ToList();

        // Apply sorting
        filteredList = query.SortBy?.ToLowerInvariant() switch
        {
            "timestamp" => query.SortOrder == "asc" 
                ? filteredList.OrderBy(r => r.Timestamp).ToList()
                : filteredList.OrderByDescending(r => r.Timestamp).ToList(),
            "severity" => query.SortOrder == "asc"
                ? filteredList.OrderBy(r => r.Severity).ToList()
                : filteredList.OrderByDescending(r => r.Severity).ToList(),
            "resourcetype" => query.SortOrder == "asc"
                ? filteredList.OrderBy(r => r.ResourceType).ToList()
                : filteredList.OrderByDescending(r => r.ResourceType).ToList(),
            _ => filteredList.OrderByDescending(r => r.Timestamp).ToList()
        };

        // Calculate summary
        var summary = new DriftSummary
        {
            Total = filteredList.Count,
            Reconciled = filteredList.Count(r => r.Reconciled),
            Pending = filteredList.Count(r => !r.Reconciled),
            ByType = filteredList.GroupBy(r => r.DriftType.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = filteredList.GroupBy(r => r.Severity.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByResourceType = filteredList.GroupBy(r => r.ResourceType).ToDictionary(g => g.Key, g => g.Count())
        };

        // Apply pagination
        var pagedReports = filteredList
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var response = new DriftReportsResponse
        {
            TotalCount = filteredList.Count,
            Page = query.Page,
            PageSize = query.PageSize,
            Reports = pagedReports,
            Summary = summary
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific drift report by ID.
    /// </summary>
    /// <param name="driftId">The drift report ID.</param>
    /// <returns>The drift report.</returns>
    [HttpGet("drift/{driftId}")]
    [ProducesResponseType(typeof(DriftReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DriftReport>> GetDriftReport(string driftId)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        // Get from reconciler pending reports
        var pendingReports = await _reconciler.GetPendingDriftReportsAsync(tenantId);
        var report = pendingReports.FirstOrDefault(r => r.DriftId == driftId);

        if (report == null)
        {
            // Check in-memory storage
            lock (_lock)
            {
                var key = GetTenantKey(tenantId);
                var reports = _driftReports.GetValueOrDefault(key, new List<DriftReport>());
                report = reports.FirstOrDefault(r => r.DriftId == driftId);
            }
        }

        if (report == null)
        {
            return NotFound(new { error = "Drift report not found", driftId });
        }

        return Ok(report);
    }

    /// <summary>
    /// T127: Manually reconciles a drift report.
    /// POST /api/drift/{driftId}/reconcile
    /// </summary>
    /// <param name="driftId">The drift report ID.</param>
    /// <param name="request">Reconciliation request.</param>
    /// <returns>Reconciliation result.</returns>
    [HttpPost("drift/{driftId}/reconcile")]
    [ProducesResponseType(typeof(ReconciliationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReconciliationResult>> ReconcileDrift(
        string driftId,
        [FromBody] ReconcileDriftRequest request)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        var actorId = tenantContext.ActorId;
        
        _logger.LogInformation(
            "Manual reconciliation requested for drift {DriftId} by {ActorId}",
            driftId, actorId);

        // Find the drift report
        var pendingReports = await _reconciler.GetPendingDriftReportsAsync(tenantId);
        var driftReport = pendingReports.FirstOrDefault(r => r.DriftId == driftId);

        if (driftReport == null)
        {
            return NotFound(new { error = "Drift report not found or already reconciled", driftId });
        }

        // Create manual reconciliation request
        var manualRequest = new ManualReconciliationRequest
        {
            DriftId = driftId,
            Direction = request.Direction,
            ActorId = actorId ?? "anonymous",
            Notes = request.Notes,
            ApplyImmediately = request.ApplyImmediately
        };

        // Process reconciliation
        var result = await _reconciler.ProcessManualReconciliationAsync(
            manualRequest,
            tenantId,
            driftReport.ProviderId);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage, driftId });
        }

        return Ok(result);
    }

    /// <summary>
    /// T133: Gets conflict reports for the current tenant.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>Paged list of conflict reports.</returns>
    [HttpGet("conflicts")]
    [ProducesResponseType(typeof(ConflictReportsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConflictReportsResponse>> GetConflictReports([FromQuery] ConflictQueryParameters query)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        _logger.LogInformation("Getting conflict reports for tenant {TenantId}", tenantId);

        // Get pending conflict reports from reconciler
        var pendingReports = await _reconciler.GetPendingConflictReportsAsync(tenantId, query.ProviderId);

        // Also get from in-memory storage (in production, query from database)
        List<ConflictReport> allReports;
        lock (_lock)
        {
            var key = GetTenantKey(tenantId);
            allReports = _conflictReports.GetValueOrDefault(key, new List<ConflictReport>()).ToList();
        }

        // Merge pending reports
        var reportIds = allReports.Select(r => r.Id).ToHashSet();
        foreach (var pending in pendingReports)
        {
            if (!reportIds.Contains(pending.Id))
            {
                allReports.Add(pending);
            }
        }

        // Apply filters
        var filtered = allReports.AsQueryable();

        if (!string.IsNullOrEmpty(query.ProviderId))
        {
            filtered = filtered.Where(r => r.ProviderId == query.ProviderId);
        }

        if (query.Resolved.HasValue)
        {
            filtered = filtered.Where(r => r.Resolved == query.Resolved.Value);
        }

        if (query.ConflictType.HasValue)
        {
            filtered = filtered.Where(r => r.ConflictType == query.ConflictType.Value);
        }

        if (!string.IsNullOrEmpty(query.ResourceType))
        {
            filtered = filtered.Where(r => r.ResourceType.Equals(query.ResourceType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinSeverity.HasValue)
        {
            filtered = filtered.Where(r => r.Severity >= query.MinSeverity.Value);
        }

        if (query.SyncBlocked.HasValue)
        {
            filtered = filtered.Where(r => r.SyncBlocked == query.SyncBlocked.Value);
        }

        if (query.Since.HasValue)
        {
            filtered = filtered.Where(r => r.Timestamp >= query.Since.Value);
        }

        if (query.Until.HasValue)
        {
            filtered = filtered.Where(r => r.Timestamp <= query.Until.Value);
        }

        var filteredList = filtered.ToList();

        // Apply sorting
        filteredList = query.SortBy?.ToLowerInvariant() switch
        {
            "timestamp" => query.SortOrder == "asc"
                ? filteredList.OrderBy(r => r.Timestamp).ToList()
                : filteredList.OrderByDescending(r => r.Timestamp).ToList(),
            "severity" => query.SortOrder == "asc"
                ? filteredList.OrderBy(r => r.Severity).ToList()
                : filteredList.OrderByDescending(r => r.Severity).ToList(),
            "resourcetype" => query.SortOrder == "asc"
                ? filteredList.OrderBy(r => r.ResourceType).ToList()
                : filteredList.OrderByDescending(r => r.ResourceType).ToList(),
            _ => filteredList.OrderByDescending(r => r.Timestamp).ToList()
        };

        // Calculate summary
        var summary = new ConflictSummary
        {
            Total = filteredList.Count,
            Resolved = filteredList.Count(r => r.Resolved),
            Pending = filteredList.Count(r => !r.Resolved),
            SyncBlocked = filteredList.Count(r => r.SyncBlocked),
            ByType = filteredList.GroupBy(r => r.ConflictType.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = filteredList.GroupBy(r => r.Severity.ToString()).ToDictionary(g => g.Key, g => g.Count())
        };

        // Apply pagination
        var pagedReports = filteredList
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var response = new ConflictReportsResponse
        {
            TotalCount = filteredList.Count,
            Page = query.Page,
            PageSize = query.PageSize,
            Reports = pagedReports,
            Summary = summary
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific conflict report by ID.
    /// </summary>
    /// <param name="conflictId">The conflict report ID.</param>
    /// <returns>The conflict report.</returns>
    [HttpGet("conflicts/{conflictId}")]
    [ProducesResponseType(typeof(ConflictReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConflictReport>> GetConflictReport(string conflictId)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        // Get from reconciler pending reports
        var pendingReports = await _reconciler.GetPendingConflictReportsAsync(tenantId);
        var report = pendingReports.FirstOrDefault(r => r.ConflictId == conflictId);

        if (report == null)
        {
            // Check in-memory storage
            lock (_lock)
            {
                var key = GetTenantKey(tenantId);
                var reports = _conflictReports.GetValueOrDefault(key, new List<ConflictReport>());
                report = reports.FirstOrDefault(r => r.ConflictId == conflictId);
            }
        }

        if (report == null)
        {
            return NotFound(new { error = "Conflict report not found", conflictId });
        }

        return Ok(report);
    }

    /// <summary>
    /// Resolves a conflict report.
    /// POST /api/conflicts/{conflictId}/resolve
    /// </summary>
    /// <param name="conflictId">The conflict report ID.</param>
    /// <param name="request">Resolution request.</param>
    /// <returns>Reconciliation result.</returns>
    [HttpPost("conflicts/{conflictId}/resolve")]
    [ProducesResponseType(typeof(ReconciliationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReconciliationResult>> ResolveConflict(
        string conflictId,
        [FromBody] ResolveConflictRequest request)
    {
        var tenantContext = _tenantResolver.GetCurrentTenant();
        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            return Unauthorized("Tenant ID not found in token");
        }

        var tenantId = tenantContext.TenantId;
        var actorId = tenantContext.ActorId;

        _logger.LogInformation(
            "Conflict resolution requested for {ConflictId} by {ActorId}",
            conflictId, actorId);

        // Find the conflict report
        var pendingReports = await _reconciler.GetPendingConflictReportsAsync(tenantId);
        var conflictReport = pendingReports.FirstOrDefault(r => r.ConflictId == conflictId);

        if (conflictReport == null)
        {
            return NotFound(new { error = "Conflict report not found or already resolved", conflictId });
        }

        // Get sync state
        var syncState = await _syncStateRepository.GetAsync(tenantId, conflictReport.ProviderId);
        if (syncState == null)
        {
            return BadRequest(new { error = "Sync state not found for provider", providerId = conflictReport.ProviderId });
        }

        // Process resolution
        var result = await _reconciler.ReconcileConflictAsync(
            conflictReport,
            syncState,
            request.Resolution,
            actorId ?? "anonymous");

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage, conflictId });
        }

        return Ok(result);
    }

    /// <summary>
    /// Adds a drift report to storage (for internal use).
    /// </summary>
    internal static void AddDriftReport(string tenantId, DriftReport report)
    {
        lock (_lock)
        {
            var key = GetTenantKey(tenantId);
            if (!_driftReports.ContainsKey(key))
            {
                _driftReports[key] = new List<DriftReport>();
            }
            _driftReports[key].Add(report);
        }
    }

    /// <summary>
    /// Adds a conflict report to storage (for internal use).
    /// </summary>
    internal static void AddConflictReport(string tenantId, ConflictReport report)
    {
        lock (_lock)
        {
            var key = GetTenantKey(tenantId);
            if (!_conflictReports.ContainsKey(key))
            {
                _conflictReports[key] = new List<ConflictReport>();
            }
            _conflictReports[key].Add(report);
        }
    }

    private static string GetTenantKey(string tenantId) => $"tenant:{tenantId}";
}
