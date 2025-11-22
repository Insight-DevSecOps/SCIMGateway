# Cosmos DB Database Schema Design

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22

---

## 1. Overview

This document defines the Cosmos DB schema design for the SCIM Gateway, including container structure, partition strategy, indexing policy, and data access patterns.

### Database Name

```
scim-gateway-db
```

### API Choice

**Azure Cosmos DB for NoSQL** (formerly SQL API)

**Rationale**:
- Native JSON document support
- Flexible schema (SCIM attributes vary per provider)
- SQL-like query language (familiar, powerful)
- Automatic indexing
- Built-in support for TTL (Time-To-Live)
- Change feed for drift detection
- Multi-region replication support

---

## 2. Container Design

### 2.1 Container: `sync-state`

**Purpose**: Track sync state per tenant and provider for drift detection.

**Partition Key**: `/tenantId`

**Schema**:
```json
{
  "id": "tenant-123:salesforce-prod",
  "tenantId": "tenant-123",
  "providerId": "salesforce-prod",
  "providerName": "Salesforce",
  "lastSyncTimestamp": "2025-11-22T10:00:00.000Z",
  "syncDirection": "ENTRA_TO_SAAS",
  "lastKnownState": {
    "userCount": 1523,
    "groupCount": 45,
    "snapshotChecksum": "sha256:abc123...",
    "snapshotTimestamp": "2025-11-22T10:00:00.000Z"
  },
  "driftLog": [
    {
      "driftId": "drift-001",
      "timestamp": "2025-11-22T11:00:00.000Z",
      "driftType": "USER_ADDED_ON_PROVIDER",
      "resourceType": "USER",
      "resourceId": "user-789",
      "details": {
        "userName": "new.user@example.com",
        "action": "PROVISION_TO_ENTRA"
      },
      "reconciled": false
    }
  ],
  "conflictLog": [
    {
      "conflictId": "conflict-001",
      "timestamp": "2025-11-22T11:15:00.000Z",
      "conflictType": "DUAL_MODIFICATION",
      "resourceType": "USER",
      "resourceId": "user-456",
      "details": {
        "entraChange": "role changed to Manager",
        "providerChange": "role changed to Rep",
        "resolution": "MANUAL_REVIEW_REQUIRED"
      },
      "resolved": false
    }
  ],
  "errorLog": [
    {
      "errorId": "error-001",
      "timestamp": "2025-11-22T12:00:00.000Z",
      "errorCode": "RATE_LIMIT_EXCEEDED",
      "message": "Salesforce API rate limit (429)",
      "retryCount": 3,
      "resolved": true
    }
  ],
  "_ts": 1700000000,
  "ttl": -1
}
```

**Indexing Policy**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {"path": "/tenantId/?"},
    {"path": "/providerId/?"},
    {"path": "/lastSyncTimestamp/?"},
    {"path": "/syncDirection/?"},
    {"path": "/driftLog/*/timestamp/?"},
    {"path": "/driftLog/*/reconciled/?"}
  ],
  "excludedPaths": [
    {"path": "/lastKnownState/*"}
  ]
}
```

**Access Patterns**:
- Query by `tenantId` (partition key query - most efficient)
- Query by `providerId` within tenant
- Query unreconciled drift items: `SELECT * FROM c WHERE c.tenantId = @tenantId AND ARRAY_LENGTH(c.driftLog) > 0`
- Query sync direction: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.syncDirection = 'ENTRA_TO_SAAS'`

---

### 2.2 Container: `users`

**Purpose**: Store SCIM User entities with provider-specific mappings.

**Partition Key**: `/tenantId`

