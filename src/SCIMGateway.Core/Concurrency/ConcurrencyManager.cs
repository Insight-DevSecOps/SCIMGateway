// ==========================================================================
// T050: ConcurrencyManager - Optimistic Concurrency Control
// ==========================================================================
// Implements versioning and concurrency control using ETags per RFC 7644
// ==========================================================================

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Concurrency;

/// <summary>
/// Interface for managing resource versioning and concurrency control.
/// </summary>
public interface IConcurrencyManager
{
    /// <summary>
    /// Generates a new version string for a resource.
    /// </summary>
    /// <returns>A new version string (ETag format).</returns>
    string GenerateVersion();

    /// <summary>
    /// Generates a version based on resource content.
    /// </summary>
    /// <param name="resource">The resource to generate version for.</param>
    /// <returns>A content-based version string.</returns>
    string GenerateContentVersion(object resource);

    /// <summary>
    /// Validates that the If-Match header matches the current version.
    /// </summary>
    /// <param name="ifMatchHeader">The If-Match header value.</param>
    /// <param name="currentVersion">The current resource version.</param>
    /// <returns>True if valid, false if version mismatch.</returns>
    bool ValidateVersion(string? ifMatchHeader, string? currentVersion);

    /// <summary>
    /// Validates version with exception on mismatch.
    /// </summary>
    /// <param name="ifMatchHeader">The If-Match header value.</param>
    /// <param name="currentVersion">The current resource version.</param>
    /// <exception cref="VersionMismatchException">Thrown if versions don't match.</exception>
    void ValidateVersionOrThrow(string? ifMatchHeader, string? currentVersion);

    /// <summary>
    /// Increments the version for a resource.
    /// </summary>
    /// <param name="meta">The resource metadata to update.</param>
    /// <returns>The new version string.</returns>
    string IncrementVersion(ScimMeta? meta);

    /// <summary>
    /// Parses the If-Match header value.
    /// </summary>
    /// <param name="ifMatchHeader">The raw header value.</param>
    /// <returns>The parsed ETag value without quotes.</returns>
    string? ParseIfMatchHeader(string? ifMatchHeader);

    /// <summary>
    /// Formats a version string as an ETag.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns>The ETag-formatted string (e.g., W/"version").</returns>
    string FormatAsETag(string version);
}

/// <summary>
/// Implementation of concurrency control for SCIM resources.
/// </summary>
public class ConcurrencyManager : IConcurrencyManager
{
    private readonly ILogger<ConcurrencyManager>? _logger;

