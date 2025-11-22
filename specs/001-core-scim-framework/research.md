# Phase 0 Research: SCIM 2.0 Specification & Provider Analysis

**Objective**: Deep understanding of SCIM 2.0 specification, provider API capabilities, group model diversity  
**Duration**: Week 1-2  
**Output**: Foundation for Phase 1 design and adapter interface contract definition

---

## Part 1: SCIM 2.0 Deep Dive (RFC 7643)

### SCIM 2.0 Core Concepts

**What is SCIM?**
System for Cross-domain Identity Management (SCIM) is a REST/JSON-based protocol for automated user provisioning and lifecycle management. SCIM 2.0 (RFC 7643, RFC 7644) standardizes how cloud applications receive, validate, and respond to user and group management operations.

**Why SCIM?**
- **Standardized Protocol**: Eliminates need for proprietary connectors
- **RESTful**: HTTP verbs (POST, GET, PATCH, DELETE) map cleanly to CRUD operations
- **JSON Format**: Human-readable, language-agnostic data representation
- **Security**: Built-in Bearer token authentication, TLS encryption
- **Schema Flexibility**: Core schema + custom extensions per provider

---

### RFC 7643: SCIM Data Models & JSON Format

#### 1. SCIM Core Schema

**Universal Attributes** (all resources):
- `id` (string, immutable, server-assigned) - Unique identifier
- `externalId` (string) - Unique identifier in external system (client-managed)
- `meta` (complex):
  - `resourceType` (string) - "User", "Group", etc.
  - `created` (dateTime) - ISO 8601 format
  - `lastModified` (dateTime) - ISO 8601 format
  - `location` (string) - URL reference to resource
  - `version` (string) - Entity tag for concurrency control (e.g., "W/\"3\"")

**User Resource** (Required + Optional attributes):

| Attribute | Type | Required | Multiplicity | Notes |
|-----------|------|----------|-------------|-------|
| `userName` | string | YES | Single | Must be unique within tenant |
| `name` | complex | NO | Single | Contains familyName, givenName, etc. |
| `displayName` | string | NO | Single | User-friendly display name |
| `nickName` | string | NO | Single | Informal name |
| `profileUrl` | string | NO | Single | URL to user's profile |
| `emails` | array | NO | Multi | Complex type with value, type, primary |
| `addresses` | array | NO | Multi | Complex type with street, locality, region, postalCode, country |
| `phoneNumbers` | array | NO | Multi | Complex type with value, type (work, home, mobile, fax, other) |
| `ims` | array | NO | Multi | Instant messaging (jabber, sip, xmpp, etc.) |
| `photos` | array | NO | Multi | Photo URLs with types |
| `active` | boolean | NO | Single | Default: true |
| `userType` | string | NO | Single | e.g., "Employee", "Contractor", "Customer" |
| `title` | string | NO | Single | Job title |
| `preferredLanguage` | string | NO | Single | BCP 47 language code (en-US, fr-FR, etc.) |
| `locale` | string | NO | Single | IETF Language Region code |
| `timezone` | string | NO | Single | IANA timezone (America/Los_Angeles, etc.) |
| `groups` | array | NO | Multi | Read-only; populated by Group membership |
| `roles` | array | NO | Multi | User roles (can be extended by providers) |
| `entitlements` | array | NO | Multi | List of entitlements user is entitled to |
| `x509Certificates` | array | NO | Multi | User X.509 certificates |

