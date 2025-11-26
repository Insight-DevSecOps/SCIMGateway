// ==========================================================================
// T034-T038: UsersController - SCIM User Endpoints
// ==========================================================================
// Implements SCIM 2.0 User endpoints per RFC 7644
// POST /scim/v2/Users - Create user
// GET /scim/v2/Users/{id} - Get user by ID
// GET /scim/v2/Users - List users with filtering/pagination
// PATCH /scim/v2/Users/{id} - Partial update user
// PUT /scim/v2/Users/{id} - Full update user
// DELETE /scim/v2/Users/{id} - Delete user
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Authentication;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Repositories;
using SCIMGateway.Core.Validation;
using System.Net;

namespace SCIMGateway.Api.Controllers;

/// <summary>
/// SCIM 2.0 Users endpoint controller.
/// </summary>
[ApiController]
[Route("scim/v2/[controller]")]
[Produces("application/scim+json", "application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ISchemaValidator _schemaValidator;
    private readonly IAuditLogger _auditLogger;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        ISchemaValidator schemaValidator,
        IAuditLogger auditLogger,
        IErrorHandler errorHandler,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new user (POST /scim/v2/Users).
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user with 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ScimUser), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] ScimUser user, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        
        try
        {
            _logger.LogInformation("Creating user {UserName} for tenant {TenantId}", user.UserName, tenantId);

            // Validate the user
            var validationResult = await _schemaValidator.ValidateUserAsync(user);
            if (!validationResult.IsValid)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Check for duplicate userName
            if (await _userRepository.UserNameExistsAsync(user.UserName, tenantId, cancellationToken: cancellationToken))
            {
                return Conflict(_errorHandler.CreateScimError(
                    HttpStatusCode.Conflict,
                    ScimErrorType.Uniqueness,
                    $"A user with userName '{user.UserName}' already exists"));
            }

            // Create the user
            var createdUser = await _userRepository.CreateAsync(user, tenantId, cancellationToken);

            // Set the location header
            var location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{createdUser.Id}";
            if (createdUser.Meta != null)
            {
                createdUser.Meta.Location = location;
            }

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Create,
                ResourceType = "User",
                ResourceId = createdUser.Id,
                TenantId = tenantId,
                HttpStatus = 201
            });

            _logger.LogInformation("Created user {UserId} for tenant {TenantId}", createdUser.Id, tenantId);

            return Created(location, createdUser);
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user for tenant {TenantId}", tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Get a user by ID (GET /scim/v2/Users/{id}).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user or 404 if not found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ScimUser), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogDebug("Getting user {UserId} for tenant {TenantId}", id, tenantId);

            var user = await _userRepository.GetByIdAsync(id, tenantId, cancellationToken);

            if (user == null)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"User with id '{id}' not found"));
            }

            // Set the location
            if (user.Meta != null)
            {
                user.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{user.Id}";
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// List users with optional filtering and pagination (GET /scim/v2/Users).
    /// </summary>
    /// <param name="filter">Optional SCIM filter expression.</param>
    /// <param name="startIndex">1-based start index (default: 1).</param>
    /// <param name="count">Maximum number of results (default: 100).</param>
    /// <param name="sortBy">Attribute to sort by.</param>
    /// <param name="sortOrder">Sort order (ascending/descending).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SCIM list response with users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ScimListResponse<ScimUser>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListUsers(
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
            _logger.LogDebug("Listing users for tenant {TenantId} with filter: {Filter}", tenantId, filter);

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

            var (users, totalCount) = await _userRepository.ListAsync(
                tenantId,
                filter,
                startIndex,
                count,
                sortBy,
                sortOrder,
                cancellationToken);

            // Set locations for each user
            var baseUrl = $"{Request.Scheme}://{Request.Host}/scim/v2/Users";
            foreach (var user in users)
            {
                if (user.Meta != null)
                {
                    user.Meta.Location = $"{baseUrl}/{user.Id}";
                }
            }

            var response = new ScimListResponse<ScimUser>
            {
                TotalResults = totalCount,
                ItemsPerPage = users.Count(),
                StartIndex = startIndex,
                Resources = users.ToList()
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
            _logger.LogError(ex, "Error listing users for tenant {TenantId}", tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Partially update a user (PATCH /scim/v2/Users/{id}).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="patchRequest">The patch operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ScimUser), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchUser(
        string id,
        [FromBody] ScimPatchRequest patchRequest,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Patching user {UserId} for tenant {TenantId}", id, tenantId);

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
            var patchedUser = await _userRepository.PatchAsync(id, tenantId, patchRequest.Operations, cancellationToken);

            // Validate the patched user
            var validationResult = await _schemaValidator.ValidateUserAsync(patchedUser);
            if (!validationResult.IsValid)
            {
                // This shouldn't normally happen, but if patch makes user invalid
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Set the location
            if (patchedUser.Meta != null)
            {
                patchedUser.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{patchedUser.Id}";
            }

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Patch,
                ResourceType = "User",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 200
            });

            _logger.LogInformation("Patched user {UserId} for tenant {TenantId}", id, tenantId);

            return Ok(patchedUser);
        }
        catch (ScimNotFoundException)
        {
            return NotFound(_errorHandler.CreateScimError(
                HttpStatusCode.NotFound,
                null,
                $"User with id '{id}' not found"));
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching user {UserId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Fully replace a user (PUT /scim/v2/Users/{id}).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="user">The complete user data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ScimUser), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceUser(
        string id,
        [FromBody] ScimUser user,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Replacing user {UserId} for tenant {TenantId}", id, tenantId);

            // Ensure ID matches
            if (!string.IsNullOrEmpty(user.Id) && user.Id != id)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    "User id in body must match id in URL"));
            }

            user.Id = id;

            // Validate the user
            var validationResult = await _schemaValidator.ValidateUserAsync(user);
            if (!validationResult.IsValid)
            {
                return BadRequest(_errorHandler.CreateScimError(
                    HttpStatusCode.BadRequest,
                    ScimErrorType.InvalidValue,
                    string.Join("; ", validationResult.Errors.Select(e => e.Message))));
            }

            // Check if user exists
            var existingUser = await _userRepository.GetByIdAsync(id, tenantId, cancellationToken);
            if (existingUser == null)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"User with id '{id}' not found"));
            }

            // Check for duplicate userName (if changed)
            if (user.UserName != existingUser.UserName &&
                await _userRepository.UserNameExistsAsync(user.UserName, tenantId, id, cancellationToken))
            {
                return Conflict(_errorHandler.CreateScimError(
                    HttpStatusCode.Conflict,
                    ScimErrorType.Uniqueness,
                    $"A user with userName '{user.UserName}' already exists"));
            }

            // Update the user
            var updatedUser = await _userRepository.UpdateAsync(user, tenantId, cancellationToken);

            // Set the location
            if (updatedUser.Meta != null)
            {
                updatedUser.Meta.Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{updatedUser.Id}";
            }

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Update,
                ResourceType = "User",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 200
            });

            _logger.LogInformation("Replaced user {UserId} for tenant {TenantId}", id, tenantId);

            return Ok(updatedUser);
        }
        catch (ScimException ex)
        {
            return HandleScimException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing user {UserId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, _errorHandler.HandleException(ex));
        }
    }

    /// <summary>
    /// Delete a user (DELETE /scim/v2/Users/{id}).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ScimError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        try
        {
            _logger.LogInformation("Deleting user {UserId} for tenant {TenantId}", id, tenantId);

            var deleted = await _userRepository.DeleteAsync(id, tenantId, cancellationToken);

            if (!deleted)
            {
                return NotFound(_errorHandler.CreateScimError(
                    HttpStatusCode.NotFound,
                    null,
                    $"User with id '{id}' not found"));
            }

            // Audit log
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                OperationType = OperationType.Delete,
                ResourceType = "User",
                ResourceId = id,
                TenantId = tenantId,
                HttpStatus = 204
            });

            _logger.LogInformation("Deleted user {UserId} for tenant {TenantId}", id, tenantId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} for tenant {TenantId}", id, tenantId);
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

        // Try to get from header
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) && !string.IsNullOrEmpty(headerValue))
        {
            return headerValue.ToString();
        }

        // Default tenant for development
        return "default";
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

/// <summary>
/// SCIM list response per RFC 7644.
/// </summary>
/// <typeparam name="T">Resource type.</typeparam>
public class ScimListResponse<T>
{
    /// <summary>
    /// SCIM schemas for list response.
    /// </summary>
    public List<string> Schemas { get; set; } = [ScimConstants.Schemas.ListResponse];

    /// <summary>
    /// Total number of results matching the query.
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Number of items in this page.
    /// </summary>
    public int ItemsPerPage { get; set; }

    /// <summary>
    /// 1-based start index of this page.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Resources in this page.
    /// </summary>
    public List<T> Resources { get; set; } = [];
}
