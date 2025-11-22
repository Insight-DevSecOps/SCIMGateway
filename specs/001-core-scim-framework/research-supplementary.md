# Phase 0 Supplementary Research: Edge Cases, Provider Deep Dives, Transformation Patterns

**Objective**: Address complex scenarios, provider-specific quirks, and transformation edge cases not covered in primary research  
**Duration**: Week 1-2 extended  
**Output**: Comprehensive knowledge base for Phase 1 design decisions

---

## Part 1: SCIM Attribute Mapping Edge Cases

### 1.1 Complex Attribute Types

**Multi-valued Attributes with Type Discriminators**

SCIM allows multi-valued attributes with type indicators. Example: multiple emails (work, home, other).

```json
{
  "emails": [
    {
      "value": "bjensen@example.com",
      "type": "work",
      "primary": true
    },
    {
      "value": "bjensen.home@example.com",
      "type": "home",
      "primary": false
    }
  ]
}
```

**Challenge**: Providers have different email models
- Salesforce: `Email` (single) + `Phone` + custom fields
- Workday: Multiple email addresses with type (WORK, PERSONAL)
- ServiceNow: Single email + additional phone/mobile fields

**Mapping Strategy**:
```
SCIM emails[type=work] → Salesforce Email
SCIM emails[type=home] → Salesforce custom field (if available)
SCIM emails[primary=true] → Workday primary email
SCIM emails → ServiceNow email field (first work email)
```

---

### 1.2 Nested Object Mapping

**Manager Reference (Hierarchical Relationship)**

SCIM manager attribute references another user:
```json
{
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "manager": {
      "value": "26118915-6090-4610-87e4-49d8ca9f808d",
      "display": "John Smith",
      "$ref": "https://example.com/Users/26118915-6090-4610-87e4-49d8ca9f808d"
    }
  }
}
```

**Challenge**: Manager relationship handling per provider

| Provider | Manager Model | Mapping Approach |
|----------|---------------|-----------------|
| Salesforce | `ManagerId` field | Map SCIM manager.value → Salesforce ManagerId |
| Workday | Org hierarchy + manager link | Requires two-step: assign org, then set manager ID |
| ServiceNow | `Manager` field (user reference) | Direct mapping: SCIM manager.value → sys_user reference |

**Transformation Rules**:
1. **Resolve manager ID**: If SCIM provides `manager.value` (UUID), look up provider's manager identifier
2. **Handle missing manager**: If manager doesn't exist yet, queue as "pending relationship" (post-sync dependency)
3. **Detect circular references**: Prevent manager A → B → A cycles
4. **Reconcile on updates**: If manager changes, verify both users exist before applying

---

### 1.3 Custom Attribute Extensions

**Enterprise User Extension Attributes**

Standard enterprise extension defines:
```json
{
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "employeeNumber": "701984",
    "costCenter": "4130",
    "organization": "Sales",
    "department": "EMEA",
    "division": "Commercial",
    "manager": { /* reference */ }
  }
}
```

**Provider-Specific Custom Attributes**

Salesforce may define:
```json
{
  "urn:ietf:params:scim:schemas:extension:salesforce:2.0:User": {
    "salesforceId": "0051t000000IZ3f",
    "profileId": "00e1t000000IfaxAAC",
    "territory": "APAC-Sales-01",
    "salesRepRole": "Account Executive"
  }
}
```

**Transformation Challenge**: Which attributes to preserve?
- **Keep**: SCIM standard attributes + enterprise extension
- **Drop**: Provider-specific custom attributes (not portable)
- **Log**: Dropped attributes in drift detection

**Configuration**:
```json
{
  "attributeMappings": {
    "keep": [
      "urn:ietf:params:scim:schemas:core:2.0:User",
      "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
    ],
    "drop": [
      "urn:ietf:params:scim:schemas:extension:salesforce:2.0:User"
    ],
    "custom": {
      "urn:company:params:scim:schemas:extension:custom:2.0:User": {
        "costCenter": "department",
        "division": "organizationalUnit"
      }
    }
  }
}
```

---

## Part 2: Provider-Specific Edge Cases

### 2.1 Salesforce Specific Scenarios

