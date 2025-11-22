# OAuth 2.0 Token Validation Flow

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22  
**RFC Reference**: RFC 6749 (OAuth 2.0), RFC 7519 (JWT)

---

## 1. Overview

This document defines the OAuth 2.0 token validation architecture for the SCIM Gateway. All API endpoints require Bearer token authentication with Azure Entra ID (formerly Azure AD) as the identity provider.

**Identity Provider**: Azure Entra ID  
**Token Type**: JWT (JSON Web Token)  
**Authentication Scheme**: Bearer  
**Token Lifetime**: 1 hour (default Entra ID setting)

---

## 2. Token Acquisition Flow

### 2.1 Service Principal Authentication (Entra ID → SCIM Gateway)

**Scenario**: Entra ID provisioning service calls SCIM Gateway API

**Flow**:
```
┌─────────────┐                          ┌──────────────────┐
│ Entra ID    │                          │ SCIM Gateway     │
│ Provisioning│                          │ API              │
└──────┬──────┘                          └────────┬─────────┘
       │                                           │
       │ 1. Request access token                   │
       ├──────────────────────────────────────────▶│
       │    POST https://login.microsoftonline.   │
       │         com/{tenant-id}/oauth2/v2.0/token│
       │    Body:                                  │
       │      client_id={app-id}                   │
       │      client_secret={secret}               │
       │      scope={api-scope}                    │
       │      grant_type=client_credentials        │
       │                                           │
       │◀──────────────────────────────────────────┤
       │ 2. Return access_token (JWT)              │
       │                                           │
       │ 3. Call SCIM API with Bearer token        │
       ├──────────────────────────────────────────▶│
       │    POST /scim/v2/Users                    │
       │    Authorization: Bearer {access_token}   │
       │                                           │
       │◀──────────────────────────────────────────┤
       │ 4. Validate token + process request       │
       │                                           │
```

### 2.2 Token Request

**Endpoint**: `POST https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token`

**Request Body** (application/x-www-form-urlencoded):
```
client_id=a1b2c3d4-e5f6-7890-abcd-ef1234567890
client_secret=your-client-secret
scope=api://scim-gateway-api/.default
grant_type=client_credentials
```

**Success Response** (200 OK):
```json
{
  "token_type": "Bearer",
  "expires_in": 3599,
  "ext_expires_in": 3599,
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6..."
}
```

---

## 3. Token Structure

### 3.1 JWT Token Format

**Structure**: `<header>.<payload>.<signature>`

**Example Decoded Token**:

**Header**:
```json
{
  "typ": "JWT",
  "alg": "RS256",
  "x5t": "key-thumbprint",
  "kid": "key-id"
}
```

**Payload** (Claims):
```json
{
  "aud": "api://scim-gateway-api",
  "iss": "https://sts.windows.net/tenant-id-123/",
  "iat": 1700000000,
  "nbf": 1700000000,
  "exp": 1700003600,
  "aio": "...",
  "appid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "appidacr": "1",
  "idp": "https://sts.windows.net/tenant-id-123/",
  "oid": "service-principal-object-id",
  "rh": "...",
  "sub": "service-principal-object-id",
  "tid": "tenant-id-123",
  "uti": "...",
  "ver": "1.0",
  "roles": ["SCIM.Provisioning"]
}
```

### 3.2 Critical Claims

| Claim | Description | Validation Rule |
|-------|-------------|-----------------|
| `aud` | Audience (API identifier) | Must equal `api://scim-gateway-api` |
| `iss` | Issuer (Entra ID) | Must match `https://sts.windows.net/{tid}/` |
| `exp` | Expiration time (Unix timestamp) | Must be > current time |
| `nbf` | Not before time (Unix timestamp) | Must be <= current time |
| `tid` | Tenant ID | Used for tenant isolation |
| `oid` | Object ID (service principal) | Used for actor identification |
| `appid` | Application ID (client ID) | Identifies calling application |
| `roles` | Application roles | Must contain `SCIM.Provisioning` |

