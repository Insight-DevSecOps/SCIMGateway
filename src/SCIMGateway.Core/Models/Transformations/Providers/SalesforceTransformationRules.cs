// ==========================================================================
// T101: SalesforceTransformationRules - Default Salesforce Transformation Rules
// ==========================================================================
// Provides example transformation rules for Salesforce provider
// Maps SCIM Groups to Salesforce Roles, Permission Sets, and Profiles
// ==========================================================================

namespace SCIMGateway.Core.Models.Transformations.Providers;

/// <summary>
/// Factory for creating default Salesforce transformation rules.
/// Salesforce supports:
/// - Roles: Hierarchical structure (CEO → VP → Manager → Rep)
/// - Permission Sets: Additional permissions beyond profile
/// - Profiles: Base access level (Standard User, System Administrator)
/// </summary>
public static class SalesforceTransformationRules
{
    /// <summary>
    /// Target types for Salesforce entitlements.
    /// </summary>
    public static class TargetTypes
    {
        public const string Role = "SALESFORCE_ROLE";
        public const string PermissionSet = "SALESFORCE_PERMISSION_SET";
        public const string Profile = "SALESFORCE_PROFILE";
    }

    /// <summary>
    /// Common Salesforce role names.
    /// </summary>
    public static class Roles
    {
        public const string CEO = "CEO";
        public const string VPSales = "VP_Sales";
        public const string VPMarketing = "VP_Marketing";
        public const string SalesManager = "Sales_Manager";
        public const string SalesRepresentative = "Sales_Representative";
        public const string MarketingManager = "Marketing_Manager";
        public const string MarketingRepresentative = "Marketing_Representative";
        public const string SupportManager = "Support_Manager";
        public const string SupportRepresentative = "Support_Representative";
    }

    /// <summary>
    /// Common Salesforce permission set names.
    /// </summary>
    public static class PermissionSets
    {
        public const string SalesCloud = "Sales_Cloud_User";
        public const string ServiceCloud = "Service_Cloud_User";
        public const string MarketingCloud = "Marketing_Cloud_User";
        public const string ReportBuilder = "Report_Builder";
        public const string DashboardCreator = "Dashboard_Creator";
        public const string DataExport = "Data_Export";
        public const string ApiAccess = "API_Access";
    }

