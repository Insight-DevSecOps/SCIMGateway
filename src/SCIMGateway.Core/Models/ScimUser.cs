// ==========================================================================
// T018: ScimUser Model - RFC 7643 Compliant User Schema
// ==========================================================================
// SCIM 2.0 User resource model per RFC 7643
// Including: core attributes, multi-valued attributes, meta, enterprise extension
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models;

/// <summary>
/// SCIM 2.0 User resource per RFC 7643.
/// </summary>
public class ScimUser
{
    /// <summary>
    /// SCIM schemas included in this resource.
    /// </summary>
    [JsonPropertyName("schemas")]
    [JsonProperty("schemas")]
    public List<string> Schemas { get; set; } =
    [
        ScimConstants.Schemas.User
    ];

    /// <summary>
    /// Unique identifier for the resource (assigned by service provider).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier from the provisioning client.
    /// </summary>
    [JsonPropertyName("externalId")]
    [JsonProperty("externalId")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unique identifier for the user (required).
    /// </summary>
    [JsonPropertyName("userName")]
    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Name to display for the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's name components.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public ScimName? Name { get; set; }

    /// <summary>
    /// Casual name of the user.
    /// </summary>
    [JsonPropertyName("nickName")]
    [JsonProperty("nickName")]
    public string? NickName { get; set; }

    /// <summary>
    /// URL of the user's profile page.
    /// </summary>
    [JsonPropertyName("profileUrl")]
    [JsonProperty("profileUrl")]
    public string? ProfileUrl { get; set; }

    /// <summary>
    /// User's title (e.g., "Vice President").
    /// </summary>
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Type of user (e.g., "Employee", "Contractor").
    /// </summary>
    [JsonPropertyName("userType")]
    [JsonProperty("userType")]
    public string? UserType { get; set; }

    /// <summary>
    /// User's preferred language (BCP 47 format).
    /// </summary>
    [JsonPropertyName("preferredLanguage")]
    [JsonProperty("preferredLanguage")]
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// User's locale (BCP 47 format).
    /// </summary>
    [JsonPropertyName("locale")]
    [JsonProperty("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// User's timezone (IANA Time Zone format).
    /// </summary>
    [JsonPropertyName("timezone")]
    [JsonProperty("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    [JsonPropertyName("active")]
    [JsonProperty("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    /// User's password (write-only, never returned).
    /// </summary>
    [JsonPropertyName("password")]
    [JsonProperty("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Email addresses for the user.
    /// </summary>
    [JsonPropertyName("emails")]
    [JsonProperty("emails")]
    public List<ScimEmail>? Emails { get; set; }

    /// <summary>
    /// Phone numbers for the user.
    /// </summary>
    [JsonPropertyName("phoneNumbers")]
    [JsonProperty("phoneNumbers")]
    public List<ScimPhoneNumber>? PhoneNumbers { get; set; }

    /// <summary>
    /// Instant messaging addresses.
    /// </summary>
    [JsonPropertyName("ims")]
    [JsonProperty("ims")]
    public List<ScimIm>? Ims { get; set; }

    /// <summary>
    /// URLs of photos for the user.
    /// </summary>
    [JsonPropertyName("photos")]
    [JsonProperty("photos")]
    public List<ScimPhoto>? Photos { get; set; }

    /// <summary>
    /// Physical mailing addresses.
    /// </summary>
    [JsonPropertyName("addresses")]
    [JsonProperty("addresses")]
    public List<ScimAddress>? Addresses { get; set; }

    /// <summary>
    /// Groups the user belongs to (read-only).
    /// </summary>
    [JsonPropertyName("groups")]
    [JsonProperty("groups")]
    public List<ScimUserGroup>? Groups { get; set; }

    /// <summary>
    /// Entitlements the user has.
    /// </summary>
    [JsonPropertyName("entitlements")]
    [JsonProperty("entitlements")]
    public List<ScimEntitlement>? Entitlements { get; set; }

    /// <summary>
    /// Roles the user has.
    /// </summary>
    [JsonPropertyName("roles")]
    [JsonProperty("roles")]
    public List<ScimRole>? Roles { get; set; }

    /// <summary>
    /// X.509 certificates for the user.
    /// </summary>
    [JsonPropertyName("x509Certificates")]
    [JsonProperty("x509Certificates")]
    public List<ScimX509Certificate>? X509Certificates { get; set; }

    /// <summary>
    /// Resource metadata.
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonProperty("meta")]
    public ScimMeta? Meta { get; set; }

    /// <summary>
    /// Enterprise user extension data.
    /// </summary>
    [JsonPropertyName("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
    [JsonProperty("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
    public EnterpriseUserExtension? EnterpriseUser { get; set; }

    // ===== Internal SDK Attributes (not part of SCIM spec) =====

    /// <summary>
    /// Internal: Tenant identifier for multi-tenant isolation.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? TenantId { get; set; }

    /// <summary>
    /// Internal: Adapter identifier that manages this user.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? AdapterId { get; set; }

    /// <summary>
    /// Internal: Synchronization state.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? SyncState { get; set; }
}

/// <summary>
/// SCIM Name complex type per RFC 7643.
/// </summary>
public class ScimName
{
    /// <summary>
    /// Full name formatted for display.
    /// </summary>
    [JsonPropertyName("formatted")]
    [JsonProperty("formatted")]
    public string? Formatted { get; set; }

    /// <summary>
    /// Family name (last name).
    /// </summary>
    [JsonPropertyName("familyName")]
    [JsonProperty("familyName")]
    public string? FamilyName { get; set; }

    /// <summary>
    /// Given name (first name).
    /// </summary>
    [JsonPropertyName("givenName")]
    [JsonProperty("givenName")]
    public string? GivenName { get; set; }

    /// <summary>
    /// Middle name(s).
    /// </summary>
    [JsonPropertyName("middleName")]
    [JsonProperty("middleName")]
    public string? MiddleName { get; set; }

    /// <summary>
    /// Honorific prefix (e.g., "Dr.", "Ms.").
    /// </summary>
    [JsonPropertyName("honorificPrefix")]
    [JsonProperty("honorificPrefix")]
    public string? HonorificPrefix { get; set; }

    /// <summary>
    /// Honorific suffix (e.g., "Jr.", "III").
    /// </summary>
    [JsonPropertyName("honorificSuffix")]
    [JsonProperty("honorificSuffix")]
    public string? HonorificSuffix { get; set; }
}

/// <summary>
/// SCIM Email multi-valued attribute per RFC 7643.
/// </summary>
public class ScimEmail
{
    /// <summary>
    /// Email address value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the email.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of email (e.g., "work", "home", "other").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary email.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Phone Number multi-valued attribute per RFC 7643.
/// </summary>
public class ScimPhoneNumber
{
    /// <summary>
    /// Phone number value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the phone number.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of phone (e.g., "work", "home", "mobile", "fax", "pager").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary phone number.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Instant Messaging multi-valued attribute per RFC 7643.
/// </summary>
public class ScimIm
{
    /// <summary>
    /// IM address value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the IM address.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of IM (e.g., "aim", "gtalk", "icq", "xmpp", "msn", "skype", "qq", "yahoo").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary IM.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Photo multi-valued attribute per RFC 7643.
/// </summary>
public class ScimPhoto
{
    /// <summary>
    /// URL of the photo.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the photo.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of photo (e.g., "photo", "thumbnail").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary photo.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Address multi-valued attribute per RFC 7643.
/// </summary>
public class ScimAddress
{
    /// <summary>
    /// Full address formatted for display.
    /// </summary>
    [JsonPropertyName("formatted")]
    [JsonProperty("formatted")]
    public string? Formatted { get; set; }

    /// <summary>
    /// Street address component.
    /// </summary>
    [JsonPropertyName("streetAddress")]
    [JsonProperty("streetAddress")]
    public string? StreetAddress { get; set; }

    /// <summary>
    /// City or locality component.
    /// </summary>
    [JsonPropertyName("locality")]
    [JsonProperty("locality")]
    public string? Locality { get; set; }

    /// <summary>
    /// State or region component.
    /// </summary>
    [JsonPropertyName("region")]
    [JsonProperty("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Postal code component.
    /// </summary>
    [JsonPropertyName("postalCode")]
    [JsonProperty("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country component (ISO 3166-1).
    /// </summary>
    [JsonPropertyName("country")]
    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Type of address (e.g., "work", "home", "other").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary address.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM User Group reference (read-only).
/// </summary>
public class ScimUserGroup
{
    /// <summary>
    /// Group ID.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// URI reference to the group.
    /// </summary>
    [JsonPropertyName("$ref")]
    [JsonProperty("$ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Display name of the group.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of group membership ("direct" or "indirect").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }
}

/// <summary>
/// SCIM Entitlement multi-valued attribute.
/// </summary>
public class ScimEntitlement
{
    /// <summary>
    /// Entitlement value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the entitlement.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of entitlement.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary entitlement.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Role multi-valued attribute.
/// </summary>
public class ScimRole
{
    /// <summary>
    /// Role value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the role.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of role.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary role.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM X.509 Certificate multi-valued attribute.
/// </summary>
public class ScimX509Certificate
{
    /// <summary>
    /// DER-encoded X.509 certificate (base64).
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Display name for the certificate.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Type of certificate.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the primary certificate.
    /// </summary>
    [JsonPropertyName("primary")]
    [JsonProperty("primary")]
    public bool Primary { get; set; }
}

/// <summary>
/// SCIM Meta complex type per RFC 7643.
/// </summary>
public class ScimMeta
{
    /// <summary>
    /// Type of resource (e.g., "User", "Group").
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// DateTime the resource was created.
    /// </summary>
    [JsonPropertyName("created")]
    [JsonProperty("created")]
    public DateTime? Created { get; set; }

    /// <summary>
    /// DateTime the resource was last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    [JsonProperty("lastModified")]
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// URI of the resource.
    /// </summary>
    [JsonPropertyName("location")]
    [JsonProperty("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Version of the resource (ETag).
    /// </summary>
    [JsonPropertyName("version")]
    [JsonProperty("version")]
    public string? Version { get; set; }
}

/// <summary>
/// Enterprise User Extension per RFC 7643 Section 4.3.
/// Schema: urn:ietf:params:scim:schemas:extension:enterprise:2.0:User
/// </summary>
public class EnterpriseUserExtension
{
    /// <summary>
    /// Employee number.
    /// </summary>
    [JsonPropertyName("employeeNumber")]
    [JsonProperty("employeeNumber")]
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// Cost center.
    /// </summary>
    [JsonPropertyName("costCenter")]
    [JsonProperty("costCenter")]
    public string? CostCenter { get; set; }

    /// <summary>
    /// Organization name.
    /// </summary>
    [JsonPropertyName("organization")]
    [JsonProperty("organization")]
    public string? Organization { get; set; }

    /// <summary>
    /// Division name.
    /// </summary>
    [JsonPropertyName("division")]
    [JsonProperty("division")]
    public string? Division { get; set; }

    /// <summary>
    /// Department name.
    /// </summary>
    [JsonPropertyName("department")]
    [JsonProperty("department")]
    public string? Department { get; set; }

    /// <summary>
    /// Manager reference.
    /// </summary>
    [JsonPropertyName("manager")]
    [JsonProperty("manager")]
    public ScimManager? Manager { get; set; }
}

/// <summary>
/// SCIM Manager reference for Enterprise User Extension.
/// </summary>
public class ScimManager
{
    /// <summary>
    /// Manager's user ID.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// URI reference to the manager.
    /// </summary>
    [JsonPropertyName("$ref")]
    [JsonProperty("$ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Display name of the manager.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// SCIM schema constants.
/// </summary>
public static class ScimConstants
{
    /// <summary>
    /// SCIM schema URNs.
    /// </summary>
    public static class Schemas
    {
        public const string User = "urn:ietf:params:scim:schemas:core:2.0:User";
        public const string Group = "urn:ietf:params:scim:schemas:core:2.0:Group";
        public const string EnterpriseUser = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";
        public const string Error = "urn:ietf:params:scim:api:messages:2.0:Error";
        public const string ListResponse = "urn:ietf:params:scim:api:messages:2.0:ListResponse";
        public const string PatchOp = "urn:ietf:params:scim:api:messages:2.0:PatchOp";
        public const string BulkRequest = "urn:ietf:params:scim:api:messages:2.0:BulkRequest";
        public const string BulkResponse = "urn:ietf:params:scim:api:messages:2.0:BulkResponse";
        public const string SearchRequest = "urn:ietf:params:scim:api:messages:2.0:SearchRequest";
    }

    /// <summary>
    /// Resource type names.
    /// </summary>
    public static class ResourceTypes
    {
        public const string User = "User";
        public const string Group = "Group";
    }
}