---

## 4. Token Validation Process

### 4.1 Validation Steps (Sequential)

**Step 1: Extract Token from Header**
```csharp
// C# Example
var authHeader = Request.Headers["Authorization"];
if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
{
    return Unauthorized("Missing or invalid Authorization header");
}

var token = authHeader.Substring("Bearer ".Length).Trim();
```

**Step 2: Decode Token (Without Validation)**
```csharp
// Decode to extract kid (key ID) for public key lookup
var handler = new JwtSecurityTokenHandler();
var jwtToken = handler.ReadJwtToken(token);
var kid = jwtToken.Header.Kid;
```

**Step 3: Fetch Entra ID Public Keys**
```csharp
// Cache public keys (refresh every 24 hours)
var jwksUrl = "https://login.microsoftonline.com/common/discovery/v2.0/keys";
var httpClient = new HttpClient();
var jwks = await httpClient.GetFromJsonAsync<JsonWebKeySet>(jwksUrl);
var signingKey = jwks.Keys.FirstOrDefault(k => k.Kid == kid);

if (signingKey == null)
{
    return Unauthorized("Token signing key not found");
}
```

**Step 4: Validate Token Signature**
```csharp
var validationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new JsonWebKey(signingKey),
    ValidateIssuer = true,
    ValidIssuer = $"https://sts.windows.net/{expectedTenantId}/",
    ValidateAudience = true,
    ValidAudience = "api://scim-gateway-api",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5)
};

var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
```

**Step 5: Extract and Validate Claims**
```csharp
var tid = principal.FindFirst("tid")?.Value;
var oid = principal.FindFirst("oid")?.Value;
var appid = principal.FindFirst("appid")?.Value;
var roles = principal.FindAll("roles").Select(c => c.Value).ToList();

if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(oid))
{
    return Unauthorized("Required claims missing");
}

if (!roles.Contains("SCIM.Provisioning"))
{
    return Forbidden("Insufficient permissions");
}
```

**Step 6: Enforce Tenant Isolation**
```csharp
// Store tenantId in request context for all downstream operations
HttpContext.Items["TenantId"] = tid;
HttpContext.Items["ActorId"] = oid;
```

### 4.2 Validation Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ Token Validation Middleware                                 │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
           ┌────────────────────────┐
           │ Extract Bearer Token   │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Decode JWT (no verify) │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Fetch Public Keys      │
           │ (cache 24h)            │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Validate Signature     │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Validate Claims        │
           │ - aud, iss, exp, nbf   │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Check Roles            │
           │ - SCIM.Provisioning    │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Extract tid & oid      │
           │ Store in context       │
           └────────┬───────────────┘
                    │
                    ▼
           ┌────────────────────────┐
           │ Allow Request          │
           └────────────────────────┘
```

---

## 5. Caching Strategy

### 5.1 Public Key Caching

**Why Cache?**
- Entra ID public keys change infrequently (weeks/months)
- Fetching keys on every request adds latency
- Reduces external dependency failures

**Cache Strategy**:
```csharp
public class JwksCache
{
    private static JsonWebKeySet _cachedKeys;
    private static DateTime _cacheExpiry;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public static async Task<JsonWebKeySet> GetKeysAsync()
    {
        if (_cachedKeys != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedKeys;
        }

        var httpClient = new HttpClient();
        var jwksUrl = "https://login.microsoftonline.com/common/discovery/v2.0/keys";
        _cachedKeys = await httpClient.GetFromJsonAsync<JsonWebKeySet>(jwksUrl);
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

        return _cachedKeys;
    }
}
```

### 5.2 Token Caching (Client-Side)

**Client Responsibility**: Entra ID clients should cache access tokens until `expires_in` (typically 1 hour).

**Anti-Pattern**: Requesting new token for every API call (causes rate limiting).

---

## 6. Error Responses

### 6.1 Authentication Errors

**Missing Token** (401 Unauthorized):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "401",
  "detail": "Authorization header missing or invalid"
}
```