    /// <summary>
    /// Creates the default set of Salesforce transformation rules.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the rules.</param>
    /// <param name="providerId">Provider ID (e.g., "salesforce-prod").</param>
    /// <returns>List of default transformation rules.</returns>
    public static List<TransformationRule> CreateDefaultRules(string tenantId, string providerId = "salesforce")
    {
        var rules = new List<TransformationRule>();
        var now = DateTime.UtcNow;
        int priority = 1;

        // ===================================================================
        // EXACT Match Rules - High Priority (specific group names)
        // ===================================================================

        // Executive roles
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Executives",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.CEO,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.HIGHEST_PRIVILEGE,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 100,
                ["description"] = "Executive leadership group"
            },
            Examples = [
                new TransformationExample { Input = "Executives", ExpectedOutput = Roles.CEO }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Sales Team → Sales Representative
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Sales Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.SalesRepresentative,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 10,
                ["description"] = "Standard sales team member"
            },
            Examples = [
                new TransformationExample { Input = "Sales Team", ExpectedOutput = Roles.SalesRepresentative }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Sales Managers → Sales Manager
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Sales Managers",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.SalesManager,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.HIGHEST_PRIVILEGE,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 50,
                ["description"] = "Sales management team"
            },
            Examples = [
                new TransformationExample { Input = "Sales Managers", ExpectedOutput = Roles.SalesManager }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Marketing Team → Marketing Representative
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Marketing Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.MarketingRepresentative,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 10,
                ["description"] = "Standard marketing team member"
            },
            Examples = [
                new TransformationExample { Input = "Marketing Team", ExpectedOutput = Roles.MarketingRepresentative }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Support Team → Support Representative
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Support Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.SupportRepresentative,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 10,
                ["description"] = "Standard support team member"
            },
            Examples = [
                new TransformationExample { Input = "Support Team", ExpectedOutput = Roles.SupportRepresentative }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // REGEX Match Rules - Regional Sales Teams
        // ===================================================================

        // Sales-{Region} → Sales_{Region}_Rep
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^Sales-(\w+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = "Sales_${1}_Rep",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 10,
                ["description"] = "Regional sales teams (Sales-EMEA, Sales-APAC, etc.)"
            },
            Examples = [
                new TransformationExample { Input = "Sales-EMEA", ExpectedOutput = "Sales_EMEA_Rep" },
                new TransformationExample { Input = "Sales-APAC", ExpectedOutput = "Sales_APAC_Rep" },
                new TransformationExample { Input = "Sales-Americas", ExpectedOutput = "Sales_Americas_Rep" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Marketing-{Region} → Marketing_{Region}_Rep
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^Marketing-(\w+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = "Marketing_${1}_Rep",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 10,
                ["description"] = "Regional marketing teams"
            },
            Examples = [
                new TransformationExample { Input = "Marketing-EMEA", ExpectedOutput = "Marketing_EMEA_Rep" },
                new TransformationExample { Input = "Marketing-APAC", ExpectedOutput = "Marketing_APAC_Rep" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // CONDITIONAL Rules - Manager Detection
        // ===================================================================

        // Any group containing "Manager" → appropriate manager role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.CONDITIONAL,
            SourcePattern = "Manager",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.SalesManager,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.HIGHEST_PRIVILEGE,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 50,
                ["condition"] = "CONTAINS",
                ["description"] = "Groups containing 'Manager' get manager-level access"
            },
            Examples = [
                new TransformationExample { Input = "Regional Manager", ExpectedOutput = Roles.SalesManager },
                new TransformationExample { Input = "Account Manager", ExpectedOutput = Roles.SalesManager }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Any group containing "VP" or "Director" → VP role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.REGEX,
            SourcePattern = @".*(VP|Director).*",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.VPSales,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.HIGHEST_PRIVILEGE,
            Metadata = new Dictionary<string, object>
            {
                ["privilegeLevel"] = 80,
                ["description"] = "VP and Director level groups"
            },
            Examples = [
                new TransformationExample { Input = "VP Sales", ExpectedOutput = Roles.VPSales },
                new TransformationExample { Input = "Sales Director", ExpectedOutput = Roles.VPSales }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // Permission Set Rules
        // ===================================================================

        // API Access group → API Permission Set
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "API Access",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.PermissionSet,
            TargetMapping = PermissionSets.ApiAccess,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "API access permission set"
            },
            Examples = [
                new TransformationExample { Input = "API Access", ExpectedOutput = PermissionSets.ApiAccess }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Report Builders → Report Builder Permission Set
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Report Builders",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.PermissionSet,
            TargetMapping = PermissionSets.ReportBuilder,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Report building permission set"
            },
            Examples = [
                new TransformationExample { Input = "Report Builders", ExpectedOutput = PermissionSets.ReportBuilder }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Data Export group → Data Export Permission Set
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Data Export",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.PermissionSet,
            TargetMapping = PermissionSets.DataExport,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Data export permission set"
            },
            Examples = [
                new TransformationExample { Input = "Data Export", ExpectedOutput = PermissionSets.DataExport }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Sales Cloud Users → Sales Cloud Permission Set
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Sales Cloud Users",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.PermissionSet,
            TargetMapping = PermissionSets.SalesCloud,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Sales Cloud product access"
            },
            Examples = [
                new TransformationExample { Input = "Sales Cloud Users", ExpectedOutput = PermissionSets.SalesCloud }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Service Cloud Users → Service Cloud Permission Set
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Salesforce",
            RuleType = RuleType.EXACT,
            SourcePattern = "Service Cloud Users",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.PermissionSet,
            TargetMapping = PermissionSets.ServiceCloud,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Service Cloud product access"
            },
            Examples = [
                new TransformationExample { Input = "Service Cloud Users", ExpectedOutput = PermissionSets.ServiceCloud }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        return rules;
    }

    /// <summary>
    /// Creates a privilege ranking dictionary for HIGHEST_PRIVILEGE conflict resolution.
    /// </summary>
    /// <returns>Dictionary mapping role names to privilege levels.</returns>
    public static Dictionary<string, int> GetPrivilegeRanking()
    {
        return new Dictionary<string, int>
        {
            [Roles.CEO] = 100,
            [Roles.VPSales] = 80,
            [Roles.VPMarketing] = 80,
            [Roles.SalesManager] = 50,
            [Roles.MarketingManager] = 50,
            [Roles.SupportManager] = 50,
            [Roles.SalesRepresentative] = 10,
            [Roles.MarketingRepresentative] = 10,
            [Roles.SupportRepresentative] = 10
        };
    }

    /// <summary>
    /// Gets the role hierarchy for Salesforce (parent-child relationships).
    /// </summary>
    /// <returns>Dictionary mapping roles to their parent role.</returns>
    public static Dictionary<string, string> GetRoleHierarchy()
    {
        return new Dictionary<string, string>
        {
            [Roles.VPSales] = Roles.CEO,
            [Roles.VPMarketing] = Roles.CEO,
            [Roles.SalesManager] = Roles.VPSales,
            [Roles.MarketingManager] = Roles.VPMarketing,
            [Roles.SupportManager] = Roles.VPSales,
            [Roles.SalesRepresentative] = Roles.SalesManager,
            [Roles.MarketingRepresentative] = Roles.MarketingManager,
            [Roles.SupportRepresentative] = Roles.SupportManager
        };
    }
}
