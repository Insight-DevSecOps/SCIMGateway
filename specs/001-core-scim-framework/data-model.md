# Data Model: Extensible SCIM Gateway SDK/Framework

**Feature**: [001-core-scim-framework](../) | **Date**: 2025-11-22 | **Phase**: 1 Design

## Entity Definitions

### SCIM User

**SCIM Standard Attributes** (RFC 7643):
- `id` (string, immutable) - Unique identifier assigned by resource server (e.g., UUID)
- `userName` (string, required) - Unique identifier for user (e.g., john.doe@company.com)
- `displayName` (string) - Display name (e.g., "John Doe")
- `name` (complex):
  - `familyName` (string) - Last name
  - `givenName` (string) - First name
  - `middleName` (string) - Middle name
  - `honorificPrefix` (string) - Title (e.g., "Dr.")
  - `honorificSuffix` (string)
- `emails` (array of complex):
  - `value` (string) - Email address
  - `type` (string) - "work", "home", "other"
  - `primary` (boolean) - Is primary email
- `phoneNumbers` (array of complex):
  - `value` (string) - Phone number
  - `type` (string) - "work", "home", "mobile", "fax"
- `active` (boolean, default: true) - User active status
- `groups` (array of complex, read-only):
  - `value` (string) - Group ID
  - `display` (string) - Group display name
  - `$ref` (string) - URL reference to group resource
- `roles` (array of strings) - User roles (provider-specific, may be populated by adapter)
- `externalId` (string) - Identifier from external system (e.g., Workday ID, Salesforce ID)
- `meta` (complex, read-only):
  - `resourceType` (string) - "User"
  - `created` (dateTime) - Resource creation timestamp
  - `lastModified` (dateTime) - Last modification timestamp
  - `location` (string) - URL reference to resource
  - `version` (string) - Resource version (for optimistic concurrency)

**Internal Attributes** (Gateway-specific):
- `tenantId` (string, required) - Entra ID tenant ID (partitioning key)
- `adapterId` (string) - ID of adapter managing this user
- `providerUserId` (string) - External ID used by provider (e.g., Salesforce user ID)
- `syncState` (enum) - "ACTIVE", "PENDING_SYNC", "FAILED", "DELETED"
- `lastSyncTimestamp` (dateTime) - Last sync operation timestamp
- `lastSyncDirection` (enum) - "ENTRA_TO_PROVIDER", "PROVIDER_TO_ENTRA", "CONFLICT"
- `providerData` (JSON) - Provider-specific attributes not in SCIM schema
- `conflictLog` (array) - History of sync conflicts for this user

**Database Representation** (Cosmos DB):
```json
{
  "id": "user-uuid",
  "resourceType": "User",
  "tenantId": "entra-tenant-id",
  "adapterId": "salesforce",
  "providerUserId": "salesforce-user-id",
  "userName": "john.doe@company.com",
  "displayName": "John Doe",
  "name": {
    "familyName": "Doe",
    "givenName": "John"
  },
  "emails": [{"value": "john.doe@company.com", "type": "work", "primary": true}],
  "active": true,
  "groups": [
    {"value": "group-id-1", "display": "Sales Team"},
    {"value": "group-id-2", "display": "EMEA Region"}
  ],
  "externalId": "workday-user-id",
  "syncState": "ACTIVE",
  "lastSyncTimestamp": "2025-11-22T14:30:00Z",
  "lastSyncDirection": "ENTRA_TO_PROVIDER",
  "meta": {
    "resourceType": "User",
    "created": "2025-11-22T10:00:00Z",
    "lastModified": "2025-11-22T14:30:00Z",
    "location": "https://gateway.azure.com/Users/user-uuid",
    "version": "W/\"3\""
  }
}
```

---

### SCIM Group

**SCIM Standard Attributes** (RFC 7643):
- `id` (string, immutable) - Unique identifier assigned by resource server
- `displayName` (string, required) - Display name (e.g., "Sales Team")
- `members` (array of complex, read-only):
  - `value` (string) - User ID
  - `display` (string) - User display name
  - `$ref` (string) - URL reference to user resource
- `description` (string) - Group description
- `externalId` (string) - External ID from source system
- `meta` (complex, read-only):
  - `resourceType` (string) - "Group"
  - `created` (dateTime)
  - `lastModified` (dateTime)
  - `location` (string)
  - `version` (string)