#### Challenge 1: UserType vs Profile vs Role

Salesforce has three overlapping access control models:
- **Profile**: Security settings (what actions user can perform)
- **Role**: Org hierarchy (data visibility and delegation)
- **Permission Sets**: Additional permissions (not tied to role)

**Scenario**: SCIM group "Sales Manager" should map to:
- Salesforce Role: "Sales Manager" (org hierarchy)
- AND Salesforce Permission Set: "Sales Reports Access"
- AND Salesforce Profile: "Standard User"

**Complexity**: Profiles are predefined; can't create new ones. Must use existing profiles + permission sets for additional access.

**Transformation Rule**:
```json
{
  "sourcePattern": "Sales Manager",
  "targetType": "COMPOSITE",
  "targets": [
    {
      "type": "SALESFORCE_ROLE",
      "mapping": "Sales Manager",
      "action": "ASSIGN_OR_CREATE"
    },
    {
      "type": "SALESFORCE_PERMISSION_SET",
      "mapping": "Sales Reports Access",
      "action": "ASSIGN"
    },
    {
      "type": "SALESFORCE_PROFILE",
      "mapping": "Standard User",
      "action": "READONLY"
    }
  ]
}
```

#### Challenge 2: Salesforce User Provisioning Limits

Salesforce has strict provisioning limits per edition:
- **Developer Edition**: 2 admin + 1 standard user = 3 total users
- **Essentials**: 3 users + $50/user
- **Professional**: 100+ users
- **Enterprise**: Unlimited (license-based)

**Risk**: Provisioning users to Salesforce may fail if license limit reached.

**Mitigation Strategy**:
1. Query Salesforce org limits via API: `SELECT COUNT() FROM User WHERE IsActive = TRUE`
2. Compare against org's user limit (from Organization object)
3. If approaching limit, log warning + create alert
4. Fail provision operation with 429 (Too Many Requests) or 422 (Unprocessable Entity)
5. Recommend: "Contact Salesforce admin to increase user license"

#### Challenge 3: Salesforce Email Uniqueness

Salesforce enforces **email uniqueness at the org level** (not per-user).
- User1: john.smith@company.com
- User2: john.smith@company.com (FAILS - duplicate email)

**But**: Salesforce also allows inactive users. When reactivating a user, the email must be unique across active + inactive users.

**Risk Scenario**:
1. Delete User1 (john.smith@company.com) - marked as inactive
2. Create User2 (john.smith@company.com) - FAILS because inactive User1 still owns that email

**Mitigation**:
1. Before creating user with email X, check if email exists in Salesforce (active + inactive)
2. If email exists (inactive), either:
   - Reactivate + update existing user, OR
   - Generate unique email (john.smith+2@company.com)
3. Log the email resolution for audit trail

---

### 2.2 Workday Specific Scenarios

#### Challenge 1: Org Hierarchy Immutability

Workday organization hierarchy is **deeply nested and immutable** (cannot delete, rename org nodes).

**Typical Structure**:
```
Company (1 level)
  ├── Division (2 level)
  │   ├── Department (3 level)
  │   │   └── Team (4 level)
```

**Scenario**: SCIM group "APAC Sales" needs to map to Workday org level.
- Problem: Workday only has 4 levels; can't create arbitrary groups
- Solution: Must map SCIM group to existing org node (e.g., Department level)

**Transformation Challenge**:
1. SCIM group "APAC Sales" → Look up existing Workday org with name "APAC Sales"
2. If not found → Cannot create (orgs are immutable)
3. Recommendation: Map to closest existing org in hierarchy

**Configuration**:
```json
{
  "providerName": "Workday",
  "groupMappingStrategy": "ORG_HIERARCHY",
  "orgLevelMapping": {
    "1": "COMPANY",
    "2": "DIVISION",
    "3": "DEPARTMENT",
    "4": "TEAM"
  },
  "transformationRules": [
    {
      "sourcePattern": "APAC.*",
      "targetType": "WORKDAY_ORG",
      "targetLevel": 2,
      "lookupField": "externalId",
      "action": "LOOKUP_OR_IGNORE"
    }
  ]
}
```