    public ConcurrencyManager(ILogger<ConcurrencyManager>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateVersion()
    {
        // Generate a new unique version using GUID
        return $"W/\"{Guid.NewGuid():N}\"";
    }

    /// <inheritdoc />
    public string GenerateContentVersion(object resource)
    {
        try
        {
            // Serialize the resource and compute hash
            var json = System.Text.Json.JsonSerializer.Serialize(resource);
            var hash = ComputeHash(json);
            return $"W/\"{hash}\"";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to generate content-based version, using random version");
            return GenerateVersion();
        }
    }

    /// <inheritdoc />
    public bool ValidateVersion(string? ifMatchHeader, string? currentVersion)
    {
        // If no If-Match header, allow the operation
        if (string.IsNullOrEmpty(ifMatchHeader))
        {
            return true;
        }

        // If If-Match is *, always allow
        if (ifMatchHeader == "*")
        {
            return true;
        }

        // If no current version, reject the operation (resource should have version)
        if (string.IsNullOrEmpty(currentVersion))
        {
            return false;
        }

        // Parse and compare versions
        var requestedVersion = ParseIfMatchHeader(ifMatchHeader);
        var actualVersion = ParseIfMatchHeader(currentVersion);

        return string.Equals(requestedVersion, actualVersion, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void ValidateVersionOrThrow(string? ifMatchHeader, string? currentVersion)
    {
        if (!ValidateVersion(ifMatchHeader, currentVersion))
        {
            var requested = ParseIfMatchHeader(ifMatchHeader) ?? "none";
            var actual = ParseIfMatchHeader(currentVersion) ?? "none";
            
            _logger?.LogWarning(
                "Version mismatch: requested '{RequestedVersion}', actual '{ActualVersion}'",
                requested, actual);

            throw new VersionMismatchException(ifMatchHeader ?? "none", currentVersion ?? "none");
        }
    }

    /// <inheritdoc />
    public string IncrementVersion(ScimMeta? meta)
    {
        var newVersion = GenerateVersion();

        if (meta != null)
        {
            meta.Version = newVersion;
            meta.LastModified = DateTime.UtcNow;
        }

        return newVersion;
    }

    /// <inheritdoc />
    public string? ParseIfMatchHeader(string? ifMatchHeader)
    {
        if (string.IsNullOrEmpty(ifMatchHeader))
        {
            return null;
        }

        // Handle wildcard
        if (ifMatchHeader == "*")
        {
            return "*";
        }

        // Remove W/ prefix if present (weak ETag)
        var value = ifMatchHeader;
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        // Remove surrounding quotes
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
        {
            value = value[1..^1];
        }

        return value;
    }

    /// <inheritdoc />
    public string FormatAsETag(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return GenerateVersion();
        }

        // Already formatted as ETag
        if (version.StartsWith("W/\"") && version.EndsWith('"'))
        {
            return version;
        }

        // Already has quotes
        if (version.StartsWith('"') && version.EndsWith('"'))
        {
            return $"W/{version}";
        }

        // Add weak ETag formatting
        return $"W/\"{version}\"";
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash)[..16]; // Use first 16 chars of hash
    }
}

/// <summary>
/// Extension methods for concurrency control.
/// </summary>
public static class ConcurrencyExtensions
{
    /// <summary>
    /// Initializes version metadata for a new resource.
    /// </summary>
    /// <param name="user">The user to initialize.</param>
    /// <param name="concurrencyManager">The concurrency manager.</param>
    public static void InitializeVersion(this ScimUser user, IConcurrencyManager concurrencyManager)
    {
        user.Meta ??= new ScimMeta { ResourceType = ScimConstants.ResourceTypes.User };
        user.Meta.Created = DateTime.UtcNow;
        user.Meta.LastModified = DateTime.UtcNow;
        user.Meta.Version = concurrencyManager.GenerateVersion();
    }

    /// <summary>
    /// Updates version metadata for a modified resource.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="concurrencyManager">The concurrency manager.</param>
    public static void UpdateVersion(this ScimUser user, IConcurrencyManager concurrencyManager)
    {
        user.Meta ??= new ScimMeta { ResourceType = ScimConstants.ResourceTypes.User };
        user.Meta.LastModified = DateTime.UtcNow;
        user.Meta.Version = concurrencyManager.GenerateVersion();
    }

    /// <summary>
    /// Initializes version metadata for a new group.
    /// </summary>
    /// <param name="group">The group to initialize.</param>
    /// <param name="concurrencyManager">The concurrency manager.</param>
    public static void InitializeVersion(this ScimGroup group, IConcurrencyManager concurrencyManager)
    {
        group.Meta ??= new ScimMeta { ResourceType = ScimConstants.ResourceTypes.Group };
        group.Meta.Created = DateTime.UtcNow;
        group.Meta.LastModified = DateTime.UtcNow;
        group.Meta.Version = concurrencyManager.GenerateVersion();
    }

    /// <summary>
    /// Updates version metadata for a modified group.
    /// </summary>
    /// <param name="group">The group to update.</param>
    /// <param name="concurrencyManager">The concurrency manager.</param>
    public static void UpdateVersion(this ScimGroup group, IConcurrencyManager concurrencyManager)
    {
        group.Meta ??= new ScimMeta { ResourceType = ScimConstants.ResourceTypes.Group };
        group.Meta.LastModified = DateTime.UtcNow;
        group.Meta.Version = concurrencyManager.GenerateVersion();
    }

    /// <summary>
    /// Gets the ETag value from the resource metadata.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The ETag value or null.</returns>
    public static string? GetETag(this ScimUser user)
    {
        return user.Meta?.Version;
    }

    /// <summary>
    /// Gets the ETag value from the resource metadata.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <returns>The ETag value or null.</returns>
    public static string? GetETag(this ScimGroup group)
    {
        return group.Meta?.Version;
    }
}