**Internal Attributes** (Gateway-specific):
- `tenantId` (string, required) - Entra ID tenant ID
- `adapterId` (string) - ID of adapter managing this group
- `providerGroupId` (string) - External ID used by provider
- `entitlementMapping` (array) - How this group maps to provider entitlements
  - `adapterId` (string) - Target adapter
  - `entitlementType` (enum) - "ROLE", "DEPARTMENT", "PERMISSION", "ORG_HIERARCHY_LEVEL"
  - `entitlementId` (string) - Provider-specific entitlement ID
  - `transformationRule` (string) - Rule ID applied to create this mapping
- `syncState` (enum) - "ACTIVE", "PENDING_SYNC", "FAILED", "DELETED"
- `lastSyncTimestamp` (dateTime)
- `lastSyncDirection` (enum)
- `memberCount` (integer) - Number of members in group
- `conflictLog` (array) - Sync conflict history

**Database Representation** (Cosmos DB):
```json
{
  "id": "group-uuid",
  "resourceType": "Group",
  "tenantId": "entra-tenant-id",
  "adapterId": "salesforce",
  "providerGroupId": "salesforce-group-id",
  "displayName": "Sales Team",
  "description": "North American sales organization",
  "members": [
    {"value": "user-uuid-1", "display": "John Doe"},
    {"value": "user-uuid-2", "display": "Jane Smith"}
  ],
  "externalId": "workday-group-id",
  "entitlementMapping": [
    {
      "adapterId": "salesforce",
      "entitlementType": "ROLE",
      "entitlementId": "role-salesforce-sales",
      "transformationRule": "tr-001-group-to-salesforce-role"
    }
  ],
  "syncState": "ACTIVE",
  "lastSyncTimestamp": "2025-11-22T14:30:00Z",
  "lastSyncDirection": "ENTRA_TO_PROVIDER",
  "memberCount": 25,
  "meta": {
    "resourceType": "Group",
    "created": "2025-11-22T10:00:00Z",
    "lastModified": "2025-11-22T14:30:00Z",
    "location": "https://gateway.azure.com/Groups/group-uuid",
    "version": "W/\"2\""
  }
}
```

---

### Entitlement (Provider-Specific Model)

**Attributes**:
- `id` (string) - Unique identifier within gateway (UUID)
- `providerId` (string) - Adapter/provider identifier
- `providerEntitlementId` (string) - External ID in provider system
- `name` (string) - Entitlement name (e.g., "Sales Representative", "Department: Engineering")
- `type` (enum) - "ROLE", "PERMISSION", "DEPARTMENT", "ORG_HIERARCHY_LEVEL", "CUSTOM"
- `description` (string) - Human-readable description
- `parentEntitlementId` (string) - For hierarchical entitlements (e.g., parent department)
- `metadata` (JSON) - Provider-specific attributes
- `mappedGroups` (array) - SCIM groups that map to this entitlement
  - `groupId` (string) - SCIM group ID
  - `transformationRule` (string) - Rule that created mapping
  - `active` (boolean)
- `priority` (integer) - For conflict resolution when user has conflicting entitlements
- `enabled` (boolean) - Whether entitlement is active

**Database Representation** (Cosmos DB):
```json
{
  "id": "entitlement-uuid",
  "type": "Entitlement",
  "tenantId": "entra-tenant-id",
  "providerId": "salesforce",
  "providerEntitlementId": "salesforce-role-123",
  "name": "Sales Representative",
  "entitlementType": "ROLE",
  "description": "Standard sales representative role in Salesforce",
  "metadata": {
    "salesforcePermissionSet": "perm-set-sales-rep",
    "userLimit": 100
  },
  "mappedGroups": [
    {
      "groupId": "group-uuid-1",
      "transformationRule": "tr-001-group-to-salesforce-role",
      "active": true
    }
  ],
  "priority": 1,
  "enabled": true
}
```

---

### Transformation Rule

**Attributes**:
- `id` (string) - Unique rule identifier (e.g., "tr-001-group-to-salesforce-role")
- `tenantId` (string) - Entra ID tenant
- `providerId` (string) - Target adapter/provider
- `name` (string) - Human-readable rule name
- `sourceType` (enum) - "SCIM_GROUP" (only one type initially)
- `targetType` (enum) - "ROLE", "DEPARTMENT", "PERMISSION", "ORG_HIERARCHY_LEVEL", "CUSTOM"
- `rules` (array) - Transformation rules (can have multiple rules per provider):
  - `sourcePattern` (string) - Group name pattern or expression (e.g., "Sales-*", regex)
  - `targetMapping` (object):
    - `entitlementId` (string) - Provider entitlement ID
    - `entitlementName` (string) - Human-readable name
    - `metadata` (JSON) - Additional provider-specific data
  - `priority` (integer) - Rule matching priority (higher wins)
  - `enabled` (boolean)
