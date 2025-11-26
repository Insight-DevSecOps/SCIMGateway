// ==========================================================================
// T103: ServiceNowTransformationRules - Default ServiceNow Transformation Rules
// ==========================================================================
// Provides example transformation rules for ServiceNow provider
// Maps SCIM Groups to ServiceNow Groups (native group support)
// ServiceNow uses sys_user_group with direct group mapping
// ==========================================================================

namespace SCIMGateway.Core.Models.Transformations.Providers;

/// <summary>
/// Factory for creating default ServiceNow transformation rules.
/// ServiceNow supports:
/// - sys_user_group: Native groups for access control
/// - Roles: Additional permissions assigned to groups
/// - Assignment Groups: Groups for ticket/task assignment
/// - Approval Groups: Groups for approval workflows
/// </summary>
public static class ServiceNowTransformationRules
{
    /// <summary>
    /// Target types for ServiceNow entitlements.
    /// </summary>
    public static class TargetTypes
    {
        public const string Group = "SERVICENOW_GROUP";
        public const string Role = "SERVICENOW_ROLE";
        public const string AssignmentGroup = "SERVICENOW_ASSIGNMENT_GROUP";
        public const string ApprovalGroup = "SERVICENOW_APPROVAL_GROUP";
        public const string Catalog = "SERVICENOW_CATALOG";
    }

    /// <summary>
    /// Common ServiceNow group prefixes.
    /// </summary>
    public static class GroupPrefixes
    {
        public const string IT = "IT_";
        public const string HR = "HR_";
        public const string Finance = "FIN_";
        public const string Operations = "OPS_";
        public const string Support = "SUP_";
        public const string Development = "DEV_";
        public const string Security = "SEC_";
    }

    /// <summary>
    /// Common ServiceNow roles.
    /// </summary>
    public static class Roles
    {
        public const string ItilUser = "itil";
        public const string ItilAdmin = "itil_admin";
        public const string ApprovalUser = "approver_user";
        public const string CatalogAdmin = "catalog_admin";
        public const string IncidentManager = "incident_manager";
        public const string ChangeManager = "change_manager";
        public const string ProblemManager = "problem_manager";
        public const string AssetManager = "asset_manager";
        public const string KnowledgeAdmin = "knowledge_admin";
    }