**Example User (Minimal)**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "id": "2819c223-7f76-453a-919d-413861904646",
  "externalId": "bjensen",
  "userName": "bjensen@example.com",
  "name": {
    "familyName": "Jensen",
    "givenName": "Barbara"
  },
  "displayName": "Barbara Jensen",
  "emails": [
    {
      "value": "bjensen@example.com",
      "type": "work",
      "primary": true
    }
  ],
  "active": true,
  "meta": {
    "resourceType": "User",
    "created": "2011-08-01T21:32:56.670Z",
    "lastModified": "2011-08-01T21:32:56.670Z",
    "location": "https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"a330bc81f0671071\""
  }
}
```

**Group Resource**:

| Attribute | Type | Required | Multiplicity | Notes |
|-----------|------|----------|-------------|-------|
| `displayName` | string | YES | Single | Group name |
| `members` | array | NO | Multi | References to users/groups in group |
| `description` | string | NO | Single | Group description |

**Example Group with Members**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "displayName": "Sales Team",
  "description": "EMEA Sales Organization",
  "members": [
    {
      "value": "2819c223-7f76-453a-919d-413861904646",
      "display": "Barbara Jensen",
      "$ref": "https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646"
    },
    {
      "value": "a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "display": "John Smith",
      "$ref": "https://example.com/v2/Users/a4d29ad1-d471-4d00-a6b8-07a27e6742f0"
    }
  ],
  "meta": {
    "resourceType": "Group",
    "created": "2011-08-01T21:32:56.670Z",
    "lastModified": "2011-08-01T21:32:56.670Z",
    "location": "https://example.com/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"a330bc81f0671071\""
  }
}
```

---

#### 2. SCIM Extension Attributes

SCIM allows extensions via the `schemas` attribute. Common extensions:

**Enterprise User Extension** (`urn:ietf:params:scim:schemas:extension:enterprise:2.0:User`):
```json
{
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "employeeNumber": "701984",
    "costCenter": "4130",
    "organization": "Engineering",
    "department": "Research and Development",
    "manager": {
      "value": "26118915-6090-4610-87e4-49d8ca9f808d",
      "display": "John Smith"
    }
  }
}
```

**Custom Extensions** (per provider):
- Salesforce: `urn:ietf:params:scim:schemas:extension:salesforce:2.0:User` - Sales roles, territories
- Workday: `urn:ietf:params:scim:schemas:extension:workday:2.0:User` - Employee ID, org hierarchy
- ServiceNow: Custom vendor extensions per app

---

#### 3. SCIM Filter & Query Syntax (RFC 7644)

**Filter Operators**:
```
eq     - equal
ne     - not equal
co     - contains
sw     - starts with
ew     - ends with
pr     - present (attribute exists)
gt     - greater than
ge     - greater than or equal
lt     - less than
le     - less than or equal
and    - logical AND
or     - logical OR
not    - logical NOT
```

**Example Filters**:
- `filter=userName eq "bjensen@example.com"` - Find user by exact username
- `filter=name.familyName co "Jensen"` - Users with family name containing "Jensen"
- `filter=emails.value sw "john"` - Users with email starting with "john"
- `filter=active eq true and userType eq "Employee"` - Active employees
- `filter=externalId pr` - Users with externalId present (have been synced)
- `filter=lastModified gt "2011-05-13T04:58:34Z"` - Users modified after date

**Query Parameters**:
```
GET /Users?filter=active eq true&sortBy=userName&sortOrder=ascending&startIndex=1&count=100
```

---

### RFC 7644: SCIM Protocol Operations

#### 1. HTTP Endpoint Design

**Standard SCIM Endpoints**:
```
GET    /Users                         - List users (with filtering, sorting, pagination)
POST   /Users                         - Create user
GET    /Users/{id}                    - Read specific user
PUT    /Users/{id}                    - Replace entire user
PATCH  /Users/{id}                    - Update specific attributes
DELETE /Users/{id}                    - Delete user

GET    /Groups                        - List groups
POST   /Groups                        - Create group
GET    /Groups/{id}                   - Read specific group
PUT    /Groups/{id}                   - Replace entire group
PATCH  /Groups/{id}                   - Update group members
DELETE /Groups/{id}                   - Delete group

POST   /Bulk                          - Batch operations (create multiple users/groups)
GET    /.well-known/scim-configuration - Service provider configuration
```