- `reverseTransform` (boolean) - Whether to apply reverse transformation for drift detection
- `conflictResolution` (enum) - "UNION" (user gets all matching entitlements), "FIRST_MATCH" (first rule wins), "MANUAL"
- `enabled` (boolean) - Whether rule is active
- `createdBy` (string) - User/service that created rule
- `createdAt` (dateTime)
- `lastModifiedBy` (string)
- `lastModifiedAt` (dateTime)

**Database Representation** (Cosmos DB):
```json
{
  "id": "tr-001-group-to-salesforce-role",
  "type": "TransformationRule",
  "tenantId": "entra-tenant-id",
  "providerId": "salesforce",
  "name": "Map Entra ID Groups to Salesforce Roles",
  "sourceType": "SCIM_GROUP",
  "targetType": "ROLE",
  "rules": [
    {
      "sourcePattern": "Sales-.*",
      "targetMapping": {
        "entitlementId": "salesforce-role-sales",
        "entitlementName": "Sales Role",
        "metadata": {"permissionSet": "perm-sales"}
      },
      "priority": 10,
      "enabled": true
    },
    {
      "sourcePattern": "Engineering-.*",
      "targetMapping": {
        "entitlementId": "salesforce-role-eng",
        "entitlementName": "Engineering Role",
        "metadata": {"permissionSet": "perm-eng"}
      },
      "priority": 10,
      "enabled": true
    }
  ],
  "reverseTransform": true,
  "conflictResolution": "UNION",
  "enabled": true,
  "createdBy": "admin@company.com",
  "createdAt": "2025-11-22T09:00:00Z"
}
```

---

### Sync State

**Attributes**:
- `id` (string) - UUID (format: "syncstate-{tenantId}-{providerId}")
- `tenantId` (string) - Entra ID tenant
- `providerId` (string) - Adapter/provider identifier
- `lastSyncTimestamp` (dateTime) - When last sync completed
- `lastSyncDuration` (integer) - Duration of last sync in milliseconds
- `lastSyncStatus` (enum) - "SUCCESS", "PARTIAL_FAILURE", "FAILED", "IN_PROGRESS"
- `lastKnownState` (object) - Snapshot of provider state at last sync:
  - `userCount` (integer) - Number of users
  - `groupCount` (integer) - Number of groups
  - `usersHash` (string) - Hash of all user data for quick comparison
  - `groupsHash` (string) - Hash of all group data
  - `timestamp` (dateTime) - When snapshot was taken
- `syncDirection` (enum) - "ENTRA_TO_SAAS" or "SAAS_TO_ENTRA"
- `driftLog` (array) - Recent drift detections:
  - `timestamp` (dateTime)
  - `resourceType` (string) - "User" or "Group"
  - `resourceId` (string)
  - `driftType` (enum) - "ADDED", "MODIFIED", "DELETED", "CONFLICT"
  - `oldValue` (JSON) - Previous state
  - `newValue` (JSON) - Current state
  - `reconciliationStatus` (enum) - "PENDING", "RECONCILED", "IGNORED", "MANUAL_REVIEW"
  - `reconciliationAction` (string) - What was done to resolve drift
- `conflictLog` (array) - Unresolved conflicts:
  - `resourceType` (string)
  - `resourceId` (string)
  - `conflictType` (enum) - "DUAL_MODIFY" (both sides changed), "DELETE_MODIFY" (deleted on one side, modified on other)
  - `entraValue` (JSON) - Entra ID state
  - `providerValue` (JSON) - Provider state
  - `reportedAt` (dateTime)
  - `status` (enum) - "PENDING", "REVIEWED", "RESOLVED"
  - `resolution` (string) - How conflict was resolved
- `nextSyncScheduledAt` (dateTime) - When next sync will run
- `retryCount` (integer) - Number of retries for this sync cycle
- `errorLog` (array) - Recent errors:
  - `timestamp` (dateTime)
  - `errorCode` (string)
  - `errorMessage` (string)
  - `context` (JSON) - Error context (which user/group, which operation)

**Database Representation** (Cosmos DB):
```json
{
  "id": "syncstate-tenant-123-salesforce",
  "type": "SyncState",
  "tenantId": "entra-tenant-id",
  "providerId": "salesforce",
  "lastSyncTimestamp": "2025-11-22T14:30:00Z",
  "lastSyncDuration": 45000,
  "lastSyncStatus": "SUCCESS",
  "lastKnownState": {
    "userCount": 1250,
    "groupCount": 45,
    "usersHash": "sha256-abc123...",
    "groupsHash": "sha256-def456...",
    "timestamp": "2025-11-22T14:30:00Z"
  },
  "syncDirection": "ENTRA_TO_SAAS",
  "driftLog": [
    {
      "timestamp": "2025-11-22T14:35:00Z",
      "resourceType": "User",
      "resourceId": "user-uuid-123",
      "driftType": "MODIFIED",
      "oldValue": {"phoneNumbers": [{"value": "+1-555-0100"}]},
      "newValue": {"phoneNumbers": [{"value": "+1-555-0101"}]},
      "reconciliationStatus": "PENDING"
    }
  ],
  "conflictLog": [],
  "nextSyncScheduledAt": "2025-11-22T15:00:00Z",
  "retryCount": 0,
  "errorLog": []
}
```

