// ==========================================================================
// T013: BearerTokenValidator - OAuth 2.0 Token Validation
// ==========================================================================
// Validates Bearer tokens per Microsoft SCIM specification
// ==========================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SCIMGateway.Core.Authentication;

/// <summary>
/// Interface for Bearer token validation.
/// </summary>
public interface IBearerTokenValidator
{
    /// <summary>
    /// Validates a Bearer token and returns the claims principal.
    /// </summary>
    /// <param name="token">The Bearer token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validated claims principal.</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts claims from a validated token.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>Token claims.</returns>
    TokenClaims ExtractClaims(ClaimsPrincipal principal);
}

/// <summary>
/// Token validation error types per RFC 6750.
/// </summary>
public enum TokenValidationError
{
    /// <summary>
    /// No error.
    /// </summary>
    None,

    /// <summary>
    /// Token is missing.
    /// </summary>
    MissingToken,

    /// <summary>
    /// Token is invalid or malformed.
    /// </summary>
    InvalidToken,

    /// <summary>
    /// Token signature is invalid.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// Token has expired.
    /// </summary>
    ExpiredToken,

    /// <summary>
    /// Token audience is invalid.
    /// </summary>
    InvalidAudience,

    /// <summary>
    /// Token issuer is invalid.
    /// </summary>
    InvalidIssuer,

    /// <summary>
    /// Token scope is insufficient.
    /// </summary>
    InsufficientScope
}

/// <summary>
/// Result of token validation.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The validated claims principal.
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Alias for Principal property.
    /// </summary>
    public ClaimsPrincipal? ClaimsPrincipal { get => Principal; set => Principal = value; }

    /// <summary>
    /// Extracted claims.
    /// </summary>
    public TokenClaims? Claims { get; set; }

    /// <summary>
    /// Error type if validation failed.
    /// </summary>
    public TokenValidationError Error { get; set; } = TokenValidationError.None;

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Alias for ErrorMessage.
    /// </summary>
    public string? ErrorDescription { get => ErrorMessage; set => ErrorMessage = value; }

    /// <summary>
    /// Error code if validation failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// When the token expires (for retry-after header).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Extracted claims from a validated token.
/// </summary>
public class TokenClaims
{
    /// <summary>
    /// Tenant ID (tid claim).
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Object ID (oid claim) - the actor ID.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Subject (sub claim).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Application ID (appid or azp claim).
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Audience (aud claim).
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Issuer (iss claim).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Token issued at time.
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Token not valid before time.
    /// </summary>
    public DateTime? NotBefore { get; set; }

    /// <summary>
    /// Scopes (scp or scope claim).
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Roles (roles claim).
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// User principal name (upn claim).
    /// </summary>
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Display name (name claim).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// All claims from the token.
    /// </summary>
    public Dictionary<string, string> AllClaims { get; set; } = [];
}

/// <summary>
/// Options for token validation.
/// </summary>
public class TokenValidationOptions
{
    /// <summary>
    /// Expected audience (your API's application ID or URI).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Valid issuers (typically Azure AD tenant URLs).
    /// </summary>
    public List<string> ValidIssuers { get; set; } = [];

    /// <summary>
    /// Whether to validate the issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the signing key.
    /// </summary>
    public bool ValidateSigningKey { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance for lifetime validation.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Azure AD metadata endpoint for key discovery.
    /// </summary>
    public string MetadataEndpoint { get; set; } = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

    /// <summary>
    /// Required scopes for access.
    /// </summary>
    public List<string> RequiredScopes { get; set; } = [];

    /// <summary>
    /// Valid audiences for token validation (supports multiple).
    /// </summary>
    public List<string> ValidAudiences { get; set; } = [];
}

/// <summary>
/// Alias for TokenValidationOptions for backward compatibility.
/// </summary>
public class BearerTokenValidatorOptions : TokenValidationOptions
{
}

/// <summary>
/// Bearer token validator implementation.
/// </summary>
public class BearerTokenValidator : IBearerTokenValidator
{
    private readonly TokenValidationOptions _options;
    private readonly ILogger<BearerTokenValidator> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private TokenValidationParameters? _validationParameters;
    private DateTime _parametersLastRefresh = DateTime.MinValue;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public BearerTokenValidator(
        IOptions<TokenValidationOptions> options,
        ILogger<BearerTokenValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <inheritdoc />
    public async Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token is required",
                ErrorCode = "invalid_token"
            };
        }