**Invalid Token Signature** (401 Unauthorized):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "401",
  "detail": "Token signature validation failed"
}
```

**Expired Token** (401 Unauthorized):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "401",
  "detail": "Token has expired. Please request a new access token."
}
```

**Invalid Audience** (401 Unauthorized):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "401",
  "detail": "Token audience does not match API identifier"
}
```

### 6.2 Authorization Errors

**Missing Role** (403 Forbidden):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "403",
  "detail": "Insufficient permissions. Required role: SCIM.Provisioning"
}
```

**Cross-Tenant Access** (403 Forbidden):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "403",
  "detail": "Access denied. Resource belongs to different tenant."
}
```

---

## 7. Tenant Isolation Enforcement

### 7.1 Tenant ID Extraction

**From Token**:
```csharp
var tid = principal.FindFirst("tid")?.Value;
HttpContext.Items["TenantId"] = tid;
```

**In Data Access Layer**:
```csharp
public async Task<User> GetUserAsync(string userId)
{
    var tenantId = _httpContextAccessor.HttpContext.Items["TenantId"] as string;
    
    // All Cosmos DB queries MUST include tenantId filter
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.id = @userId AND c.tenantId = @tenantId"
    )
    .WithParameter("@userId", userId)
    .WithParameter("@tenantId", tenantId);
    
    // ...
}
```

### 7.2 Cross-Tenant Protection

**Scenario**: Token from Tenant-A tries to access Tenant-B resource

**Validation**:
```csharp
public async Task<IActionResult> GetUser(string userId)
{
    var requestTenantId = HttpContext.Items["TenantId"] as string;
    var user = await _userService.GetUserAsync(userId);
    
    if (user == null)
    {
        return NotFound();
    }
    
    // Double-check tenant isolation (defense in depth)
    if (user.TenantId != requestTenantId)
    {
        _logger.LogWarning(
            "Cross-tenant access attempt: Token tid={TokenTid}, Resource tid={ResourceTid}",
            requestTenantId,
            user.TenantId
        );
        return Forbidden("Cross-tenant access denied");
    }
    
    return Ok(user);
}
```

---

## 8. Role-Based Access Control (RBAC)

### 8.1 Application Roles

**Role**: `SCIM.Provisioning`

**Description**: Grants full CRUD access to SCIM User and Group resources.

**Permissions**:
- Create, Read, Update, Delete Users
- Create, Read, Update, Delete Groups
- Add/Remove Group Members
- Query Users and Groups

### 8.2 Future Roles (Extensibility)

| Role | Permissions | Use Case |
|------|-------------|----------|
| `SCIM.ReadOnly` | Read Users/Groups | Reporting/audit |
| `SCIM.Admin` | All + config management | Gateway administration |
| `SCIM.Drift.Reconcile` | Read + reconcile drift | Drift remediation |

### 8.3 Role Validation

```csharp
[Authorize(Roles = "SCIM.Provisioning")]
public class UsersController : ControllerBase
{
    // All actions require SCIM.Provisioning role
}
```

---

## 9. Token Refresh Strategy

### 9.1 Client-Side Refresh (Entra ID Responsibility)

**Token Lifetime**: 1 hour (3600 seconds)

**Refresh Strategy**:
1. Client caches token and `expires_in` timestamp
2. Before expiration (e.g., 5 minutes before), request new token
3. Use new token for subsequent requests

**Example** (pseudo-code):
```csharp
public class TokenManager
{
    private string _cachedToken;
    private DateTime _tokenExpiry;