#### Challenge 2: Workday Worker ID Format

Workday Worker IDs have specific format requirements:
- Internal ID: UUID (e.g., "8f3cf61d6b7d11ecada0123456789abc")
- External ID: Customer-defined (e.g., "EMP-12345")

**SCIM Mapping**:
```
SCIM externalId → Workday externalId
SCIM id → Workday internalId (read-only after creation)
```

**Transformation Rule**:
```json
{
  "workdayWorkerIdSource": "externalId",
  "externalIdFormat": "EMP-{nnnnnn}",
  "validation": {
    "pattern": "^EMP-\\d{6}$",
    "required": true
  }
}
```

#### Challenge 3: Workday Contingent Workers

Workday distinguishes between:
- **Employee**: Full-time, permanent worker (managed by HR)
- **Contingent Worker**: Contractor, temp, vendor (managed differently)

**SCIM Mapping Challenge**:
- SCIM `userType` (standard attribute) can be "Employee", "Contractor", etc.
- Workday has separate `Worker` vs `Contingent Worker` objects with different fields

**Transformation**:
```json
{
  "userTypeMapping": {
    "Employee": "WORKER",
    "Contractor": "CONTINGENT_WORKER",
    "Customer": "EXTERNAL_USER"
  }
}
```

---

### 2.3 ServiceNow Specific Scenarios

#### Challenge 1: ServiceNow Table Extension Model

ServiceNow groups are stored in `sys_user_group` table. Groups can have:
- **Members** (users): `sys_user_group_member` table
- **Roles** (permissions): `sys_group_has_role` table
- **Custom fields**: Extensible per customer configuration

**SCIM Mapping**:
- SCIM Group `displayName` → ServiceNow group `name`
- SCIM Group `members` → ServiceNow group members via `sys_user_group_member`
- SCIM Group `description` → ServiceNow group `description`

**Challenge**: ServiceNow allows **recursive group membership** (group A can contain group B, which contains users).

**SCIM doesn't support** recursive group membership (RFC 7643). SCIM `members` are users only, not groups.

**Transformation Strategy**:
```json
{
  "groupMembershipStrategy": "FLAT_EXPANSION",
  "description": "Expand recursive groups to flat user list",
  "rules": [
    {
      "scimGroup": "Sales Team",
      "serviceNowGroup": "sales_team",
      "includeSubGroups": false,
      "action": "ADD_USERS_ONLY"
    }
  ]
}
```

#### Challenge 2: ServiceNow Inheritance Rules

ServiceNow groups have **inheritance rules** (e.g., "Project Lead role inherits from Team Member role").

**SCIM Mapping Challenge**: SCIM doesn't model inheritance. If SCIM group represents role inheritance, must flatten for ServiceNow.

**Transformation**:
1. Query ServiceNow for group `sales_team` and all inherited roles
2. Flatten role hierarchy into single permission set
3. Assign flattened permission set to user

#### Challenge 3: ServiceNow Change Management Integration

ServiceNow may have **Change Advisory Board (CAB)** approval required for user provisioning (especially admins).

**SCIM Mapping Challenge**: SCIM provisioning is synchronous; ServiceNow may require async approval workflow.

**Mitigation Strategy**:
```json
{
  "requiresApproval": {
    "userTypes": ["ADMIN", "MANAGER"],
    "groups": ["System Administrators", "Compliance Team"]
  },
  "workflowMapping": {
    "action": "CREATE_CHANGE_REQUEST",
    "changeType": "STANDARD_CHANGE",
    "assignmentGroup": "Change Management Team",
    "waitForApproval": true,
    "timeout": 86400
  }
}
```

---

## Part 3: Transformation Engine Patterns

### 3.1 Rule Matching Algorithms

#### Pattern 1: Exact Match

```json
{
  "type": "EXACT_MATCH",
  "sourcePattern": "Sales Team",
  "targetMapping": "SALESFORCE_ROLE:Sales_Representative"
}
```

**Use Case**: 1:1 group mapping when SCIM group name exactly matches provider group name.

#### Pattern 2: Regex Pattern Match

