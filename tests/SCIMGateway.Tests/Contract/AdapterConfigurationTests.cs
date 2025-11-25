// ==========================================================================
// T011a: Contract Test for AdapterConfiguration Model
// ==========================================================================
// Validates the AdapterConfiguration model meets all requirements from:
// - contracts/adapter-interface.md
// - tasks.md T011a specification
// 
// Required fields to validate:
// - credentials (API keys, OAuth tokens)
// - endpoints (base URLs, API versions)
// - transformation rules (group/entitlement mappings)
// - rate limits (per-tenant, per-adapter)
// - timeouts (connection, request)
// - retry policy (max retries, backoff strategy)
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for AdapterConfiguration model.
/// These tests define the expected shape and behavior of the configuration model
/// that all adapters must use.
/// </summary>
public class AdapterConfigurationTests
{
    #region Required Fields Tests

    [Fact]
    public void AdapterConfiguration_Should_Have_Credentials_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var credentialsProperty = configurationType.GetProperty("Credentials");
        Assert.NotNull(credentialsProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_Endpoints_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var endpointsProperty = configurationType.GetProperty("Endpoints");
        Assert.NotNull(endpointsProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_TransformationRules_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var transformationRulesProperty = configurationType.GetProperty("TransformationRules");
        Assert.NotNull(transformationRulesProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_RateLimits_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var rateLimitsProperty = configurationType.GetProperty("RateLimits");
        Assert.NotNull(rateLimitsProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_Timeouts_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var timeoutsProperty = configurationType.GetProperty("Timeouts");
        Assert.NotNull(timeoutsProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_RetryPolicy_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var retryPolicyProperty = configurationType.GetProperty("RetryPolicy");
        Assert.NotNull(retryPolicyProperty);
    }

    #endregion

    #region Credentials Validation Tests

    [Fact]
    public void Credentials_Should_Support_ApiKey_Authentication()
    {
        // Arrange & Act
        var credentialsType = GetCredentialsType();
        
        // Assert
        Assert.NotNull(credentialsType);
        var apiKeyProperty = credentialsType.GetProperty("ApiKey");
        Assert.NotNull(apiKeyProperty);
    }

    [Fact]
    public void Credentials_Should_Support_OAuth_Authentication()
    {
        // Arrange & Act
        var credentialsType = GetCredentialsType();
        
        // Assert
        Assert.NotNull(credentialsType);
        // OAuth requires ClientId, ClientSecret, and TokenEndpoint
        Assert.NotNull(credentialsType.GetProperty("ClientId"));
        Assert.NotNull(credentialsType.GetProperty("ClientSecret"));
        Assert.NotNull(credentialsType.GetProperty("TokenEndpoint"));
    }

    [Fact]
    public void Credentials_Should_Have_AuthenticationType_Enum()
    {
        // Arrange & Act
        var credentialsType = GetCredentialsType();
        
        // Assert
        Assert.NotNull(credentialsType);
        var authTypeProperty = credentialsType.GetProperty("AuthenticationType");
        Assert.NotNull(authTypeProperty);
    }

    #endregion

    #region Endpoints Validation Tests

    [Fact]
    public void Endpoints_Should_Have_BaseUrl_Property()
    {
        // Arrange & Act
        var endpointsType = GetEndpointsType();
        
        // Assert
        Assert.NotNull(endpointsType);
        var baseUrlProperty = endpointsType.GetProperty("BaseUrl");
        Assert.NotNull(baseUrlProperty);
    }

    [Fact]
    public void Endpoints_Should_Have_ApiVersion_Property()
    {
        // Arrange & Act
        var endpointsType = GetEndpointsType();
        
        // Assert
        Assert.NotNull(endpointsType);
        var apiVersionProperty = endpointsType.GetProperty("ApiVersion");
        Assert.NotNull(apiVersionProperty);
    }

    #endregion

    #region Rate Limits Validation Tests

    [Fact]
    public void RateLimits_Should_Have_MaxRequestsPerMinute_Property()
    {
        // Arrange & Act
        var rateLimitsType = GetRateLimitsType();
        
        // Assert
        Assert.NotNull(rateLimitsType);
        var maxRequestsProperty = rateLimitsType.GetProperty("MaxRequestsPerMinute");
        Assert.NotNull(maxRequestsProperty);
    }

    [Fact]
    public void RateLimits_Should_Have_MaxRequestsPerSecond_Property()
    {
        // Arrange & Act
        var rateLimitsType = GetRateLimitsType();
        
        // Assert
        Assert.NotNull(rateLimitsType);
        var maxRequestsProperty = rateLimitsType.GetProperty("MaxRequestsPerSecond");
        Assert.NotNull(maxRequestsProperty);
    }

    [Fact]
    public void RateLimits_Should_Have_BurstLimit_Property()
    {
        // Arrange & Act
        var rateLimitsType = GetRateLimitsType();
        
        // Assert
        Assert.NotNull(rateLimitsType);
        var burstLimitProperty = rateLimitsType.GetProperty("BurstLimit");
        Assert.NotNull(burstLimitProperty);
    }

    #endregion

    #region Timeouts Validation Tests

    [Fact]
    public void Timeouts_Should_Have_ConnectionTimeout_Property()
    {
        // Arrange & Act
        var timeoutsType = GetTimeoutsType();
        
        // Assert
        Assert.NotNull(timeoutsType);
        var connectionTimeoutProperty = timeoutsType.GetProperty("ConnectionTimeout");
        Assert.NotNull(connectionTimeoutProperty);
    }

    [Fact]
    public void Timeouts_Should_Have_RequestTimeout_Property()
    {
        // Arrange & Act
        var timeoutsType = GetTimeoutsType();
        
        // Assert
        Assert.NotNull(timeoutsType);
        var requestTimeoutProperty = timeoutsType.GetProperty("RequestTimeout");
        Assert.NotNull(requestTimeoutProperty);
    }

    [Fact]
    public void Timeouts_Should_Use_TimeSpan_Type()
    {
        // Arrange & Act
        var timeoutsType = GetTimeoutsType();
        
        // Assert
        Assert.NotNull(timeoutsType);
        var connectionTimeoutProperty = timeoutsType.GetProperty("ConnectionTimeout");
        Assert.NotNull(connectionTimeoutProperty);
        Assert.Equal(typeof(TimeSpan), connectionTimeoutProperty.PropertyType);
    }

    #endregion

    #region Retry Policy Validation Tests

    [Fact]
    public void RetryPolicy_Should_Have_MaxRetries_Property()
    {
        // Arrange & Act
        var retryPolicyType = GetRetryPolicyType();
        
        // Assert
        Assert.NotNull(retryPolicyType);
        var maxRetriesProperty = retryPolicyType.GetProperty("MaxRetries");
        Assert.NotNull(maxRetriesProperty);
    }

    [Fact]
    public void RetryPolicy_Should_Have_InitialDelay_Property()
    {
        // Arrange & Act
        var retryPolicyType = GetRetryPolicyType();
        
        // Assert
        Assert.NotNull(retryPolicyType);
        var initialDelayProperty = retryPolicyType.GetProperty("InitialDelay");
        Assert.NotNull(initialDelayProperty);
    }

    [Fact]
    public void RetryPolicy_Should_Have_MaxDelay_Property()
    {
        // Arrange & Act
        var retryPolicyType = GetRetryPolicyType();
        
        // Assert
        Assert.NotNull(retryPolicyType);
        var maxDelayProperty = retryPolicyType.GetProperty("MaxDelay");
        Assert.NotNull(maxDelayProperty);
    }

    [Fact]
    public void RetryPolicy_Should_Have_BackoffMultiplier_Property()
    {
        // Arrange & Act
        var retryPolicyType = GetRetryPolicyType();
        
        // Assert
        Assert.NotNull(retryPolicyType);
        var backoffMultiplierProperty = retryPolicyType.GetProperty("BackoffMultiplier");
        Assert.NotNull(backoffMultiplierProperty);
    }

    [Fact]
    public void RetryPolicy_Should_Have_RetryableStatusCodes_Property()
    {
        // Arrange & Act
        var retryPolicyType = GetRetryPolicyType();
        
        // Assert
        Assert.NotNull(retryPolicyType);
        var retryableStatusCodesProperty = retryPolicyType.GetProperty("RetryableStatusCodes");
        Assert.NotNull(retryableStatusCodesProperty);
    }

    #endregion

    #region Schema Validation Tests

    [Fact]
    public void AdapterConfiguration_Should_Have_ProviderId_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var providerIdProperty = configurationType.GetProperty("ProviderId");
        Assert.NotNull(providerIdProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_TenantId_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var tenantIdProperty = configurationType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    [Fact]
    public void AdapterConfiguration_Should_Have_Enabled_Property()
    {
        // Arrange & Act
        var configurationType = GetAdapterConfigurationType();
        
        // Assert
        Assert.NotNull(configurationType);
        var enabledProperty = configurationType.GetProperty("Enabled");
        Assert.NotNull(enabledProperty);
    }

    #endregion

    #region Helper Methods

    private static Type? GetAdapterConfigurationType()
    {
        // Try to find the AdapterConfiguration type in the Core assembly
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.AdapterConfiguration")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.AdapterConfiguration");
    }

    private static Type? GetCredentialsType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.AdapterCredentials")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.AdapterCredentials");
    }

    private static Type? GetEndpointsType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.AdapterEndpoints")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.AdapterEndpoints");
    }

    private static Type? GetRateLimitsType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.RateLimitConfiguration")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.RateLimitConfiguration");
    }

    private static Type? GetTimeoutsType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.TimeoutConfiguration")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.TimeoutConfiguration");
    }

    private static Type? GetRetryPolicyType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.RetryPolicyConfiguration")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Adapters.RetryPolicyConfiguration");
    }

    #endregion
}
