// ==========================================================================
// T025: ResponseFormatter - SCIM Response Formatting per RFC 7644
// ==========================================================================
// Formats SCIM responses including ListResponse and resource locations
// ==========================================================================

using Newtonsoft.Json;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Validation;

namespace SCIMGateway.Core.Formatting;

/// <summary>
/// Interface for SCIM response formatting.
/// </summary>
public interface IResponseFormatter
{
    /// <summary>
    /// Formats a single resource response.
    /// </summary>
    string FormatResource<T>(T resource, string baseUri) where T : class;

    /// <summary>
    /// Formats a ListResponse for multiple resources.
    /// </summary>
    string FormatListResponse<T>(IEnumerable<T> resources, int totalResults, int startIndex, int itemsPerPage, string baseUri) where T : class;

    /// <summary>
    /// Formats a 201 Created response.
    /// </summary>
    string FormatCreatedResponse<T>(T resource, string baseUri) where T : class;

    /// <summary>
    /// Generates a resource location URI.
    /// </summary>
    string GenerateLocationUri(string baseUri, string resourceType, string resourceId);

    /// <summary>
    /// Sets meta attributes on a resource.
    /// </summary>
    void SetMetaAttributes<T>(T resource, string baseUri) where T : class;
}

/// <summary>
/// SCIM ListResponse model per RFC 7644.
/// </summary>
public class ScimListResponse<T>
{
    /// <summary>
    /// SCIM schema URIs.
    /// </summary>
    [JsonProperty("schemas")]
    public List<string> Schemas { get; set; } = [ScimSchemaUris.ListResponse];

    /// <summary>
    /// Total number of matching resources.
    /// </summary>
    [JsonProperty("totalResults")]
    public int TotalResults { get; set; }

    /// <summary>
    /// 1-based index of first resource in page.
    /// </summary>
    [JsonProperty("startIndex")]
    public int StartIndex { get; set; } = 1;

    /// <summary>
    /// Number of resources in current page.
    /// </summary>
    [JsonProperty("itemsPerPage")]
    public int ItemsPerPage { get; set; }

    /// <summary>
    /// Array of resources.
    /// </summary>
    [JsonProperty("Resources")]
    public List<T> Resources { get; set; } = [];
}

/// <summary>
/// Non-generic SCIM ListResponse for dynamic scenarios.
/// </summary>
public class ScimListResponse
{
    /// <summary>
    /// SCIM schema URIs.
    /// </summary>
    [JsonProperty("schemas")]
    public List<string> Schemas { get; set; } = [ScimSchemaUris.ListResponse];

    /// <summary>
    /// Total number of matching resources.
    /// </summary>
    [JsonProperty("totalResults")]
    public int TotalResults { get; set; }

    /// <summary>
    /// 1-based index of first resource in page.
    /// </summary>
    [JsonProperty("startIndex")]
    public int StartIndex { get; set; } = 1;

    /// <summary>
    /// Number of resources in current page.
    /// </summary>
    [JsonProperty("itemsPerPage")]
    public int ItemsPerPage { get; set; }

    /// <summary>
    /// Array of resources.
    /// </summary>
    [JsonProperty("Resources")]
    public List<object> Resources { get; set; } = [];
}

/// <summary>
/// SCIM response formatter implementation.
/// </summary>
public class ResponseFormatter : IResponseFormatter
{
    private readonly JsonSerializerSettings _jsonSettings;

    /// <summary>
    /// Initializes a new instance of ResponseFormatter.
    /// </summary>
    public ResponseFormatter()
    {
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Newtonsoft.Json.Formatting.Indented
        };
    }

    /// <inheritdoc />
    public string FormatResource<T>(T resource, string baseUri) where T : class
    {
        SetMetaAttributes(resource, baseUri);
        return JsonConvert.SerializeObject(resource, _jsonSettings);
    }

    /// <inheritdoc />
    public string FormatListResponse<T>(IEnumerable<T> resources, int totalResults, int startIndex, int itemsPerPage, string baseUri) where T : class
    {
        var resourceList = resources.ToList();
        
        // Set meta attributes on each resource
        foreach (var resource in resourceList)
        {
            SetMetaAttributes(resource, baseUri);
        }

        var listResponse = new ScimListResponse<T>
        {
            TotalResults = totalResults,
            StartIndex = startIndex,
            ItemsPerPage = itemsPerPage,
            Resources = resourceList
        };

        return JsonConvert.SerializeObject(listResponse, _jsonSettings);
    }

    /// <inheritdoc />
    public string FormatCreatedResponse<T>(T resource, string baseUri) where T : class
    {
        SetMetaAttributes(resource, baseUri);
        return JsonConvert.SerializeObject(resource, _jsonSettings);
    }

    /// <inheritdoc />
    public string GenerateLocationUri(string baseUri, string resourceType, string resourceId)
    {
        // Normalize base URI
        var normalizedBaseUri = baseUri.TrimEnd('/');
        
        // Resource type should be plural
        var pluralResourceType = GetPluralResourceType(resourceType);
        
        return $"{normalizedBaseUri}/{pluralResourceType}/{resourceId}";
    }

    /// <inheritdoc />
    public void SetMetaAttributes<T>(T resource, string baseUri) where T : class
    {
        // Use reflection to set meta attributes
        var metaProperty = typeof(T).GetProperty("Meta");
        if (metaProperty == null) return;

        var resourceType = GetResourceType(resource);
        var resourceId = GetResourceId(resource);

        if (string.IsNullOrEmpty(resourceId)) return;

        var meta = metaProperty.GetValue(resource) as ScimMeta;
        if (meta == null)
        {
            meta = new ScimMeta();
            metaProperty.SetValue(resource, meta);
        }

        meta.ResourceType = resourceType;
        meta.Location = GenerateLocationUri(baseUri, resourceType, resourceId);
        
        // Set version (ETag) if not already set
        if (string.IsNullOrEmpty(meta.Version))
        {
            meta.Version = GenerateETag(resource);
        }
    }

    private static string GetResourceType<T>(T resource)
    {
        return resource switch
        {
            ScimUser => "User",
            ScimGroup => "Group",
            _ => typeof(T).Name.Replace("Scim", "")
        };
    }

    private static string? GetResourceId<T>(T resource)
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(resource)?.ToString();
    }

    private static string GetPluralResourceType(string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "user" => "Users",
            "group" => "Groups",
            "serviceproviderconfig" => "ServiceProviderConfig",
            "resourcetype" => "ResourceTypes",
            "schema" => "Schemas",
            _ => resourceType + "s"
        };
    }

    private static string GenerateETag<T>(T resource)
    {
        // Generate ETag from resource hash
        var json = JsonConvert.SerializeObject(resource);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return $"W/\"{Convert.ToBase64String(hash)[..16]}\"";
    }
}

/// <summary>
/// SCIM meta attribute model.
/// </summary>
public class ScimMeta
{
    /// <summary>
    /// Resource type (User, Group).
    /// </summary>
    [JsonProperty("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// Resource location URI.
    /// </summary>
    [JsonProperty("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Resource version (ETag).
    /// </summary>
    [JsonProperty("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    [JsonProperty("created")]
    public DateTimeOffset? Created { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [JsonProperty("lastModified")]
    public DateTimeOffset? LastModified { get; set; }
}