```json
{
  "type": "REGEX",
  "sourcePattern": "^Sales-(.*)$",
  "targetMapping": "SALESFORCE_ROLE:Sales_${1}",
  "caseSensitive": false
}
```

**Use Case**: Multiple groups matching pattern (Sales-Americas, Sales-EMEA) map to multiple roles.

#### Pattern 3: Hierarchical Match

```json
{
  "type": "HIERARCHICAL",
  "sourcePattern": "COMPANY/DIVISION/DEPARTMENT",
  "targetMapping": "WORKDAY_ORG:${2}/${3}",
  "delimiter": "/",
  "levels": {
    "1": "COMPANY",
    "2": "DIVISION",
    "3": "DEPARTMENT"
  }
}
```

**Use Case**: SCIM groups represent org hierarchy; map to Workday org levels.

#### Pattern 4: Composite Rules

```json
{
  "type": "COMPOSITE",
  "sourceCriteria": {
    "groupName": "Engineering.*",
    "department": "Research",
    "userType": "Employee"
  },
  "targetMappings": [
    {"type": "SALESFORCE_ROLE", "mapping": "Sr_Engineer"},
    {"type": "PERMISSION_SET", "mapping": "Code_Review_Access"},
    {"type": "PERMISSION_SET", "mapping": "Production_Deploy"}
  ]
}
```

**Use Case**: User matches multiple criteria → multiple entitlements assigned.

---

### 3.2 Conflict Resolution Strategies

#### Conflict: User in Multiple Groups (Multiple Roles)

**Scenario**:
- SCIM group "Sales" → Salesforce Role "Sales Rep"
- SCIM group "Managers" → Salesforce Role "Sales Manager"
- User is in both groups

**Strategy Options**:

**1. UNION Strategy** (most permissions):
- User gets Sales Rep + Sales Manager roles
- More permissive; users have all applicable permissions
- Risk: Privilege escalation if not careful

**2. FIRST_MATCH Strategy** (first rule):
- User gets Sales Manager role (managers group processed first)
- Less permissive; clearer precedence
- Risk: Silent permission loss if rule order changes

**3. HIGHEST_PRIVILEGE Strategy**:
- User gets Sales Manager role (higher in hierarchy)
- Requires defining role hierarchy
- Risk: Complex hierarchy maintenance

**4. MANUAL_REVIEW Strategy**:
- Flag conflict for admin review; don't apply automatically
- Safest but slower (requires human intervention)
- Risk: Operational overhead

**Configuration**:
```json
{
  "conflictResolution": {
    "multiGroupAssignment": "UNION",
    "priorityOrder": ["Managers", "Sales", "Support"],
    "fallback": "MANUAL_REVIEW"
  }
}
```

---

#### Conflict: Dual Modifications (Sync Direction)

**Scenario** (ENTRA_TO_SAAS + SAAS_TO_ENTRA):
- User in Entra created group assignment "Sales Team"
- Same user in Salesforce removed from "Sales Team" role (different direction)
- Sync encounters conflict: Should user be in role or not?

**Strategy Options**:

**1. IGNORE_CONFLICT** (current direction wins):
- ENTRA_TO_SAAS: Ignore Salesforce removals; push from Entra
- SAAS_TO_ENTRA: Ignore Entra changes; pull from Salesforce
- Ensures direction-specific consistency

**2. UNION** (both sides apply):
- User gets assignment from both Entra and Salesforce
- If either side says "yes", result is "yes"
- Risk: Privilege escalation

**3. MERGE_WITH_AUDIT** (apply both, log conflict):
- Apply both sides; log diff
- Admin reviews and resolves manually
- Safest for multi-direction sync

**Configuration**:
```json
{
  "syncDirection": "ENTRA_TO_SAAS",
  "conflictDetection": {
    "dualModifications": "MERGE_WITH_AUDIT",
    "deleteVsModify": "MANUAL_REVIEW",
    "auditLog": "Application Insights"
  }
}
```

---

### 3.3 Reverse Transformation (Provider → SCIM)

**Challenge**: When pulling from provider, map provider entitlements back to SCIM groups.

#### Reverse Transformation Example