**Schema**:
```json
{
  "id": "2819c223-7f76-453a-919d-413861904646",
  "tenantId": "tenant-123",
  "externalId": "bjensen",
  "userName": "bjensen@example.com",
  "name": {
    "familyName": "Jensen",
    "givenName": "Barbara",
    "formatted": "Barbara Jensen"
  },
  "displayName": "Barbara Jensen",
  "emails": [
    {
      "value": "bjensen@example.com",
      "type": "work",
      "primary": true
    }
  ],
  "phoneNumbers": [
    {
      "value": "+1-555-0123",
      "type": "work",
      "primary": true
    }
  ],
  "active": true,
  "groups": [
    {
      "value": "e9e30844-f356-4344-b7c1-b9602b88e457",
      "display": "Sales Team",
      "$ref": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457"
    }
  ],
  "roles": [
    {
      "value": "Sales_Representative",
      "display": "Sales Representative",
      "type": "SCIM_ROLE"
    }
  ],
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "employeeNumber": "701984",
    "costCenter": "4130",
    "organization": "Sales",
    "department": "EMEA",
    "manager": {
      "value": "26118915-6090-4610-87e4-49d8ca9f808d",
      "displayName": "Jane Manager"
    }
  },
  "meta": {
    "resourceType": "User",
    "created": "2025-11-22T10:00:00.000Z",
    "lastModified": "2025-11-22T10:00:00.000Z",
    "location": "/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"1\""
  },
  "_providerMappings": {
    "salesforce-prod": {
      "providerUserId": "0051t000000IZ3fAAG",
      "providerUserName": "bjensen@example.com",
      "syncState": "SYNCED",
      "lastSyncTimestamp": "2025-11-22T10:00:00.000Z",
      "roleAssignments": [
        {
          "roleId": "00E1t000000IfaxAAC",
          "roleName": "Sales_Representative"
        }
      ]
    },
    "servicenow-prod": {
      "providerUserId": "sys_user_12345",
      "syncState": "SYNCED",
      "lastSyncTimestamp": "2025-11-22T10:00:00.000Z"
    }
  },
  "_conflictLog": [
    {
      "timestamp": "2025-11-22T11:00:00.000Z",
      "conflictType": "EMAIL_CHANGED_IN_MULTIPLE_PLACES",
      "oldValue": "bjensen@example.com",
      "newValueEntra": "bjensen.new@example.com",
      "newValueProvider": "bjensen.alt@example.com",
      "resolution": "ENTRA_WINS"
    }
  ],
  "_ts": 1700000000,
  "ttl": -1
}
```

**Indexing Policy**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {"path": "/tenantId/?"},
    {"path": "/userName/?"},
    {"path": "/externalId/?"},
    {"path": "/active/?"},
    {"path": "/emails/*/value/?"},
    {"path": "/meta/lastModified/?"},
    {"path": "/_providerMappings/*/providerUserId/?"},
    {"path": "/_providerMappings/*/syncState/?"}
  ],
  "excludedPaths": [
    {"path": "/_conflictLog/*"}
  ]
}
```

**Composite Indexes**:
```json
{
  "compositeIndexes": [
    [
      {"path": "/tenantId", "order": "ascending"},
      {"path": "/userName", "order": "ascending"}
    ],
    [
      {"path": "/tenantId", "order": "ascending"},
      {"path": "/active", "order": "ascending"},
      {"path": "/meta/lastModified", "order": "descending"}
    ]
  ]
}
```

**Access Patterns**:
- Get user by ID (point read - most efficient): `SELECT * FROM c WHERE c.id = @id AND c.tenantId = @tenantId`
- Get user by userName: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.userName = @userName`
- Get user by externalId: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.externalId = @externalId`
- List users by active status: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.active = true`
- List users by sync state for provider: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c._providerMappings['salesforce-prod'].syncState = 'SYNCED'`

---

### 2.3 Container: `groups`

**Purpose**: Store SCIM Group entities with provider-specific mappings.

**Partition Key**: `/tenantId`

**Schema**:
```json
{
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "tenantId": "tenant-123",
  "externalId": "sales-team",
  "displayName": "Sales Team",
  "description": "EMEA Sales Organization",
  "members": [
    {
      "value": "2819c223-7f76-453a-919d-413861904646",
      "display": "Barbara Jensen",
      "$ref": "/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
      "type": "User"
    },
    {
      "value": "a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "display": "John Smith",
      "$ref": "/scim/v2/Users/a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "type": "User"
    }
  ],
  "meta": {
    "resourceType": "Group",
    "created": "2025-11-22T10:00:00.000Z",
    "lastModified": "2025-11-22T10:00:00.000Z",
    "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"1\""
  },
  "_providerMappings": {
    "salesforce-prod": {
      "providerGroupId": "00G1t000000IfaxAAC",
      "providerGroupName": "Sales_Representative",
      "mappingType": "SALESFORCE_ROLE",
      "syncState": "SYNCED",
      "lastSyncTimestamp": "2025-11-22T10:00:00.000Z",
      "memberCount": 2
    },
    "workday-prod": {
      "providerGroupId": "ORG-0150",
      "providerGroupName": "EMEA Sales",
      "mappingType": "WORKDAY_ORG",
      "orgLevel": 3,
      "syncState": "SYNCED",
      "lastSyncTimestamp": "2025-11-22T10:00:00.000Z"
    }
  },
  "_entitlementMapping": {
    "transformationRuleId": "rule-001",
    "sourcePattern": "^Sales.*$",
    "targetType": "SALESFORCE_ROLE",
    "targetMapping": "Sales_Representative",
    "priority": 1,
    "enabled": true
  },
  "_ts": 1700000000,
  "ttl": -1
}
```

**Indexing Policy**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {"path": "/tenantId/?"},
    {"path": "/displayName/?"},
    {"path": "/externalId/?"},
    {"path": "/members/*/value/?"},
    {"path": "/_providerMappings/*/providerGroupId/?"},
    {"path": "/_providerMappings/*/syncState/?"}
  ]
}
```