#### 2. HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | GET successful, PATCH successful |
| 201 | Created | POST successful (create user/group) |
| 204 | No Content | DELETE successful |
| 400 | Bad Request | Invalid request (malformed JSON, invalid schema) |
| 401 | Unauthorized | Missing/invalid Bearer token |
| 403 | Forbidden | Authenticated but no permission |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Version mismatch (optimistic concurrency), resource exists |
| 412 | Precondition Failed | Required version header missing on PATCH/PUT |
| 422 | Unprocessable Entity | Validation failed (e.g., duplicate userName) |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Provider error |
| 501 | Not Implemented | Optional feature not implemented |

#### 3. SCIM Error Response Format

**Error Schema**:
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 400,
  "scimType": "invalidSyntax",
  "detail": "Request is unparsable, syntactically incorrect, or violates JSON formatting rules."
}
```

**Common scimType Values**:
- `invalidSyntax` - Request is malformed JSON
- `tooMany` - Too many results (exceeded count limit)
- `uniqueness` - Duplicate value (e.g., userName already exists)
- `mutability` - Attempting to modify immutable attribute
- `invalidFilter` - Invalid filter expression
- `noTarget` - PATCH operation target doesn't exist
- `serverUnavailable` - Service temporarily unavailable
- `unsupportedFilter` - Filter not supported by provider
- `unsupportedPath` - PATCH path not supported
- `resourceAlreadyExists` - Resource already exists

**Example Error Response**:
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 409,
  "scimType": "uniqueness",
  "detail": "User with userName 'bjensen@example.com' already exists."
}
```

#### 4. PATCH Operations (RFC 6902 JSON Patch)

SCIM PATCH uses JSON Patch syntax for partial updates:

**Operations**:
- `add` - Add attribute or array element
- `remove` - Remove attribute or array element
- `replace` - Replace attribute value
- `move` - Move attribute
- `copy` - Copy attribute
- `test` - Test attribute value

**Example PATCH Request**:
```json
PATCH /Users/2819c223-7f76-453a-919d-413861904646 HTTP/1.1
Content-Type: application/scim+json
Authorization: Bearer {token}
If-Match: W/"a330bc81f0671071"

{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "replace",
      "path": "phoneNumbers[primary eq true].value",
      "value": "+1-555-0123"
    },
    {
      "op": "add",
      "path": "emails",
      "value": {
        "value": "bjensen.work@example.com",
        "type": "work"
      }
    },
    {
      "op": "remove",
      "path": "nickName"
    }
  ]
}
```

**Path Expressions**:
- `userName` - Top-level attribute
- `name.familyName` - Nested attribute
- `emails[value eq "bjensen@example.com"]` - Array element with filter
- `emails[0].value` - Array element by index
- `emails[primary eq true]` - Array element where primary is true

---

#### 5. Authentication & Authorization

**Bearer Token** (OAuth 2.0):
```
Authorization: Bearer {access_token}
```

**Token Validation**:
- Token must be issued by Entra ID for this tenant
- Token must be valid (not expired)
- Token must have SCIM provisioning scope
- Token claims must include `oid` (object ID) and `tid` (tenant ID)

**Tenant Isolation**:
- All operations scoped to authenticated tenant
- User cannot access another tenant's data
- Cross-tenant queries return empty results

---

### SCIM Extension Patterns

#### 1. Enterprise User Extension (Standard)

Microsoft & many providers support enterprise extension:

```json
{
  "schemas": [
    "urn:ietf:params:scim:schemas:core:2.0:User",
    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
  ],
  "userName": "john.smith@company.com",
  "displayName": "John Smith",
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "employeeNumber": "701984",
    "costCenter": "4130",
    "organization": "Sales",
    "department": "EMEA",
    "manager": {
      "value": "manager-user-id",
      "display": "Jane Manager",
      "$ref": "https://provider.com/Users/manager-user-id"
    }
  }
}
```

---

## Part 2: Provider API Analysis

### Provider 1: Salesforce

#### Salesforce User/Group Model

**User Representation**:
- Salesforce User object (native)
  - Username (email-format, unique)
  - Email
  - First Name / Last Name
  - Full Name
  - User Type (e.g., Standard, Admin, Chatter Free)
  - Role (not group membership; users assigned to org hierarchy role)
  - Profile (security settings)
  - Status (Active/Inactive)