    public async Task<string> GetTokenAsync()
    {
        if (DateTime.UtcNow.AddMinutes(5) < _tokenExpiry)
        {
            return _cachedToken; // Token still valid
        }

        // Request new token
        var tokenResponse = await RequestTokenAsync();
        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        
        return _cachedToken;
    }
}
```

### 9.2 Gateway-Side Handling

**Gateway does NOT refresh tokens** (stateless design).

**Client responsibility**: Request new token when 401 received.

---

## 10. Security Best Practices

### 10.1 HTTPS Only

**All API endpoints MUST use HTTPS** (TLS 1.2+).

**Azure Functions/App Service**:
- Enable "HTTPS Only" setting
- Reject HTTP requests (301 redirect or 400 Bad Request)

### 10.2 Token Validation Checklist

- ✅ Validate token signature with Entra ID public keys
- ✅ Validate `aud` claim matches API identifier
- ✅ Validate `iss` claim matches Entra ID tenant
- ✅ Validate `exp` (expiration) > current time
- ✅ Validate `nbf` (not before) <= current time
- ✅ Validate `roles` claim contains required role
- ✅ Extract `tid` for tenant isolation
- ✅ Extract `oid` for actor identification

### 10.3 Defense in Depth

**Layer 1**: Token validation (signature, claims)  
**Layer 2**: Role-based authorization  
**Layer 3**: Tenant isolation (query filters)  
**Layer 4**: Resource ownership check (tenantId match)  
**Layer 5**: Audit logging (all access attempts)

---

## 11. Configuration

### 11.1 Azure Functions Configuration

**App Settings** (Azure Portal or ARM template):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "api://scim-gateway-api",
    "Audience": "api://scim-gateway-api"
  }
}
```

### 11.2 Middleware Registration

**Startup.cs** (Azure Functions .NET Isolated):
```csharp
public override void Configure(IFunctionsWorkerApplicationBuilder builder)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://login.microsoftonline.com/common/v2.0";
            options.Audience = "api://scim-gateway-api";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });

    builder.Services.AddAuthorization();
    
    builder.UseMiddleware<AuthenticationMiddleware>();
}
```

---

## 12. Testing

### 12.1 Unit Tests

**Test 1: Valid Token**
```csharp
[Fact]
public async Task ValidateToken_ValidToken_ReturnsSuccess()
{
    // Arrange
    var token = GenerateMockToken(
        tid: "tenant-123",
        oid: "service-principal-456",
        roles: new[] { "SCIM.Provisioning" }
    );

    // Act
    var result = await _tokenValidator.ValidateAsync(token);

    // Assert
    Assert.True(result.IsValid);
    Assert.Equal("tenant-123", result.TenantId);
}
```

**Test 2: Expired Token**
```csharp
[Fact]
public async Task ValidateToken_ExpiredToken_ReturnsUnauthorized()
{
    // Arrange
    var token = GenerateMockToken(exp: DateTime.UtcNow.AddHours(-1));

    // Act
    var result = await _tokenValidator.ValidateAsync(token);

    // Assert
    Assert.False(result.IsValid);
    Assert.Equal("Token has expired", result.ErrorMessage);
}
```

### 12.2 Integration Tests

**Test: End-to-End Token Validation**
```csharp
[Fact]
public async Task GetUser_WithValidToken_Returns200()
{
    // Arrange
    var token = await AcquireRealTokenAsync(); // Use Entra ID test tenant
    _httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _httpClient.GetAsync("/scim/v2/Users/user-123");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## 13. Monitoring & Alerts

### 13.1 Key Metrics

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| Token validation failures | > 10/min | Investigate token config |
| 401 errors | > 5% of requests | Check client token refresh |
| 403 errors | > 1% of requests | Investigate role assignments |
| Public key fetch failures | > 0 | Check Entra ID connectivity |

### 13.2 Application Insights Queries

**Failed Token Validations**:
```kql
requests
| where timestamp > ago(1h)
| where resultCode == "401"
| summarize count() by bin(timestamp, 5m), cloud_RoleName
| render timechart
```

**Cross-Tenant Access Attempts**:
```kql
traces
| where timestamp > ago(24h)
| where message contains "Cross-tenant access attempt"
| project timestamp, tenantId, userId, requestId
```

---

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