        // Remove "Bearer " prefix if present
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token["Bearer ".Length..];
        }

        try
        {
            var parameters = await GetValidationParametersAsync(cancellationToken);
            
            var principal = _tokenHandler.ValidateToken(token, parameters, out var validatedToken);
            var claims = ExtractClaims(principal);

            // Validate required claims
            if (string.IsNullOrEmpty(claims.TenantId))
            {
                _logger.LogWarning("Token missing tid claim");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token missing required tenant identifier (tid) claim",
                    ErrorCode = "invalid_token"
                };
            }

            if (string.IsNullOrEmpty(claims.ObjectId))
            {
                _logger.LogWarning("Token missing oid claim");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token missing required object identifier (oid) claim",
                    ErrorCode = "invalid_token"
                };
            }

            // Validate required scopes if configured
            if (_options.RequiredScopes.Count > 0)
            {
                var hasRequiredScope = _options.RequiredScopes.Any(s => claims.Scopes.Contains(s));
                if (!hasRequiredScope)
                {
                    _logger.LogWarning("Token missing required scopes");
                    return new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Token missing required scope",
                        ErrorCode = "insufficient_scope"
                    };
                }
            }

            _logger.LogDebug("Token validated successfully for tenant {TenantId}, actor {ObjectId}", 
                claims.TenantId, claims.ObjectId);

            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                Claims = claims,
                ExpiresAt = claims.ExpiresAt
            };
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired",
                ErrorCode = "token_expired",
                ExpiresAt = ex.Expires
            };
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token has invalid signature");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid signature",
                ErrorCode = "invalid_signature"
            };
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Token has invalid audience");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid audience",
                ErrorCode = "invalid_audience"
            };
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token has invalid issuer");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid issuer",
                ErrorCode = "invalid_issuer"
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token",
                ErrorCode = "invalid_token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed",
                ErrorCode = "server_error"
            };
        }
    }

    /// <inheritdoc />
    public TokenClaims ExtractClaims(ClaimsPrincipal principal)
    {
        var claims = new TokenClaims();
        
        foreach (var claim in principal.Claims)
        {
            claims.AllClaims[claim.Type] = claim.Value;
            
            switch (claim.Type)
            {
                case "tid":
                    claims.TenantId = claim.Value;
                    break;
                case "oid":
                    claims.ObjectId = claim.Value;
                    break;
                case "sub":
                    claims.Subject = claim.Value;
                    break;
                case "appid":
                case "azp":
                    claims.ApplicationId = claim.Value;
                    break;
                case "aud":
                    claims.Audience = claim.Value;
                    break;
                case "iss":
                    claims.Issuer = claim.Value;
                    break;
                case "exp":
                    if (long.TryParse(claim.Value, out var exp))
                        claims.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                    break;
                case "iat":
                    if (long.TryParse(claim.Value, out var iat))
                        claims.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime;
                    break;
                case "nbf":
                    if (long.TryParse(claim.Value, out var nbf))
                        claims.NotBefore = DateTimeOffset.FromUnixTimeSeconds(nbf).UtcDateTime;
                    break;
                case "scp":
                case "scope":
                    claims.Scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;
                case "roles":
                    claims.Roles.Add(claim.Value);
                    break;
                case "upn":
                    claims.UserPrincipalName = claim.Value;
                    break;
                case "name":
                    claims.DisplayName = claim.Value;
                    break;
            }
        }

        return claims;
    }

    private async Task<TokenValidationParameters> GetValidationParametersAsync(CancellationToken cancellationToken)
    {
        // Refresh parameters periodically (every hour)
        if (_validationParameters != null && DateTime.UtcNow - _parametersLastRefresh < TimeSpan.FromHours(1))
        {
            return _validationParameters;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_validationParameters != null && DateTime.UtcNow - _parametersLastRefresh < TimeSpan.FromHours(1))
            {
                return _validationParameters;
            }

            _logger.LogInformation("Refreshing token validation parameters from {Endpoint}", _options.MetadataEndpoint);

            var configManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
                _options.MetadataEndpoint,
                new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());

            var config = await configManager.GetConfigurationAsync(cancellationToken);

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _options.ValidateIssuer,
                ValidIssuers = _options.ValidIssuers.Count > 0 ? _options.ValidIssuers : null,
                ValidateAudience = _options.ValidateAudience,
                ValidAudience = _options.Audience,
                ValidateLifetime = _options.ValidateLifetime,
                ValidateIssuerSigningKey = _options.ValidateSigningKey,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = _options.ClockSkew
            };

            _parametersLastRefresh = DateTime.UtcNow;
            _logger.LogInformation("Token validation parameters refreshed successfully");

            return _validationParameters;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}

/// <summary>
/// HTTP document retriever for OIDC configuration.
/// </summary>
internal class HttpDocumentRetriever : Microsoft.IdentityModel.Protocols.IDocumentRetriever
{
    private static readonly HttpClient _httpClient = new();

    public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
    {
        return await _httpClient.GetStringAsync(address, cancel);
    }
}