**Group Representation**:
- Public Groups (custom groups, can contain users)
- Standard Groups (predefined: IT, Support, Sales, etc.)
- Permission Sets (NOT groups; are permissions)
- Roles (org hierarchy; users inherit role from org chart position)

**Critical Salesforce Concept: No "Group Membership" model**
- Salesforce doesn't have native group membership like Entra ID
- Users are assigned to Roles in org hierarchy
- Groups are primarily for Chatter and sharing
- Permission Sets are for access control (not groups)

**Key API Endpoints**:
```
GET    /services/data/v60.0/sobjects/User                    - List users
GET    /services/data/v60.0/sobjects/User/{id}               - Get user
POST   /services/data/v60.0/sobjects/User                    - Create user
PATCH  /services/data/v60.0/sobjects/User/{id}               - Update user
DELETE /services/data/v60.0/sobjects/User/{id}               - Delete user

GET    /services/data/v60.0/sobjects/Group                   - List groups
POST   /services/data/v60.0/sobjects/GroupMember             - Add user to group
DELETE /services/data/v60.0/sobjects/GroupMember/{id}        - Remove from group

GET    /services/data/v60.0/sobjects/PermissionSet           - List permission sets
POST   /services/data/v60.0/sobjects/PermissionSetAssignment - Assign permission set
```

**SCIM Support**:
- Salesforce DOES support SCIM 2.0 API (via Salesforce Identity)
- `/services/scim/v2/Users` and `/services/scim/v2/Groups` endpoints
- Maps SCIM groups to Salesforce groups OR permission sets (configurable)

---

### Provider 2: Workday

#### Workday User/Group Model

**User Representation**:
- Workday Worker (employee or contractor)
  - Worker ID (unique)
  - Email (work + personal)
  - First Name / Last Name
  - Organization (org hierarchy: Company → Department → Team)
  - Job Title
  - Manager (hierarchical relationship)
  - Status (Active/Inactive)
  - Custom Attributes (extensible)

**Group Representation**:
- Workday does NOT have traditional "groups"
- Organization hierarchy IS the grouping structure
- Security Groups (for access control, not user grouping)
- Role assignments (access control model)

**Critical Workday Concept: Org Hierarchy is grouping**
- Workday's grouping model is organizational structure (Company → Department → Division)
- Users belong to organization, not groups
- Organization hierarchy is 4+ levels deep typically
- Groups don't exist in Workday; org hierarchy replaces grouping

**Key API Endpoints**:
```
GET    /ccx/service/customreport2/company/Worker_v2        - List workers
GET    /ccx/service/customreport2/company/Worker_v2/{id}   - Get worker
PUT    /ccx/service/customreport2/company/Worker_v2/{id}   - Update worker

GET    /ccx/service/customreport2/company/Organization_v2  - List orgs

SCIM (if configured):
GET    /scim/v2/Users
POST   /scim/v2/Users
GET    /scim/v2/Groups (maps to organization hierarchy)
```

**SCIM Support**:
- Workday SCIM support is limited/beta (varies by version)
- Many integrations use custom XML/API instead of SCIM
- When SCIM is used, Groups map to organization hierarchy levels

---

### Provider 3: ServiceNow

#### ServiceNow User/Group Model

**User Representation**:
- ServiceNow User (sys_user table)
  - User ID (email-format or custom)
  - First Name / Last Name
  - Department (link to department record)
  - Email
  - Phone
  - Manager (hierarchical link)
  - Active (boolean)

**Group Representation**:
- ServiceNow Groups (sys_user_group table)
  - Group ID
  - Group Name
  - Description
  - Members (linked via user_group_membership table)
- **ServiceNow HAS native group membership**
- Groups can have users and roles
- Group membership is explicit

**Critical ServiceNow Concept: Native Group Support**
- ServiceNow understands "groups" natively
- Users can be added/removed from groups
- Groups can have roles/permissions
- Group membership is the primary access model