**Salesforce → SCIM**:
```json
{
  "reverseMapping": [
    {
      "salesforceRole": "Sales_Representative",
      "scimGroup": "Sales Team",
      "bidirectional": true
    },
    {
      "salesforcePermissionSet": "Code_Review_Access",
      "scimGroup": "Engineering-Code-Review",
      "bidirectional": true
    }
  ]
}
```

**Process**:
1. Poll Salesforce for users' roles + permission sets
2. For each role/permission set, find matching SCIM group via reverse mapping
3. Add user to SCIM group
4. Compare with last known state → detect drift

---

## Part 4: Change Detection & Drift

### 4.1 Drift Detection Scenarios

#### Scenario 1: User Added on Provider (SAAS_TO_ENTRA drift)

**Sequence**:
1. Admin creates user directly in Salesforce (john.smith@company.com)
2. SCIMGateway polls Salesforce
3. User doesn't exist in Entra ID; drift detected

**Detection**:
```json
{
  "driftType": "ADDED_ON_PROVIDER",
  "resource": "USER",
  "providerId": "john.smith@company.com",
  "lastSyncTimestamp": "2025-11-22T10:00:00Z",
  "currentTimestamp": "2025-11-22T11:00:00Z",
  "action": "PROVISION_TO_ENTRA_OR_IGNORE"
}
```

**Reconciliation Options**:
- **SYNC_TO_ENTRA**: Create corresponding user in Entra ID
- **IGNORE**: Log but take no action (expected drift)
- **DELETE_FROM_PROVIDER**: Remove from provider (most aggressive)

#### Scenario 2: User Modified on Provider (Role Change)

**Sequence**:
1. User john.smith in Salesforce with role "Sales Rep"
2. Admin changes role to "Sales Manager" directly in Salesforce
3. SCIMGateway polls Salesforce; role differs from last known state

**Detection**:
```json
{
  "driftType": "MODIFIED_ON_PROVIDER",
  "resource": "USER",
  "field": "role",
  "providerId": "john.smith@company.com",
  "oldValue": "Sales_Representative",
  "newValue": "Sales_Manager",
  "lastSyncTimestamp": "2025-11-22T10:00:00Z",
  "currentTimestamp": "2025-11-22T11:00:00Z",
  "action": "SYNC_TO_ENTRA_OR_IGNORE"
}
```

#### Scenario 3: User Deleted on Provider (SAAS_TO_ENTRA drift)

**Sequence**:
1. User john.smith exists in both Entra and Salesforce
2. Admin deletes user from Salesforce
3. SCIMGateway polls Salesforce; user gone but still in Entra

**Detection**:
```json
{
  "driftType": "DELETED_ON_PROVIDER",
  "resource": "USER",
  "providerId": "john.smith@company.com",
  "entraId": "2819c223-7f76-453a-919d-413861904646",
  "lastSyncTimestamp": "2025-11-22T10:00:00Z",
  "currentTimestamp": "2025-11-22T11:00:00Z",
  "action": "DELETE_FROM_ENTRA_OR_IGNORE"
}
```

---

### 4.2 Conflict Resolution in Drift

#### Conflict: Delete vs Modify

**Scenario**:
- Last sync: User john.smith with role "Sales Rep"
- Two diverging changes:
  - Path A: User deleted from Salesforce
  - Path B: User's role changed to "Sales Manager" in Salesforce
- SCIMGateway detects user exists but with different role

**Detection**:
```json
{
  "conflictType": "DELETE_VS_MODIFY",
  "resource": "USER",
  "userEmail": "john.smith@company.com",
  "conflicts": [
    {"operation": "DELETE", "source": "unknown_deletion"},
    {"operation": "MODIFY", "source": "role_change"}
  ],
  "resolution": "MANUAL_REVIEW"
}
```

**Reconciliation Strategy**:
1. Check deletion timestamp vs modification timestamp
2. If deletion is more recent → Delete user
3. If modification is more recent → Update role
4. If timestamps conflict → Manual review required

---

## Part 5: Rate Limiting & Throttling

### 5.1 Provider API Rate Limits

Each SaaS provider has rate limits:

| Provider | Limit | Window | Retry-After |
|----------|-------|--------|-------------|
| Salesforce | 15,000 API calls | Per 24 hours | Varies |
| Salesforce (OAuth) | 1,000 req/sec | Per second | 60 sec |
| Workday | 100 req/sec | Per second | Not standardized |
| ServiceNow | 1 req/sec per user | Per second | 429 w/ Retry-After |

### 5.2 Backoff Strategies

#### Strategy 1: Linear Backoff

```
Retry 1: Wait 1 second
Retry 2: Wait 2 seconds
Retry 3: Wait 3 seconds
Retry 4: Give up
```

**Risk**: Doesn't work well under sustained load; stacking requests.

#### Strategy 2: Exponential Backoff with Jitter

```
Retry 1: Wait 1 + random(0-1) = 0.5-1.5 sec
Retry 2: Wait 2 + random(0-2) = 2-4 sec
Retry 3: Wait 4 + random(0-4) = 4-8 sec
Retry 4: Wait 8 + random(0-8) = 8-16 sec
Max wait: 60 sec
```

**Recommendation**: Use exponential backoff with jitter (RFC 6960).

#### Strategy 3: Token Bucket Algorithm

```
Bucket capacity: N requests
Refill rate: X requests/sec
Request arrives: Deduct 1 from bucket
If bucket empty: Reject (429) or queue

Per adapter, maintain separate bucket
```

**Implementation**:
```csharp
public class TokenBucket
{
    private double _tokens;
    private readonly double _capacity;
    private readonly double _refillRate; // tokens per second
    private DateTime _lastRefillTime;
    
    public bool TryConsume(int count = 1)
    {
        Refill();
        if (_tokens >= count)
        {
            _tokens -= count;
            return true;
        }
        return false;
    }
    
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefillTime).TotalSeconds;
        _tokens = Math.Min(_capacity, _tokens + (elapsed * _refillRate));
        _lastRefillTime = now;
    }
}
```

---

## Part 6: PII Redaction Patterns

### 6.1 Sensitive Attributes Requiring Redaction

| Attribute | Risk | Redaction Pattern |
|-----------|------|-------------------|
| `emails` | HIGH | `user@company.com` → `u***@company.com` |
| `phoneNumbers` | HIGH | `+1-555-0123` → `+1-***-0123` |
| `addresses` | MEDIUM | Full street → `[REDACTED]` |
| `name` | MEDIUM | `John Smith` → `J*** S***` |
| `externalId` | LOW | Often not sensitive; depends on content |
| `userName` | LOW | Usually email; treat as email |

### 6.2 Redaction Rules

**Rule 1: Keep structure, redact content**
```
Input:  {"value": "john.smith@company.com", "type": "work"}
Output: {"value": "j***h@company.com", "type": "work"}
```

**Rule 2: Keep prefix/suffix, redact middle**
```
Input:  +1-555-0123
Output: +1-***-0123
```

**Rule 3: Full redaction for sensitive fields**
```
Input:  123 Main Street, New York, NY 10001
Output: [REDACTED - Address]
```

### 6.3 Audit Logging with Redaction

**Configuration**:
```json
{
  "auditLogging": {
    "redactionRules": [
      {
        "fields": ["emails", "phoneNumbers"],
        "strategy": "PARTIAL_MASK"
      },
      {
        "fields": ["addresses", "ssn"],
        "strategy": "FULL_REDACT"
      }
    ],
    "errorResponses": {
      "piiFields": ["password", "secret", "token"],
      "strategy": "NEVER_INCLUDE"
    }
  }
}
```

---

## Part 7: Batch Operations & Bulk Provisioning

### 7.1 Bulk User Provisioning Scenarios

**Scenario**: Provision 10,000 users from Entra to Salesforce in one sync cycle.

**Challenge**: 
- Salesforce rate limits (15,000 API calls/day)
- Network timeouts on large operations
- Partial failures (some users succeed, some fail)

### 7.2 Batch Strategy

**Strategy 1: Sequential Batches**
```
Batch 1: Users 1-100 (sequential calls)
Batch 2: Users 101-200
...
Batch 10: Users 9901-10000

Total API calls: ~10,000 (within limit)
Duration: ~10 minutes (slow but reliable)
```

