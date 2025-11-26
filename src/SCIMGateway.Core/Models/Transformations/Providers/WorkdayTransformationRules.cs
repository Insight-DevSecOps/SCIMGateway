// ==========================================================================
// T102: WorkdayTransformationRules - Default Workday Transformation Rules
// ==========================================================================
// Provides example transformation rules for Workday provider
// Maps SCIM Groups to Workday Organization Units using HIERARCHICAL patterns
// ==========================================================================

namespace SCIMGateway.Core.Models.Transformations.Providers;

/// <summary>
/// Factory for creating default Workday transformation rules.
/// Workday uses hierarchical organization structure:
/// - Company → Division → Department → Team
/// - Supervisory Organizations (reporting structure)
/// - Cost Centers (financial groupings)
/// - Location Hierarchy
/// </summary>
public static class WorkdayTransformationRules
{
    /// <summary>
    /// Target types for Workday entitlements.
    /// </summary>
    public static class TargetTypes
    {
        public const string OrganizationUnit = "WORKDAY_ORG_UNIT";
        public const string SupervisoryOrganization = "WORKDAY_SUPERVISORY_ORG";
        public const string CostCenter = "WORKDAY_COST_CENTER";
        public const string Location = "WORKDAY_LOCATION";
        public const string SecurityGroup = "WORKDAY_SECURITY_GROUP";
        public const string JobFamily = "WORKDAY_JOB_FAMILY";
    }

    /// <summary>
    /// Common organization prefixes.
    /// </summary>
    public static class OrgPrefixes
    {
        public const string Division = "DIV";
        public const string Department = "DEPT";
        public const string Team = "TEAM";
        public const string CostCenter = "CC";
        public const string Location = "LOC";
        public const string SupervisoryOrg = "SO";
    }