    /// <summary>
    /// Creates the default set of ServiceNow transformation rules.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the rules.</param>
    /// <param name="providerId">Provider ID (e.g., "servicenow-prod").</param>
    /// <returns>List of default transformation rules.</returns>
    public static List<TransformationRule> CreateDefaultRules(string tenantId, string providerId = "servicenow")
    {
        var rules = new List<TransformationRule>();
        var now = DateTime.UtcNow;
        int priority = 1;

        // ===================================================================
        // EXACT Match Rules - Direct Group Mapping (ServiceNow native)
        // ===================================================================

        // IT Support → ServiceNow IT Support Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "IT Support",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Group,
            TargetMapping = $"{GroupPrefixes.Support}IT_Support",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["description"] = "IT Support desk group"
            },
            Examples = [
                new TransformationExample { Input = "IT Support", ExpectedOutput = $"{GroupPrefixes.Support}IT_Support" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Help Desk → ServiceNow Help Desk Group (Assignment Group)
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Help Desk",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.AssignmentGroup,
            TargetMapping = $"{GroupPrefixes.Support}Help_Desk",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["defaultQueue"] = "incident",
                ["description"] = "Level 1 help desk for incident assignment"
            },
            Examples = [
                new TransformationExample { Input = "Help Desk", ExpectedOutput = $"{GroupPrefixes.Support}Help_Desk" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Network Operations → ServiceNow Network Ops Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Network Operations",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.AssignmentGroup,
            TargetMapping = $"{GroupPrefixes.Operations}Network",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["escalationLevel"] = 2,
                ["description"] = "Network operations team"
            },
            Examples = [
                new TransformationExample { Input = "Network Operations", ExpectedOutput = $"{GroupPrefixes.Operations}Network" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Security Team → ServiceNow Security Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Security Team",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Group,
            TargetMapping = $"{GroupPrefixes.Security}Team",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["securityIncidents"] = true,
                ["description"] = "Information security team"
            },
            Examples = [
                new TransformationExample { Input = "Security Team", ExpectedOutput = $"{GroupPrefixes.Security}Team" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Change Approval Board → ServiceNow CAB Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Change Approval Board",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.ApprovalGroup,
            TargetMapping = "CAB_Approvers",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["approvalGroup"] = true,
                ["changeType"] = "normal",
                ["description"] = "Change Advisory Board for change approvals"
            },
            Examples = [
                new TransformationExample { Input = "Change Approval Board", ExpectedOutput = "CAB_Approvers" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // REGEX Match Rules - Department-Based Groups
        // ===================================================================

        // {Department} Team → ServiceNow {Department} Group
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^(\w+) Team$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Group,
            TargetMapping = "GRP_${1}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["description"] = "Generic team to group mapping"
            },
            Examples = [
                new TransformationExample { Input = "Engineering Team", ExpectedOutput = "GRP_Engineering" },
                new TransformationExample { Input = "Marketing Team", ExpectedOutput = "GRP_Marketing" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Support-{Level} → ServiceNow Support Level Groups
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^Support-L(\d)$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.AssignmentGroup,
            TargetMapping = $"{GroupPrefixes.Support}Level_${{1}}",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["description"] = "Support tier groups by level"
            },
            Examples = [
                new TransformationExample { Input = "Support-L1", ExpectedOutput = $"{GroupPrefixes.Support}Level_1" },
                new TransformationExample { Input = "Support-L2", ExpectedOutput = $"{GroupPrefixes.Support}Level_2" },
                new TransformationExample { Input = "Support-L3", ExpectedOutput = $"{GroupPrefixes.Support}Level_3" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // App-{AppName}-Support → Application Support Groups
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.REGEX,
            SourcePattern = @"^App-(.+)-Support$",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.AssignmentGroup,
            TargetMapping = "APP_${1}_Support",
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["assignmentGroup"] = true,
                ["description"] = "Application-specific support groups"
            },
            Examples = [
                new TransformationExample { Input = "App-Salesforce-Support", ExpectedOutput = "APP_Salesforce_Support" },
                new TransformationExample { Input = "App-SAP-Support", ExpectedOutput = "APP_SAP_Support" }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // EXACT Rules - Role Assignments
        // ===================================================================

        // ITIL Users → itil role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "ITIL Users",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ItilUser,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "base",
                ["description"] = "Basic ITIL user access"
            },
            Examples = [
                new TransformationExample { Input = "ITIL Users", ExpectedOutput = Roles.ItilUser }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ITIL Admins → itil_admin role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "ITIL Admins",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ItilAdmin,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "admin",
                ["description"] = "ITIL administrator access"
            },
            Examples = [
                new TransformationExample { Input = "ITIL Admins", ExpectedOutput = Roles.ItilAdmin }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Incident Managers → incident_manager role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Incident Managers",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.IncidentManager,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "manager",
                ["module"] = "incident",
                ["description"] = "Incident management role"
            },
            Examples = [
                new TransformationExample { Input = "Incident Managers", ExpectedOutput = Roles.IncidentManager }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Change Managers → change_manager role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Change Managers",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ChangeManager,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "manager",
                ["module"] = "change",
                ["description"] = "Change management role"
            },
            Examples = [
                new TransformationExample { Input = "Change Managers", ExpectedOutput = Roles.ChangeManager }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Problem Managers → problem_manager role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Problem Managers",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ProblemManager,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "manager",
                ["module"] = "problem",
                ["description"] = "Problem management role"
            },
            Examples = [
                new TransformationExample { Input = "Problem Managers", ExpectedOutput = Roles.ProblemManager }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Knowledge Admins → knowledge_admin role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Knowledge Admins",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.KnowledgeAdmin,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "admin",
                ["module"] = "knowledge",
                ["description"] = "Knowledge base administrator"
            },
            Examples = [
                new TransformationExample { Input = "Knowledge Admins", ExpectedOutput = Roles.KnowledgeAdmin }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // Approvers → approver_user role
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.EXACT,
            SourcePattern = "Approvers",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ApprovalUser,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["roleType"] = "approver",
                ["description"] = "General approval capability"
            },
            Examples = [
                new TransformationExample { Input = "Approvers", ExpectedOutput = Roles.ApprovalUser }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        // ===================================================================
        // CONDITIONAL Rules - Manager Detection
        // ===================================================================

        // Groups containing "Manager" get manager-level roles
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ProviderId = providerId,
            ProviderName = "ServiceNow",
            RuleType = RuleType.CONDITIONAL,
            SourcePattern = "Manager",
            SourceType = "SCIM_GROUP",
            TargetType = TargetTypes.Role,
            TargetMapping = Roles.ApprovalUser,
            Priority = priority++,
            Enabled = true,
            ConflictResolution = ConflictResolutionStrategy.UNION,
            Metadata = new Dictionary<string, object>
            {
                ["condition"] = "CONTAINS",
                ["description"] = "All managers get approval capability"
            },
            Examples = [
                new TransformationExample { Input = "IT Manager", ExpectedOutput = Roles.ApprovalUser },
                new TransformationExample { Input = "Department Manager", ExpectedOutput = Roles.ApprovalUser }
            ],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system"
        });

        return rules;
    }

    /// <summary>
    /// Gets the ServiceNow ITSM module mappings.
    /// </summary>
    public static Dictionary<string, string> GetModuleMappings()
    {
        return new Dictionary<string, string>
        {
            ["incident"] = "Incident Management",
            ["change"] = "Change Management",
            ["problem"] = "Problem Management",
            ["request"] = "Service Request",
            ["catalog"] = "Service Catalog",
            ["knowledge"] = "Knowledge Management",
            ["asset"] = "Asset Management",
            ["cmdb"] = "Configuration Management"
        };
    }

    /// <summary>
    /// Gets the support level escalation hierarchy.
    /// </summary>
    public static List<string> GetSupportLevelHierarchy()
    {
        return [$"{GroupPrefixes.Support}Level_1", $"{GroupPrefixes.Support}Level_2", $"{GroupPrefixes.Support}Level_3"];
    }
}
