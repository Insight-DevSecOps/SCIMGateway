// ==========================================================================
// T040-T045: GroupsController - SCIM Group Endpoints
// ==========================================================================
// Implements SCIM 2.0 Group endpoints per RFC 7644
// POST /scim/v2/Groups - Create group
// GET /scim/v2/Groups/{id} - Get group by ID
// GET /scim/v2/Groups - List groups with filtering/pagination
// PATCH /scim/v2/Groups/{id} - Partial update group (member add/remove)
// PUT /scim/v2/Groups/{id} - Full update group
// DELETE /scim/v2/Groups/{id} - Delete group
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Repositories;
using SCIMGateway.Core.Validation;
using System.Net;

namespace SCIMGateway.Api.Controllers;

/// <summary>
/// SCIM 2.0 Groups endpoint controller.
/// </summary>
[ApiController]
[Route("scim/v2/[controller]")]
[Produces("application/scim+json", "application/json")]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;
    private readonly ISchemaValidator _schemaValidator;
    private readonly IAuditLogger _auditLogger;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(
        IGroupRepository groupRepository,
        ISchemaValidator schemaValidator,
        IAuditLogger auditLogger,
        IErrorHandler errorHandler,
        ILogger<GroupsController> logger)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new group (POST /scim/v2/Groups).
    /// </summary>
    /// <param name="group">The group to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created group with 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ScimGroup), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup([FromBody] ScimGroup group, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Creating group {DisplayName} for tenant {TenantId}", group.DisplayName, tenantId);

            // Validate the group
            var validationResult = await _schemaValidator.ValidateGroupAsync(group);
            if (!validationResult.IsValid)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Check for duplicate displayName
            if (await _groupRepository.DisplayNameExistsAsync(group.DisplayName, tenantId, cancellationToken: cancellationToken))
            {
                return Conflict(_errorHandler.CreateScimError(
                    HttpStatusCode.Conflict,
                    ScimErrorType.Uniqueness,
                    $"A group with displayName '{group.DisplayName}' already exists"));
            }

            // Create the group
            var createdGroup = await _groupRepository.CreateAsync(group, tenantId, cancellationToken);

            // Set the location header
            var location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{createdGroup.Id}";
            if (createdGroup.Meta != null)
            {
                createdGroup.Meta.Location = location;
            }

            // Set member $ref values
            SetMemberRefs(createdGroup);

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Create,
                ResourceType = "Group",
                ResourceId = createdGroup.Id,
                TenantId = tenantId,
                HttpStatus = 201
            });

            _logger.LogInformation("Created group {GroupId} for tenant {TenantId}", createdGroup.Id, tenantId);

            return Created(location, createdGroup);
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group for tenant {TenantId}", tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Get a group by ID (GET /scim/v2/Groups/{id}).
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group or 404 if not found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ScimGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogDebug("Getting group {GroupId} for tenant {TenantId}", id, tenantId);

            var group = await _groupRepository.GetByIdAsync(id, tenantId, cancellationToken);

            if (group == null)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"Group with id '{id}' not found"));
            }

            // Set the location
            if (group.Meta != null)
            {
                group.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{group.Id}";
            }

            // Set member $ref values
            SetMemberRefs(group);

            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group {GroupId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// List groups with optional filtering and pagination (GET /scim/v2/Groups).
    /// </summary>
    /// <param name="filter">Optional SCIM filter expression.</param>
    /// <param name="startIndex">1-based start index (default: 1).</param>
    /// <param name="count">Maximum number of results (default: 100).</param>
    /// <param name="sortBy">Attribute to sort by.</param>
    /// <param name="sortOrder">Sort order (ascending/descending).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SCIM list response with groups.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ScimListResponse<ScimGroup>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListGroups(
        [FromQuery] string? filter = null,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogDebug("Listing groups for tenant {TenantId} with filter: {Filter}", tenantId, filter);

            // Validate pagination parameters
            if (startIndex < 1)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    "startIndex must be >= 1"));
            }

            if (count < 0)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    "count must be >= 0"));
            }

            // Limit count to prevent excessive results
            if (count > 1000)
            {
                count = 1000;
            }

            var (groups, totalCount) = await _groupRepository.ListAsync(
                tenantId,
                filter,
                startIndex,
                count,
                sortBy,
                sortOrder,
                cancellationToken);

            // Set locations and member refs for each group
            var baseUrl = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups";
            foreach (var group in groups)
            {
                if (group.Meta != null)
                {
                    group.Meta.Location = $"{baseUrl}/{group.Id}";
                }
                SetMemberRefs(group);
            }

            var response = new ScimListResponse<ScimGroup>
            {
                TotalResults = totalCount,
                ItemsPerPage = groups.Count(),
                StartIndex = startIndex,
                Resources = groups.ToList()
            };

            return Ok(response);
        }
        catch (ScimInvalidFilterException ex)
        {
            return BadRequest(_errorHandler.CreateScimError(
                HttpStatusCode.BadRequest,
                ScimErrorType.InvalidFilter,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing groups for tenant {TenantId}", tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Partially update a group (PATCH /scim/v2/Groups/{id}).
    /// Commonly used for adding/removing members.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="patchRequest">The patch operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ScimGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchGroup(
        string id,
        [FromBody] ScimPatchRequest patchRequest,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Patching group {GroupId} for tenant {TenantId}", id, tenantId);

            // Validate patch request
            if (patchRequest?.Operations == null || !patchRequest.Operations.Any())
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidSyntax,
                    "Patch request must include at least one operation"));
            }

            // Validate patch schema
            if (!patchRequest.Schemas.Contains(ScimConstants.Schemas.PatchOp))
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidSyntax,
                    $"Patch request must include schema '{ScimConstants.Schemas.PatchOp}'"));
            }

            // Apply patch operations
            var patchedGroup = await _groupRepository.PatchAsync(id, tenantId, patchRequest.Operations, cancellationToken);

            // Validate the patched group
            var validationResult = await _schemaValidator.ValidateGroupAsync(patchedGroup);
            if (!validationResult.IsValid)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Set the location
            if (patchedGroup.Meta != null)
            {
                patchedGroup.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{patchedGroup.Id}";
            }

            // Set member $ref values
            SetMemberRefs(patchedGroup);

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Patch,
                ResourceType = "Group",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 200
            });

            _logger.LogInformation("Patched group {GroupId} for tenant {TenantId}", id, tenantId);

            return Ok(patchedGroup);
        }
        catch (ScimNotFoundException)
        {
            return NotFound(_errorHandler.CreateScimError(
                HttpStatusCode.NotFound,
                null,
                $"Group with id '{id}' not found"));
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching group {GroupId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Fully replace a group (PUT /scim/v2/Groups/{id}).
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="group">The complete group data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ScimGroup), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceGroup(
        string id,
        [FromBody] ScimGroup group,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Replacing group {GroupId} for tenant {TenantId}", id, tenantId);

            // Ensure ID matches
            if (!string.IsNullOrEmpty(group.Id) && group.Id != id)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    "Group id in body must match id in URL"));
            }

            group.Id = id;

            // Validate the group
            var validationResult = await _schemaValidator.ValidateGroupAsync(group);
            if (!validationResult.IsValid)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Check if group exists
            var existingGroup = await _groupRepository.GetByIdAsync(id, tenantId, cancellationToken);
            if (existingGroup == null)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"Group with id '{id}' not found"));
            }

            // Check for duplicate displayName (if changed)
            if (group.DisplayName != existingGroup.DisplayName &&
                await _groupRepository.DisplayNameExistsAsync(group.DisplayName, tenantId, id, cancellationToken))
            {
                return Conflict(_errorHandler.CreateScimError(
                    HttpStatusCode.Conflict,
                    ScimErrorType.Uniqueness,
                    $"A group with displayName '{group.DisplayName}' already exists"));
            }

            // Update the group
            var updatedGroup = await _groupRepository.UpdateAsync(group, tenantId, cancellationToken);

            // Set the location
            if (updatedGroup.Meta != null)
            {
                updatedGroup.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{updatedGroup.Id}";
            }

            // Set member $ref values
            SetMemberRefs(updatedGroup);

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Update,
                ResourceType = "Group",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 200
            });

            _logger.LogInformation("Replaced group {GroupId} for tenant {TenantId}", id, tenantId);

            return Ok(updatedGroup);
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing group {GroupId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Delete a group (DELETE /scim/v2/Groups/{id}).
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Deleting group {GroupId} for tenant {TenantId}", id, tenantId);

            var deleted = await _groupRepository.DeleteAsync(id, tenantId, cancellationToken);

            if (!deleted)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"Group with id '{id}' not found"));
            }

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Delete,
                ResourceType = "Group",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 204
            });

            _logger.LogInformation("Deleted group {GroupId} for tenant {TenantId}", id, tenantId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    #region Private Methods

    /// <summary>
    /// Gets the tenant ID from the request context.
    /// </summary>
    private string GetTenantId()
    {
        // Try to get from HttpContext items (set by tenant middleware)
        if (HttpContext.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is string tenantId)
        {
            return tenantId;
        }

        // Try to get from TenantContext
        if (HttpContext.Items.TryGetValue("TenantContext", out var tenantContextObj) &&
            tenantContextObj is Core.Authentication.TenantContext tenantContext)
        {
            return tenantContext.TenantId;
        }

        // Try to get from header
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) && !string.IsNullOrEmpty(headerValue))
        {
            return headerValue.ToString();
        }

        // Default tenant for development
        return "default";
    }

    /// <summary>
    /// Sets the $ref property for each member.
    /// </summary>
    private void SetMemberRefs(ScimGroup group)
    {
        if (group.Members == null) return;

        var baseUserUrl = $"{Request.Scheme}://{Request.Host}/scim/v2/Users";
        var baseGroupUrl = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups";

        foreach (var member in group.Members)
        {
            if (string.IsNullOrEmpty(member.Ref) && !string.IsNullOrEmpty(member.Value))
            {
                // Default to User type if not specified
                var type = member.Type?.ToLowerInvariant() ?? "user";
                member.Ref = type == "group"
                    ? $"{baseGroupUrl}/{member.Value}"
                    : $"{baseUserUrl}/{member.Value}";
            }
        }
    }

    /// <summary>
    /// Handles SCIM exceptions and returns appropriate error responses.
    /// </summary>
    private IActionResult HandleScimException(ScimException ex)
    {
        var statusCode = (int)ex.StatusCode;
        var scimError = _errorHandler.CreateScimError(ex.StatusCode, ex.ScimType, ex.Message);

        return StatusCode(statusCode, scimError);
    }

    #endregion
}