**Access Patterns**:
- Get group by ID: `SELECT * FROM c WHERE c.id = @id AND c.tenantId = @tenantId`
- Get group by displayName: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.displayName = @displayName`
- List groups with user membership: `SELECT * FROM c WHERE c.tenantId = @tenantId AND ARRAY_CONTAINS(c.members, {'value': @userId}, true)`

---

### 2.4 Container: `transformation-rules`

**Purpose**: Store transformation rules for group → entitlement mapping.

**Partition Key**: `/tenantId`

**Schema**:
```json
{
  "id": "rule-001",
  "tenantId": "tenant-123",
  "providerId": "salesforce-prod",
  "providerName": "Salesforce",
  "ruleType": "REGEX",
  "sourcePattern": "^Sales-(.*)$",
  "sourceType": "SCIM_GROUP",
  "targetType": "SALESFORCE_ROLE",
  "targetMapping": "Sales_${1}_Rep",
  "priority": 1,
  "enabled": true,
  "conflictResolution": "UNION",
  "examples": [
    {
      "input": "Sales-APAC",
      "expectedOutput": "Sales_APAC_Rep"
    },
    {
      "input": "Sales-EMEA",
      "expectedOutput": "Sales_EMEA_Rep"
    }
  ],
  "createdAt": "2025-11-22T10:00:00.000Z",
  "updatedAt": "2025-11-22T10:00:00.000Z",
  "createdBy": "admin@example.com",
  "_ts": 1700000000,
  "ttl": -1
}
```

**Indexing Policy**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {"path": "/tenantId/?"},
    {"path": "/providerId/?"},
    {"path": "/enabled/?"},
    {"path": "/priority/?"}
  ]
}
```

**Access Patterns**:
- Get all rules for provider: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.providerId = @providerId AND c.enabled = true ORDER BY c.priority ASC`

---

### 2.5 Container: `audit-logs`

**Purpose**: Store audit trail for all CRUD operations (backup to Application Insights).

**Partition Key**: `/tenantId`

**TTL**: 90 days (2592000 seconds)

**Schema**:
```json
{
  "id": "log-12345",
  "tenantId": "tenant-123",
  "timestamp": "2025-11-22T10:00:00.000Z",
  "actorId": "service-principal-456",
  "actorType": "SERVICE_PRINCIPAL",
  "operationType": "CREATE_USER",
  "resourceType": "USER",
  "resourceId": "2819c223-7f76-453a-919d-413861904646",
  "userName": "b***n@example.com",
  "httpStatus": 201,
  "responseTimeMs": 1250,
  "requestId": "req-789",
  "adapterId": "salesforce-prod",
  "oldValue": null,
  "newValue": {
    "userName": "b***n@example.com",
    "active": true,
    "displayName": "Barbara J***n"
  },
  "errorCode": null,
  "errorMessage": null,
  "_ts": 1700000000,
  "ttl": 7776000
}
```

**Indexing Policy**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {"path": "/tenantId/?"},
    {"path": "/timestamp/?"},
    {"path": "/operationType/?"},
    {"path": "/resourceType/?"},
    {"path": "/httpStatus/?"}
  ],
  "excludedPaths": [
    {"path": "/oldValue/*"},
    {"path": "/newValue/*"}
  ]
}
```