**Key API Endpoints**:
```
GET    /api/now/table/sys_user                              - List users
GET    /api/now/table/sys_user/{sys_id}                     - Get user
POST   /api/now/table/sys_user                              - Create user
PATCH  /api/now/table/sys_user/{sys_id}                     - Update user
DELETE /api/now/table/sys_user/{sys_id}                     - Delete user

GET    /api/now/table/sys_user_group                        - List groups
POST   /api/now/table/sys_user_group                        - Create group
GET    /api/now/table/sys_user_group_member                 - List group members
POST   /api/now/table/sys_user_group_member                 - Add user to group
DELETE /api/now/table/sys_user_group_member/{sys_id}        - Remove from group

SCIM (if configured):
GET    /api/sn_scim/v2/Users
POST   /api/sn_scim/v2/Users
GET    /api/sn_scim/v2/Groups
POST   /api/sn_scim/v2/Groups/{id}/Members
```

**SCIM Support**:
- ServiceNow supports SCIM 2.0 via managed connector
- Groups map directly to ServiceNow groups
- Full CRUD support for users and groups

---

## Part 3: Group Model Diversity Analysis

### Critical Finding: No Universal "Group" Model

| Provider | Group Model | Grouping Mechanism | User Assignment |
|----------|-------------|-------------------|-----------------|
| **Entra ID** | Native groups | Group object with members | Direct membership |
| **Salesforce** | No native groups | Org roles + permission sets | Role hierarchy + permission assignment |
| **Workday** | No groups | Organization hierarchy | Organization membership |
| **ServiceNow** | Native groups | Group object with members | Direct membership |

### Transformation Challenges

**Challenge 1: Salesforce - Role vs Group**
- SCIM group "Sales Team" maps to Salesforce... what?
  - Option A: Salesforce org role (Sales Org Level 1)
  - Option B: Salesforce public group (Sales Team)
  - Option C: Salesforce permission set (Sales Rep)
  - **Must be configurable per provider**

**Challenge 2: Workday - Org Hierarchy**
- SCIM group "EMEA Sales" maps to Workday organization
  - Which org level? (Company/Division/Department/Team)
  - Workday org structure is fixed; can't create arbitrary groups
  - **Must map groups to existing org nodes**

**Challenge 3: ServiceNow - Direct Mapping**
- SCIM group "Sales Team" maps to ServiceNow group "Sales Team"
  - If group doesn't exist, must create it
  - **Direct mapping possible; simplest case**

---

### Reverse Transformation: Provider → SCIM

**Salesforce Roles → SCIM Groups**:
- Salesforce role "Sales Representative" → SCIM group "Sales Representative"
- Members: users assigned to that role

**Workday Org → SCIM Groups**:
- Workday organization "EMEA Sales" → SCIM group "EMEA Sales"
- Members: workers assigned to that organization

**ServiceNow Groups → SCIM Groups**:
- Direct mapping (1:1)
- Group members query via API

---

## Part 4: Adapter Interface Contract (Preliminary)

Based on group model diversity, adapters must support:

### Adapter Methods (Finalized)

```csharp
public interface IAdapter
{
    // User operations
    Task<ScimUser> CreateUserAsync(ScimUser user);
    Task<ScimUser> GetUserAsync(string userId);
    Task<ScimUser> UpdateUserAsync(string userId, ScimUser user);
    Task DeleteUserAsync(string userId);
    Task<IEnumerable<ScimUser>> ListUsersAsync(QueryFilter filter);
    
    // Group operations
    Task<ScimGroup> CreateGroupAsync(ScimGroup group);
    Task<ScimGroup> GetGroupAsync(string groupId);
    Task<ScimGroup> UpdateGroupAsync(string groupId, ScimGroup group);
    Task DeleteGroupAsync(string groupId);
    Task<IEnumerable<ScimGroup>> ListGroupsAsync(QueryFilter filter);
    
    // Group membership (critical for transformation)
    Task AddUserToGroupAsync(string groupId, string userId);
    Task RemoveUserFromGroupAsync(string groupId, string userId);
    Task<IEnumerable<string>> GetGroupMembersAsync(string groupId);
    
    // Entitlement mapping (adapter-specific)
    Task<EntitlementMapping> MapGroupToEntitlementAsync(ScimGroup group);
    Task<ScimGroup> MapEntitlementToGroupAsync(EntitlementMapping entitlement);
}
```