**Strategy 2: Parallel Batches with Backoff**
```
Parallel workers: 5
Batch 1 (Worker 1): Users 1-20 (parallel to Worker 2)
Batch 2 (Worker 2): Users 21-40 (parallel to Worker 1)
...

Total API calls: ~10,000
Duration: ~2 minutes (faster; risk of rate limit)
Backoff if 429: Retry with exponential backoff
```

**Strategy 3: Provider Bulk API (if available)**

Salesforce Bulk API 2.0:
```
POST /services/data/v60.0/jobs/ingest
{
  "operation": "insert",
  "object": "User"
}
PUT /services/data/v60.0/jobs/ingest/{jobId}/batches
{
  /* CSV data: 10,000 users */
}
```

**Benefit**: Single API call for bulk data; provider handles internal parallelization.
**Limitation**: Salesforce Bulk API only for certain objects (not all user operations).

### 7.3 Partial Failure Handling

**Scenario**: Batch of 100 users; 99 succeed, 1 fails (duplicate email).

**Handling Strategy**:
```json
{
  "batchOperation": {
    "totalRequested": 100,
    "successful": 99,
    "failed": 1,
    "failures": [
      {
        "userId": "user-87",
        "email": "john@company.com",
        "error": "DUPLICATE_EMAIL",
        "status": 409,
        "action": "RETRY_WITH_NEW_EMAIL or MANUAL_REVIEW"
      }
    ]
  }
}
```