**Access Patterns**:
- Query logs by tenant and time range: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.timestamp >= @startTime AND c.timestamp <= @endTime`
- Query logs by operation type: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.operationType = 'CREATE_USER'`
- Query error logs: `SELECT * FROM c WHERE c.tenantId = @tenantId AND c.httpStatus >= 400`

---

## 3. Partition Strategy

### 3.1 Rationale for `/tenantId` Partition Key

**Why `/tenantId`?**
1. **Tenant Isolation**: All queries naturally scoped to tenant (security boundary)
2. **Even Distribution**: Tenants have similar workload patterns
3. **Query Efficiency**: Most queries include tenantId filter (partition key query)
4. **Scale-Out**: Each tenant can grow independently without hot partitions
5. **Multi-Region**: Tenants can be distributed across regions if needed

**Partition Key Cardinality**:
- Expected: 100-1000 tenants (high cardinality)
- Each partition (tenant) size: ~100MB - 1GB (well within 20GB limit)

### 3.2 Hierarchical Partition Keys (HPK)

**Not Required**: Single-level `/tenantId` is sufficient for current scale.

**Future Consideration**: If single tenant exceeds 20GB, use HPK:
```
/tenantId/providerId
```

This would allow:
- Tenant-123 with Salesforce data in one partition
- Tenant-123 with Workday data in another partition

---

## 4. Indexing Strategy

### 4.1 Automatic Indexing

Cosmos DB automatically indexes all properties by default. We selectively exclude:
- Large nested objects (e.g., `lastKnownState`)
- PII-sensitive fields (e.g., full email addresses)
- Conflict logs (only queried rarely)

### 4.2 Composite Indexes

For common query patterns with multiple filters:

**Example**: List active users sorted by last modified date
```sql
SELECT * FROM c 
WHERE c.tenantId = 'tenant-123' 
  AND c.active = true 
ORDER BY c.meta.lastModified DESC
```

**Composite Index**:
```json
[
  {"path": "/tenantId", "order": "ascending"},
  {"path": "/active", "order": "ascending"},
  {"path": "/meta/lastModified", "order": "descending"}
]
```

---

## 5. Consistency Level

**Recommended**: **Session Consistency**

**Rationale**:
- Guarantees read-your-writes within same session (critical for SCIM)
- Better performance than Strong consistency
- Acceptable for user provisioning (not life-critical)

**Alternative**: **Bounded Staleness** (for multi-region with 99.999% SLA)

---

## 6. Throughput (RU/s) Provisioning

### 6.1 Provisioning Model

**Autoscale** (Recommended)

**Rationale**:
- SCIM workload is bursty (bulk provisioning spikes)
- Autoscale adapts to load automatically
- Cost-effective during low-traffic periods

### 6.2 Estimated RU/s per Container

| Container | RU/s (Min) | RU/s (Max) | Rationale |
|-----------|------------|------------|-----------|
| `sync-state` | 400 | 4000 | Frequent reads/writes during drift detection |
| `users` | 1000 | 10000 | High read/write volume (CRUD operations) |
| `groups` | 400 | 4000 | Moderate read/write volume |
| `transformation-rules` | 100 | 1000 | Mostly reads (writes on config changes) |
| `audit-logs` | 400 | 4000 | High write volume (all operations logged) |

**Total**: 2,300 RU/s min → 23,000 RU/s max

**Cost Estimate** (West US 2):
- Min: ~$140/month
- Max: ~$1,400/month (only during spikes)

---

## 7. Time-To-Live (TTL)

### 7.1 TTL per Container

| Container | TTL | Rationale |
|-----------|-----|-----------|
| `sync-state` | -1 (never expire) | Need full history for drift analysis |
| `users` | -1 (never expire) | User data persists until explicitly deleted |
| `groups` | -1 (never expire) | Group data persists |
| `transformation-rules` | -1 (never expire) | Rules persist until deleted |
| `audit-logs` | 90 days (7776000 sec) | Compliance requirement: 90-day retention |

### 7.2 TTL Implementation

**Enable TTL at container level**:
```json
{
  "defaultTtl": -1
}
```

**Per-item TTL** (audit logs):
```json
{
  "id": "log-12345",
  "ttl": 7776000
}
```