### Adapter Configuration

```json
{
  "adapterId": "salesforce-prod",
  "providerName": "Salesforce",
  "providerApiUrl": "https://company.salesforce.com",
  "groupMappingStrategy": "ROLE_ASSIGNMENT",
  "transformationRules": [
    {
      "sourcePattern": "Sales-.*",
      "targetType": "SALESFORCE_ROLE",
      "targetMapping": "Sales Representative"
    },
    {
      "sourcePattern": "Support-.*",
      "targetType": "SALESFORCE_ROLE",
      "targetMapping": "Support Representative"
    }
  ],
  "credentialKeyVaultPath": "/scim/adapters/salesforce/credentials"
}
```

---

## Part 5: Data Flow Diagrams

### Push Direction (Entra → Provider)

```
Entra ID                           SCIMGateway                        Provider (Salesforce)
  |                                    |                                    |
  +-- User Created in Entra           |                                    |
       (john.smith@company.com)       |                                    |
       Email: john@company.com        |                                    |
       Groups: [Sales Team, EMEA]     |                                    |
                                      |                                    |
                                      +-- POST /Users                      |
                                      |    {SCIM User JSON}               |
                                      |                                    +-- Create User
                                      |                                    |   in Salesforce
                                      |                                    |
                                      |                                    |-- Transform
                                      |                                    |   Groups→Roles
                                      |                                    |
                                      |                                    +-- GET /Users
                                      |                                    |   (verify creation)
                                      |                                    |
                                      |<-- User Created Response (200)     |
                                      |                                    |
                                      +-- Apply Transformations           |
                                      |   Groups→Entitlements            |
                                      |                                    |
                                      +-- PATCH /Users/{id}               |
                                      |    Add roles/perms               |
                                      |                                    +-- Assign roles
                                      |                                    |
                                      |<-- Role Assignment OK (200)        |
                                      |                                    |
                                      +-- Log to App Insights             |
                                      |   CRUD operation: CREATE USER     |
                                      |   Resource: john.smith@company.com|
                                      |   Status: SUCCESS                 |
                                      |   Timestamp: 2025-11-22T10:00:00Z |
```

### Pull Direction (Provider → Entra)

```
Provider (Salesforce)              SCIMGateway                        Entra ID
       |                                 |                                 |
       |                                 |                                 |
[Scheduled Sync]                         |                                 |
       |                                 |                                 |
       |                                 +-- GET /Users                    |
       |                                 |   (poll current state)         |
       |                                 |                                 |
       +-- Return all users (1000)       |                                 |
       |   {user array}                  |                                 |
       |                                 |                                 |
       |                                 +-- Compare with                 |
       |                                 |   last known state             |
       |                                 |                                 |
       |                                 +-- Detect Drift:                |
       |                                 |   - User added on provider     |
       |                                 |   - User modified (role change)|
       |                                 |   - User deleted on provider   |
       |                                 |                                 |
       |                                 +-- Log Drift to Audit           |
       |                                 |   Resource: user-123           |
       |                                 |   Type: MODIFIED (role changed)|
       |                                 |   Old: role=User               |
       |                                 |   New: role=Admin              |
       |                                 |                                 |
       |                                 +-- Reconcile per policy         |
       |                                 |   (if push→ignore,             |
       |                                 |    if pull→apply)              |
       |                                 |                                 |
       |                                 +-- Update Sync State            |
       |                                 |   lastSyncTimestamp: now       |
       |                                 |   lastKnownState: snapshot     |
       |                                 |
```

---

## Part 6: Implementation Recommendations

### 1. Adapter Development Priority

**Priority 1: ServiceNow**
- Reason: Native group support matches Entra ID model closest
- Complexity: Low (direct group mapping)
- Effort: 1-2 weeks
- Risk: Low