---

### Audit Log Entry

**Attributes** (stored in Application Insights):
- `timestamp` (dateTime) - When operation occurred
- `tenantId` (string) - Entra ID tenant
- `actorId` (string) - Service principal or user who triggered operation
- `actorType` (enum) - "SERVICE_PRINCIPAL", "USER", "SYSTEM"
- `operationType` (enum) - "CREATE", "READ", "UPDATE", "DELETE", "PATCH", "SYNC", "CONFIG_CHANGE"
- `resourceType` (enum) - "User", "Group", "Entitlement", "TransformationRule", "SyncState", "Configuration"
- `resourceId` (string) - ID of resource being operated on
- `adapterId` (string) - Which adapter performed the operation (if applicable)
- `oldValue` (JSON, redacted) - Previous state (for UPDATE, PATCH, DELETE)
- `newValue` (JSON, redacted) - New state (for CREATE, UPDATE, PATCH)
- `httpMethod` (string) - "GET", "POST", "PATCH", "PUT", "DELETE"
- `httpStatus` (integer) - HTTP response status code
- `responseTime` (integer) - Milliseconds
- `sourceIp` (string) - Client IP address
- `userAgent` (string) - Client user agent
- `errorCode` (string) - If operation failed
- `errorMessage` (string, redacted) - If operation failed
- `context` (JSON) - Additional context (e.g., sync direction at time of operation, drift type)

**Application Insights KQL Query Example**:
```kusto
customEvents
| where name == "SCIMOperation"
| where tostring(customDimensions.resourceType) == "User"
| where tostring(customDimensions.operationType) == "CREATE"
| summarize count() by bin(timestamp, 1h), tostring(customDimensions.adapterId)
```

---

## Relationships & Constraints

```
SCIM User
  ├─ tenantId → Tenant (soft FK)
  ├─ adapterId → Adapter Configuration
  ├─ groups[] → SCIM Group (M:N relationship)
  ├─ syncState → Sync State (1:1 mapping per user during sync)
  └─ lastSyncDirection → determines if updates applied

SCIM Group
  ├─ tenantId → Tenant (soft FK)
  ├─ adapterId → Adapter Configuration
  ├─ members[] → SCIM User (M:N relationship)
  ├─ entitlementMapping[] → Entitlement
  └─ syncState → Sync State

Entitlement
  ├─ providerId → Adapter Configuration
  ├─ mappedGroups[] → SCIM Group (M:N relationship via transformation rules)
  └─ parentEntitlementId → Entitlement (for hierarchies)

Transformation Rule
  ├─ tenantId → Tenant (soft FK)
  ├─ providerId → Adapter Configuration
  ├─ rules[] → Entitlement (target mappings)
  └─ applied during group operations

Sync State
  ├─ tenantId → Tenant (soft FK)
  ├─ providerId → Adapter Configuration
  ├─ driftLog[] → detected changes to users/groups
  ├─ conflictLog[] → unresolved conflicts
  └─ errorLog[] → recent errors
```

## Partitioning Strategy (Cosmos DB)

**Partition Key**: `/tenantId`

Rationale: All queries scoped to tenant, enables multi-tenancy, high fan-out workloads per tenant are isolated

**Secondary Indexes**:
- `/adapterId` - Query users/groups by adapter
- `/syncState` - Query resources by sync state
- `/lastSyncTimestamp` - Query resources modified since last sync
- `/resourceType` - Query by resource type (User vs Group)

---

## Versioning & Concurrency

**Optimistic Concurrency** (per SCIM spec):
- `meta.version` attribute tracks resource version (e.g., "W/\"3\"")
- On update, client must provide current version
- SDK compares versions; if mismatch, returns 409 Conflict
- Adapter responsible for version management (or SDK can auto-increment)

**Conflict Resolution**:
- If both Entra ID and provider modify same user simultaneously, marked as "CONFLICT" in sync state
- Conflict log captures both versions
- Reconciliation policy determines resolution (first-wins, auto-reconcile, manual review)

---

**Version**: 1.0.0 | **Date**: 2025-11-22 | **Phase**: Design