    /// <summary>
    /// Creates the default set of Workday transformation rules.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the rules.</param>
    /// <param name="providerId">Provider ID (e.g., "workday-prod").</param>
    /// <returns>List of default transformation rules.</returns>
    public static List<TransformationRule> CreateDefaultRules(string tenantId, string providerId = "workday")
    {
        var rules = new List<TransformationRule>();
        var now = DateTime.UtcNow;
        int priority = 1;

        // ===================================================================
        // HIERARCHICAL Rules - Organization Path Mapping
        // ===================================================================

        // Full hierarchy: Company/Division/Department/Team → TEAM-{Team}
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.HIERARCHICAL,
            SourcePattern = "Company/Division/Department/Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.OrganizationUnit,
            TargetMapping = $"{OrgPrefixes.Team}-${{level3}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["hierarchyDepth"] = 4,
                ["description"] = "Four-level organization hierarchy mapping to team"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "Acme Corp/Sales/Enterprise/West Coast", 
                    ExpectedOutput = $"{OrgPrefixes.Team}-West Coast" 
                },
                new TransformationExample 
                { 
                    Input = "Acme Corp/Engineering/Platform/Backend", 
                    ExpectedOutput = $"{OrgPrefixes.Team}-Backend" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Three-level hierarchy: Company/Division/Department → DEPT-{Department}
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.HIERARCHICAL,
            SourcePattern = "Company/Division/Department",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.OrganizationUnit,
            TargetMapping = $"{OrgPrefixes.Department}-${{level2}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["hierarchyDepth"] = 3,
                ["description"] = "Three-level organization hierarchy mapping to department"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "Acme Corp/Sales/Enterprise", 
                    ExpectedOutput = $"{OrgPrefixes.Department}-Enterprise" 
                },
                new TransformationExample 
                { 
                    Input = "Acme Corp/Engineering/Platform", 
                    ExpectedOutput = $"{OrgPrefixes.Department}-Platform" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Two-level hierarchy: Company/Division → DIV-{Division}
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.HIERARCHICAL,
            SourcePattern = "Company/Division",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.OrganizationUnit,
            TargetMapping = $"{OrgPrefixes.Division}-${{level1}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["hierarchyDepth"] = 2,
                ["description"] = "Two-level organization hierarchy mapping to division"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "Acme Corp/Sales", 
                    ExpectedOutput = $"{OrgPrefixes.Division}-Sales" 
                },
                new TransformationExample 
                { 
                    Input = "Acme Corp/Engineering", 
                    ExpectedOutput = $"{OrgPrefixes.Division}-Engineering" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // HIERARCHICAL Rules - Geographic Organization
        // ===================================================================

        // Region/Country/City → LOC-{City}
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.HIERARCHICAL,
            SourcePattern = "Region/Country/City",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Location,
            TargetMapping = $"{OrgPrefixes.Location}-${{level2}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["hierarchyDepth"] = 3,
                ["description"] = "Geographic hierarchy mapping to location"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "EMEA/UK/London", 
                    ExpectedOutput = $"{OrgPrefixes.Location}-London" 
                },
                new TransformationExample 
                { 
                    Input = "Americas/US/New York", 
                    ExpectedOutput = $"{OrgPrefixes.Location}-New York" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // REGEX Rules - Cost Center Mapping
        // ===================================================================

        // CC-{Number} → WORKDAY_COST_CENTER
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^CC-(\d+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.CostCenter,
            TargetMapping = $"{OrgPrefixes.CostCenter}-${{1}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Cost center code mapping"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "CC-1001", 
                    ExpectedOutput = $"{OrgPrefixes.CostCenter}-1001" 
                },
                new TransformationExample 
                { 
                    Input = "CC-2500", 
                    ExpectedOutput = $"{OrgPrefixes.CostCenter}-2500" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // CostCenter-{Name} → WORKDAY_COST_CENTER
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^CostCenter-(.+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.CostCenter,
            TargetMapping = $"{OrgPrefixes.CostCenter}-${{1}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Named cost center mapping"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "CostCenter-Marketing", 
                    ExpectedOutput = $"{OrgPrefixes.CostCenter}-Marketing" 
                },
                new TransformationExample 
                { 
                    Input = "CostCenter-Engineering", 
                    ExpectedOutput = $"{OrgPrefixes.CostCenter}-Engineering" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // REGEX Rules - Supervisory Organization
        // ===================================================================

        // Manager-{Name} → Supervisory Org
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^Manager-(.+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.SupervisoryOrganization,
            TargetMapping = $"{OrgPrefixes.SupervisoryOrg}-${{1}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Manager direct reports group to supervisory org"
            },
            Examples = [
                new TransformationExample 
                { 
                    Input = "Manager-John Smith", 
                    ExpectedOutput = $"{OrgPrefixes.SupervisoryOrg}-John Smith" 
                }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // EXACT Rules - Security Groups
        // ===================================================================

        // HR Admins → HR Security Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.EXACT,
            SourcePattern = "HR Admins",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.SecurityGroup,
            TargetMapping = "SG-HR_Admin",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "HR administrators security group"
            },
            Examples = [
                new TransformationExample { Input = "HR Admins", ExpectedOutput = "SG-HR_Admin" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Finance Admins → Finance Security Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.EXACT,
            SourcePattern = "Finance Admins",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.SecurityGroup,
            TargetMapping = "SG-Finance_Admin",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Finance administrators security group"
            },
            Examples = [
                new TransformationExample { Input = "Finance Admins", ExpectedOutput = "SG-Finance_Admin" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Payroll Team → Payroll Security Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.EXACT,
            SourcePattern = "Payroll Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.SecurityGroup,
            TargetMapping = "SG-Payroll",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Payroll team security group"
            },
            Examples = [
                new TransformationExample { Input = "Payroll Team", ExpectedOutput = "SG-Payroll" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Benefits Team → Benefits Security Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.EXACT,
            SourcePattern = "Benefits Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.SecurityGroup,
            TargetMapping = "SG-Benefits",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Benefits team security group"
            },
            Examples = [
                new TransformationExample { Input = "Benefits Team", ExpectedOutput = "SG-Benefits" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // REGEX Rules - Job Family Mapping
        // ===================================================================

        // JF-{Family} → Job Family
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "Workday",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^JF-(.+)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.JobFamily,
            TargetMapping = "JF-${1}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.FIRST_MATCH,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Job family group mapping"
            },
            Examples = [
                new TransformationExample { Input = "JF-Engineering", ExpectedOutput = "JF-Engineering" },
                new TransformationExample { Input = "JF-Sales", ExpectedOutput = "JF-Sales" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        return rules;
    }

    /// <summary>
    /// Parses a hierarchical group name into its component levels.
    /// </summary>
    /// <param name="groupName">The hierarchical group name.</param>
    /// <param name="separator">The path separator (default: /).</param>
    /// <returns>Dictionary of level names to values.</returns>
    public static Dictionary<string, string> ParseHierarchy(string groupName, string separator = "/")
    {
        var levels = new Dictionary<string, string>();
        var parts = groupName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < parts.Length; i++)
        {
            levels[$"level{i}"] = parts[i].Trim();
        }
        
        return levels;
    }

    /// <summary>
    /// Gets the standard Workday organization hierarchy levels.
    /// </summary>
    public static List<string> GetHierarchyLevels()
    {
        return ["Company", "Division", "Department", "Team"];
    }
}