**Priority 2: Salesforce**
- Reason: Widely used; group→role transformation needed
- Complexity: Medium (role assignment vs groups)
- Effort: 2-3 weeks
- Risk: Medium (Salesforce org role hierarchy varies per customer)

**Priority 3: Workday**
- Reason: Complex org hierarchy; most challenging
- Complexity: High (org hierarchy deep nesting)
- Effort: 3-4 weeks
- Risk: High (org structure varies significantly)

### 2. Transformation Engine Capability Matrix

| Feature | Requirement | Implementation Approach |
|---------|-------------|------------------------|
| Group → Role | Salesforce | Pattern matching (group name → role ID) + direct role assignment |
| Group → Org Node | Workday | Org ID lookup from configuration, deep hierarchy support |
| Group → Group | ServiceNow | 1:1 mapping, create if not exists |
| Group → Permission Set | Salesforce (alt) | Pattern matching + permission set assignment |
| Reverse Transform | All | Query provider for entitlements, map back to group |
| Conflict Resolution | All | Union (all matching entitlements), First-Match, Manual |

### 3. Error Handling per Provider

**Salesforce Error Handling**:
- Role not found → Log error, require manual mapping
- Permission set invalid → Log error, require admin review
- User email conflict → Return 409 Conflict, suggest resolution

**Workday Error Handling**:
- Org node not found → Log error, list available org nodes
- Worker ID format wrong → Return 400 Bad Request with format help
- Authorization errors common → Retry with exponential backoff

**ServiceNow Error Handling**:
- Group not found → Create group (if auto-creation enabled)
- User not found → Return 404
- Rate limit → Respect API throttling limits

---

## Part 7: SCIM Compliance Validator Design

**Contract Tests Required**:

1. **Schema Validation**
   - All User attributes present per RFC 7643
   - All Group attributes present
   - Optional attributes handled correctly
   - Custom extensions supported

2. **HTTP Status Codes**
   - 200 OK for GET success
   - 201 Created for POST success
   - 204 No Content for DELETE
   - 400/422 for validation errors
   - 409 for conflicts
   - 401 for auth failures

3. **Error Response Format**
   - All errors follow SCIM error schema
   - `scimType` present for validation errors
   - `detail` message provided

4. **Filter & Query**
   - Filter expressions parsed correctly
   - Pagination works (startIndex, count)
   - Sorting works (sortBy, sortOrder)

5. **PATCH Operations**
   - Add/remove/replace operations work
   - Path expressions resolved correctly
   - Concurrency control (If-Match) enforced

6. **Tenant Isolation**
   - Token validated against Entra ID
   - Cross-tenant requests return 403
   - Tenant ID enforced on all queries

---

## Conclusions & Next Steps

### Key Findings

1. **No Universal Group Model**: Each provider has different grouping mechanism (roles, org hierarchy, groups)
2. **Transformation is Essential**: Group mapping rules must be configurable per provider
3. **ServiceNow is MVP Easiest**: Direct 1:1 group mapping; lowest risk
4. **Salesforce Requires Flexibility**: Role vs permission set vs public group decisions
5. **Workday is Most Complex**: Org hierarchy deeply nested; requires careful mapping

### Phase 1 Design Deliverables

1. **Adapter Interface Contract** (`contracts/adapter-interface.md`)
   - Finalized IAdapter interface with all methods
   - Error handling patterns per provider
   - Transformation rule format

2. **Provider-Specific Guides** (`contracts/provider-*.md`)
   - ServiceNow integration specifics
   - Salesforce role mapping strategy
   - Workday org hierarchy mapping

3. **Transformation Engine Design** (`contracts/transformation-engine.md`)
   - Rule matching algorithm
   - Conflict resolution strategies
   - Reverse transformation logic

4. **SCIM Compliance Test Suite** (`contracts/scim-compliance.md`)
   - Contract tests for RFC 7643
   - Provider-specific compliance tests

---

**Version**: 1.0.0 | **Date**: 2025-11-22 | **Status**: Ready for Phase 1 Design