After 90 days, Cosmos DB automatically deletes audit log items.

---

## 8. Multi-Region Configuration

### 8.1 Recommended Regions

**Primary Region**: West US 2  
**Secondary Regions**: East US 2, North Europe

**Rationale**:
- Geographic distribution for global customers
- Automatic failover for high availability
- Lower latency for distributed users

### 8.2 Multi-Region Writes

**Not Required**: Single-region writes sufficient for SCIM.

**Rationale**:
- SCIM provisioning is tenant-scoped (not global)
- Conflict resolution overhead not needed
- Cost savings (multi-region writes 2x cost)

---

## 9. Security

### 9.1 Authentication

**Azure Managed Identity** (System-Assigned)

**Rationale**:
- No connection strings in code
- Automatic credential rotation
- RBAC integration

### 9.2 RBAC Roles

| Role | Permission | Use Case |
|------|------------|----------|
| Cosmos DB Data Contributor | Read/Write/Delete | Application runtime |
| Cosmos DB Data Reader | Read-only | Monitoring/reporting |
| Cosmos DB Account Reader | Metadata only | DevOps |

### 9.3 Network Security

**Private Endpoint** (Recommended)

**Rationale**:
- No public internet exposure
- Traffic stays on Azure backbone
- Reduced attack surface

**Alternative**: Firewall rules (allow Azure services + specific IPs)

---

## 10. Backup & Disaster Recovery

### 10.1 Backup Policy

**Continuous Backup** (Recommended)

**Rationale**:
- Point-in-time restore (PITR) up to 30 days
- Accidental deletion recovery
- Compliance requirement

**Alternative**: Periodic backup (every 4 hours, retention 30 days)

### 10.2 Disaster Recovery

**RPO**: < 5 minutes (multi-region replication)  
**RTO**: < 1 hour (automatic failover)

---

## 11. Monitoring & Alerts

### 11.1 Key Metrics

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| RU consumption | > 80% of max | Scale up autoscale max |
| Throttling (429) | > 10 req/min | Investigate query patterns |
| Latency (p99) | > 5 seconds | Check indexing policy |
| Availability | < 99.9% | Check region health |

### 11.2 Diagnostic Logs

Enable diagnostic logs for:
- DataPlaneRequests (all CRUD operations)
- QueryRuntimeStatistics (query performance)
- PartitionKeyStatistics (hot partition detection)

**Send To**: Log Analytics Workspace (for KQL queries)

---

## 12. Query Examples

### 12.1 Get User by userName

```sql
SELECT * 
FROM c 
WHERE c.tenantId = 'tenant-123' 
  AND c.userName = 'bjensen@example.com'
```

**RU Cost**: ~3 RUs (indexed query)

### 12.2 List Active Users Modified After Date

```sql
SELECT c.id, c.userName, c.displayName, c.meta.lastModified
FROM c 
WHERE c.tenantId = 'tenant-123' 
  AND c.active = true 
  AND c.meta.lastModified > '2025-11-22T00:00:00.000Z'
ORDER BY c.meta.lastModified DESC
```

**RU Cost**: ~10-50 RUs (depends on result count)

### 12.3 Find Users with Unreconciled Drift

```sql
SELECT c.id, c.userName, c._providerMappings
FROM c 
WHERE c.tenantId = 'tenant-123' 
  AND c._providerMappings['salesforce-prod'].syncState = 'DRIFT_DETECTED'
```

**RU Cost**: ~20-100 RUs (depends on result count)

### 12.4 Get Groups with Specific User

```sql
SELECT c.id, c.displayName, c.members
FROM c 
WHERE c.tenantId = 'tenant-123' 
  AND ARRAY_CONTAINS(c.members, {'value': 'user-id-123'}, true)
```

**RU Cost**: ~10-30 RUs

---

## 13. Migration Strategy

### 13.1 Initial Setup

1. Create Cosmos DB account (serverless or autoscale)
2. Create database: `scim-gateway-db`
3. Create containers with partition keys and indexing policies
4. Configure TTL settings
5. Enable diagnostic logs
6. Configure backup policy
7. Set up monitoring alerts

### 13.2 Data Seeding

1. Import transformation rules from JSON config
2. Create initial sync state records per tenant/provider
3. No user/group data (populated via SCIM API)

---

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