**Retry Strategy**:
1. Log failure with context (email conflict)
2. For retryable errors (429, 503): Exponential backoff + retry
3. For non-retryable errors (422, 409): Log + flag for manual review
4. Continue processing remaining users (don't fail entire batch)

---

## Part 8: Testing Strategies per Provider

### 8.1 ServiceNow Contract Tests

**Test 1: Create User (POST /Users)**
```
Request:
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "userName": "jsmith@company.com",
  "name": {"familyName": "Smith", "givenName": "John"},
  "emails": [{"value": "jsmith@company.com", "type": "work"}],
  "active": true
}

Expected Response (201 Created):
{
  "id": "sys_user_id_12345",
  "userName": "jsmith@company.com",
  "meta": {
    "resourceType": "User",
    "created": "2025-11-22T10:00:00Z",
    "version": "W/\"1\""
  }
}
```

**Test 2: Add User to Group (PATCH /Groups/{id})**
```
Request:
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "add",
      "path": "members",
      "value": [{"value": "user-id-123"}]
    }
  ]
}

Expected Response (204 No Content or 200 OK)
```

---

### 8.2 Salesforce Contract Tests

**Test 1: User Creation with Role Assignment**
```
Request (POST /Users):
{
  "userName": "jsmith@company.com",
  "name": {"familyName": "Smith", "givenName": "John"}
}

Expected Response (201): User created, but role NOT automatically assigned

Follow-up (PATCH /Users/{id}):
Apply transformation: SCIM group → Salesforce role

Expected Response (200): Role assigned via separate permission operation
```

**Challenge**: Salesforce doesn't support role assignment via SCIM groups directly. Must use custom extension or multi-step process.

---

### 8.3 Workday Contract Tests

**Test 1: Worker Lookup (GET /Workers)**
```
Request:
GET /Workers?filter=externalId eq "EMP-12345"

Expected Response (200): Worker with org hierarchy info
```

**Test 2: Org Hierarchy Traversal**
```
Request:
GET /Organizations?filter=name eq "APAC Sales"

Expected Response (200): Organization details with parent org references
```

---

## Part 9: Provider API Authentication Details

### 9.1 Salesforce OAuth 2.0 Flow

**Step 1: Get Access Token**
```
POST /services/oauth2/token
  client_id: {consumer_key}
  client_secret: {consumer_secret}
  grant_type: password
  username: {salesforce_user}
  password: {salesforce_password}{security_token}
```

**Response**:
```json
{
  "access_token": "00D50000000IZ3E!AQcAQH0aWearHs...",
  "token_type": "Bearer",
  "instance_url": "https://company.salesforce.com",
  "id": "https://login.salesforce.com/id/00D50000000IZ3EEAWjjfjlksd",
  "issued_at": "1614556800000",
  "signature": "...",
  "scope": "full refresh_token"
}
```

**Step 2: Use Access Token**
```
GET /services/data/v60.0/sobjects/User
Authorization: Bearer {access_token}
```

**Token Expiry**: Default 2 hours; refresh token for long-lived sessions.

### 9.2 Workday OAuth 2.0 Flow

**Step 1: Get Bearer Token** (different from Salesforce)
```
POST /ccx/oauth2/token
  grant_type: client_credentials
  client_id: {app_key}
  client_secret: {app_secret}
```

**Response**:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

**Step 2: Use Token**
```
GET /ccx/service/customreport2/company/Worker_v2
Authorization: Bearer {access_token}
```

### 9.3 ServiceNow OAuth 2.0 Flow

**Step 1: Get Token via Entra ID** (ServiceNow configured with Entra app registration)
```
POST https://login.microsoftonline.com/tenant/oauth2/v2.0/token
  client_id: {app_id}
  client_secret: {app_secret}
  scope: {servicenow_app_uri}/.default
  grant_type: client_credentials
```

**Step 2: Use Token**
```
GET /api/now/table/sys_user
Authorization: Bearer {access_token}
```

---

## Part 10: Monitoring & Observability

### 10.1 Key Metrics per Provider

| Metric | Salesforce | Workday | ServiceNow |
|--------|-----------|---------|-----------|
| API Latency p95 | 2-5 sec | 3-8 sec | 1-3 sec |
| Success Rate | 95-99% | 90-95% | 98-99% |
| Rate Limit Hits | Rare | Common | Rare |
| Timeout Rate | <1% | 2-5% | <1% |

### 10.2 Alerts

**Alert 1: High Error Rate**
```
Condition: Error rate > 5% in last 5 minutes
Severity: WARNING
Action: Page on-call engineer; review error logs
```

**Alert 2: Rate Limit Exceeded**
```
Condition: 429 status code > 10 times in 5 minutes
Severity: WARNING
Action: Reduce concurrent requests; apply backoff
```

**Alert 3: Drift Detected**
```
Condition: > 100 drift items in single sync cycle
Severity: INFO
Action: Log for audit; optionally alert admin
```

---

## Conclusions & Phase 1 Integration

### Key Supplementary Findings

1. **Complex Attributes**: Multi-valued attributes (emails, phones) require per-provider mapping
2. **Manager Relationships**: Hierarchical references need two-step resolution + cycle detection
3. **Custom Extensions**: Must decide which extensions to preserve/drop per provider
4. **Salesforce Complexity**: Profile + Role + Permission Set = three overlapping models
5. **Workday Immutability**: Org hierarchy can't be created; must map to existing nodes
6. **ServiceNow Recursion**: Recursive groups not supported by SCIM; must flatten
7. **Drift Conflicts**: Delete vs Modify conflicts require timestamp-based resolution
8. **Rate Limiting**: Exponential backoff with jitter + token bucket algorithm recommended
9. **PII Redaction**: Partial mask for emails/phones; full redaction for sensitive fields
10. **Bulk Operations**: Provider bulk APIs (where available) superior to sequential calls

### Recommended Phase 1 Outputs

1. **Advanced Transformation Guide** (`contracts/transformation-advanced.md`)
   - Pattern matching algorithms
   - Conflict resolution decision trees
   - Provider-specific transformation examples

2. **Provider Integration Guide** (`contracts/provider-integration-details.md`)
   - Salesforce (Profile/Role/Permission management)
   - Workday (Org hierarchy mapping)
   - ServiceNow (Recursive group flattening)

3. **Drift Detection Guide** (`contracts/drift-detection.md`)
   - Drift types and detection strategies
   - Conflict resolution for drift scenarios
   - Reconciliation policies

4. **Observability Guide** (`contracts/observability.md`)
   - Metrics per provider
   - Alert thresholds
   - Dashboard KPIs

---

**Version**: 1.0.0 | **Date**: 2025-11-22 | **Status**: Ready for Phase 1 Detailed Design
