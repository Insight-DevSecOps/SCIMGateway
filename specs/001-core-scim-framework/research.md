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

## Part 8: Stack Decision - Azure Functions vs App Service (R9)

### Executive Summary

**DECISION**: Use **Azure App Service (Standard S1)** for SCIM Gateway hosting.

**Rationale**: Cost efficiency ($70/month vs $205/month), standard ASP.NET Core patterns, predictable performance, and easier long-term maintenance outweigh Azure Functions' superior burst scaling. Both platforms meet FR-046 latency requirements (<500ms SDK operations).

---

### Architecture Comparison

#### Azure Functions

**Hosting Plans**:
1. **Consumption Plan** (Pay-per-execution)
   - Automatic scaling: 0-200 instances
   - Cold start: 1-3 seconds (.NET 8 isolated)
   - Pricing: $0.20/million executions + $0.000016/GB-s
   - Timeout: 5-10 minutes max
   - ❌ **Violates FR-046** (<500ms SDK internal requirement due to cold starts)
   - **Note**: RFC 7644 does not specify response time requirements. FR-046's <2s end-to-end target is an architectural decision based on user experience expectations, not an RFC mandate.

2. **Premium Plan** (Pre-warmed instances)
   - Always-on: 1+ pre-warmed instances
   - Cold start: <200ms (pre-warmed eliminates cold starts)
   - Pricing: $150-300/month base + execution costs
   - Timeout: 30 minutes default, unlimited possible
   - VNet integration included
   - ✅ **Meets FR-046** (<500ms SDK latency)

3. **Dedicated Plan** (App Service infrastructure)
   - Runs on App Service Plan
   - No cold starts (always-on)
   - Pricing: Same as App Service
   - Essentially App Service with Functions runtime

**Scaling Behavior**:
- Scale-out speed: **10-30 seconds** per new instance
- Max instances: 200 (Consumption), 100 (Premium)
- Scale trigger: HTTP requests/second, custom metrics
- Scale-in: Gradual after 5 minutes idle
- ✅ **Excellent for burst traffic**

---

#### App Service

**Service Plans**:
1. **Basic Tier (B1-B3)**
   - Pricing: $13-52/month
   - Always-on: Yes (manual enable)
   - Scaling: Manual only (1-3 instances)
   - ❌ **No auto-scaling**

2. **Standard Tier (S1-S3)**
   - Pricing: $70-280/month
   - Always-on: Yes
   - Scaling: Auto-scale up to 10 instances
   - Deployment slots: 5
   - ✅ **Recommended tier for SCIM**

3. **Premium Tier (P1v3-P3v3)**
   - Pricing: $200-800/month
   - Auto-scale: Up to 30 instances
   - VNet integration, private endpoints
   - Deployment slots: 20

**Scaling Behavior**:
- Scale-out speed: **1-2 minutes** per new instance
- Max instances: 10 (Standard), 30 (Premium)
- Scale trigger: CPU %, Memory %, HTTP queue length
- Scale-in: Based on scale rules (5-10 min idle)
- ⚠️ **Slower than Functions** but adequate for SCIM workload

---

### Cost Analysis

**Workload Assumptions** (typical SCIM deployment):
- 10 tenants
- 1000 users per tenant
- Sync every 30 minutes during 8 working hours
- 2000 requests per sync (GET + PATCH per user)
- Total: **9.6M requests/month** (320K/day × 30 days)
- Average execution time: 100ms/request

#### Azure Functions Premium (EP1):
```
Base cost (1 always-on instance):  $150.00/month
Execution cost (9.6M × $0.20/1M):    $1.92/month
Compute cost (9.6M × 0.1s × 3.5GB):  $53.76/month
─────────────────────────────────────────────────
Total:                              ~$205/month
```

#### App Service Standard S1:
```
Base cost:                           $70.00/month
Scale-out (during peak, +1 instance): $0-70/month (only when scaled)
─────────────────────────────────────────────────
Total:                              ~$70-140/month
Average:                            ~$105/month (assuming 50% scale-out time)
```

**Cost Winner**: **App Service** (48% cheaper baseline, comparable with scale-out)

---

### Cold Start Impact vs FR-046 Requirements

**FR-046**: SDK internal processing <500ms (MUST), end-to-end <2s (SHOULD)

**Important**: RFC 7644 does not specify response time or timeout requirements. The FR-046 performance targets are architectural decisions based on:
- General user experience expectations (2s is a common threshold for acceptable web response times)
- Industry best practices for SCIM implementations
- Real-world deployment experience

The <2s end-to-end target is **provider-dependent and best-effort**, acknowledging that SaaS provider API latency varies.

#### Azure Functions Cold Start Latency:

**Consumption Plan**:
- Cold start time: **1-3 seconds**
  - Runtime initialization: 800ms-1.5s
  - Dependency loading (Cosmos SDK, Key Vault): 500ms-1s
  - First request processing: 200-500ms
- Cold start triggers:
  - No requests for 20 minutes (idle timeout)
  - Scale-out to new instance
  - Function app restart/deployment
- ❌ **FAILS FR-046**: 1-3 second cold start exceeds <500ms SDK internal requirement
- **Note**: This violates our architectural performance target, not an RFC 7644 requirement

**Premium Plan**:
- Cold start time: **<200ms** (pre-warmed instances)
- Always 1+ instances warm and ready
- Scale-out: New instances pre-warm during scale (~30s)
- ✅ **PASSES FR-046**: <200ms latency, no unpredictable spikes

#### App Service:
- Cold start time: **None** (always-on enabled)
- First request after deployment: ~200ms (JIT compilation)
- Consistent latency: **10-50ms** per request
- ✅ **PASSES FR-046**: Always <500ms, predictable performance

**Latency Winner**: **Tie** (both Premium Functions & App Service meet requirements)

---

### Scaling Pattern Analysis

**SCIM Traffic Pattern**: Predictable burst traffic during scheduled sync windows.

**Example Burst**:
- 10 tenants sync simultaneously at 9:00 AM
- 20,000 requests in 5 minutes (4000 req/min)
- Baseline: 100 req/min during idle periods

#### Azure Functions Premium:
- Pre-warmed instances handle baseline load (100 req/min)
- Scale-out trigger: HTTP queue length, custom metrics
- Scale-out speed: **<30 seconds** (pre-warmed instances)
- Max instances: 100 (EP1 plan)
- Scale-in: Gradual after load decreases (5 min idle)
- ✅ **Excellent for burst traffic**: Rapid scale-out without cold starts

#### App Service Standard:
- Always-on instance handles baseline load
- Scale-out trigger: CPU %, Memory %, HTTP queue length
- Scale-out speed: **1-2 minutes** per new instance
- Max instances: 10 (Standard S1)
- Scale-in: Based on scale rules (typically 5-10 min)
- ⚠️ **Good for burst traffic**: Slower scale-out but adequate for SCIM workload

**Scaling Winner**: **Azure Functions** (3-4× faster scale-out)

**Reality Check**: SCIM sync windows are typically **predictable** (scheduled every 30 min). Scheduled scale-out rules in App Service can pre-scale before known burst windows, eliminating the 1-2 minute scale-out delay.

---

### Developer Experience

#### Local Development:

**Azure Functions**:
- ✅ Azure Functions Core Tools (CLI)
- ✅ VS Code extension with F5 debugging
- ✅ Local emulator (Azurite for storage bindings)
- ✅ Hot reload in development mode
- ⚠️ Local environment differs from cloud (Functions runtime vs App Service)
- ⚠️ Requires understanding Functions-specific concepts (triggers, bindings, host.json)

**App Service**:
- ✅ Standard ASP.NET Core development (Program.cs, Controllers)
- ✅ VS Code debugging (standard .NET)
- ✅ Kestrel local server (identical to cloud)
- ✅ Hot reload (.NET 8 built-in)
- ✅ **Identical local/cloud behavior** (same runtime, same code paths)
- ✅ No additional tooling beyond .NET SDK

#### Testing:

**Azure Functions**:
- ⚠️ HTTP trigger testing requires Functions host emulator
- ✅ Function code testable as standard methods (dependency injection works)
- ⚠️ Integration tests need Functions runtime or mocking
- ⚠️ Functions-specific testing patterns (ILogger<T> vs standard logging)

**App Service**:
- ✅ **Standard ASP.NET Core testing** (TestServer, WebApplicationFactory)
- ✅ Integration tests straightforward (in-memory server)
- ✅ No special emulator or runtime needed
- ✅ Industry-standard testing patterns (xUnit, NUnit, etc.)

#### Deployment:

**Azure Functions**:
- ✅ VS Code extension (1-click deploy)
- ✅ Azure CLI (`func azure functionapp publish`)
- ✅ GitHub Actions templates available
- ✅ Deployment slots (Premium plan only)
- ⚠️ Function app configuration complexity (host.json, function.json per function)

**App Service**:
- ✅ VS Code extension (1-click deploy)
- ✅ Azure CLI (`az webapp up`)
- ✅ GitHub Actions templates available
- ✅ Deployment slots (Standard+ tier)
- ✅ Blue-green deployments built-in
- ✅ Simpler configuration (single appsettings.json)

#### Monitoring:

**Azure Functions**:
- ✅ **Application Insights deep integration** (automatic)
- ✅ **Per-function metrics** (invocations, duration, errors, success rate)
- ✅ Distributed tracing automatic (dependency tracking)
- ✅ Live Metrics Stream
- ✅ Function-level logs in Azure Portal
- ✅ **Best-in-class observability**

**App Service**:
- ✅ Application Insights integration (requires manual setup)
- ✅ HTTP request metrics (per-endpoint)
- ✅ Distributed tracing (requires configuration)
- ✅ Live Metrics Stream
- ⚠️ Less granular than Functions (no automatic per-endpoint breakdown)

**Developer Experience Winner**: **App Service** (standard .NET patterns, easier testing, simpler local development)

---

### Decision Matrix

| Criteria | Azure Functions (Premium EP1) | App Service (Standard S1) | Winner |
|----------|------------------------------|---------------------------|--------|
| **Cost (baseline)** | $205/month | $70/month | **App Service** (66% cheaper) |
| **Cost (with scale-out)** | $205/month + scale | $105/month avg | **App Service** (49% cheaper) |
| **Cold Start** | <200ms (pre-warmed) | None (always-on) | **Tie** (both excellent) |
| **FR-046 Compliance** | ✅ <500ms | ✅ <500ms | **Tie** |
| **Scale-out Speed** | <30 seconds | 1-2 minutes | **Functions** |
| **Burst Traffic Handling** | Excellent (rapid scale) | Good (predictable schedule) | **Functions** |
| **Max Scale Instances** | 100 instances | 10 instances | **Functions** |
| **Developer Experience** | Good (Functions-specific) | Excellent (standard .NET) | **App Service** |
| **Testing Complexity** | Medium (Functions host) | Low (standard ASP.NET) | **App Service** |
| **Local Development** | Good (emulator) | Excellent (Kestrel) | **App Service** |
| **Monitoring Granularity** | Excellent (per-function) | Good (per-endpoint) | **Functions** |
| **Deployment Slots** | ✅ (Premium) | ✅ (Standard+) | **Tie** |
| **VNet Integration** | ✅ (Premium) | ✅ (Standard+) | **Tie** |
| **Long-term Maintenance** | Functions-specific patterns | Standard .NET patterns | **App Service** |
| **Team Onboarding** | Requires Functions training | Standard ASP.NET Core | **App Service** |
| **Migration Flexibility** | Functions-specific | Standard HTTP hosting | **App Service** |

**Score**: App Service **10** | Functions **6** | Tie **5**

---

### Recommendation: Azure App Service (Standard S1)

**Primary Reasons**:

1. **Cost Efficiency** (66% cheaper baseline)
   - $70/month vs $205/month for comparable performance
   - SCIM workload is predictable; burst scaling less critical
   - Auto-scaling handles traffic spikes adequately (1-2 min scale-out acceptable)

2. **FR-046 Compliance**
   - Always-on eliminates cold starts entirely
   - Consistent <500ms SDK latency
   - Predictable, reliable performance

3. **Developer Experience**
   - Standard ASP.NET Core patterns (Controllers, Middleware, DI)
   - Easier onboarding for .NET developers
   - Standard testing frameworks (WebApplicationFactory, TestServer)
   - No Functions-specific concepts to learn

4. **Simplicity**
   - Single appsettings.json configuration
   - Standard HTTP hosting model
   - Easier to reason about performance and scaling
   - No pre-warming or Functions runtime complexity

5. **Long-term Maintenance**
   - ASP.NET Core is industry-standard, well-documented
   - Larger talent pool (easier to hire developers)
   - Migration path to other hosting (AKS, on-premises) simpler
   - Future-proof architecture

6. **SCIM Workload Characteristics Favor App Service**
   - Predictable traffic patterns (scheduled syncs)
   - Not event-driven (no queues, no triggers)
   - HTTP-only workload (no messaging, no timers)
   - 1-2 minute scale-out acceptable for scheduled bursts

---

### When Azure Functions Would Be Better

Azure Functions Premium would be the better choice if:

1. **Extreme Burst Traffic**: 100+ tenants syncing simultaneously, requiring <30s scale-out
2. **Event-Driven Patterns**: Future requirements include queue-based processing, timers, or non-HTTP triggers
3. **Serverless Cost Model**: Traffic is very sporadic (e.g., only 2-3 hours/day), not SCIM's continuous pattern
4. **Multi-Protocol Workload**: Combining HTTP endpoints with queue processing, timers, or event-driven functions
5. **Per-Function Isolation**: Need to isolate tenant workloads at function-level (not required for SCIM)

**Reality**: SCIM Gateway is HTTP-only, predictable traffic, standard CRUD operations → **App Service is ideal fit**.

---

### Implementation Plan

**Phase 1: Initialize with App Service Standard S1**
- Configure always-on for zero cold starts
- Enable Application Insights for monitoring
- Configure auto-scaling rules:
  - Scale-out: HTTP queue length >100 OR CPU >70%
  - Scale-in: HTTP queue length <20 AND CPU <40% for 10 min
  - Schedule-based: Pre-scale at 8:55 AM (before 9 AM sync window)
- Enable deployment slots (blue-green deployments)

**Phase 2: Monitor and Optimize**
- Track latency metrics (target: 95th percentile <200ms)
- Monitor scale-out behavior during burst windows
- Analyze cost vs performance tradeoffs

**Phase 3: Upgrade Path (if needed)**
- If scale-out speed becomes bottleneck (>10 tenants with simultaneous syncs):
  - Upgrade to Premium App Service (P1v3) for 30 max instances
  - OR migrate to Azure Functions Premium for <30s scale-out
- Migration path: Same ASP.NET Core code, minimal changes required

---

### Final Decision

**Use Azure App Service (Standard S1)** for initial SCIM Gateway implementation.

**Justification**: Cost efficiency, standard .NET patterns, predictable performance, and ease of development/maintenance align with SCIM Gateway requirements. Azure Functions' superior burst scaling is not worth the 3× cost premium and increased complexity for SCIM's predictable workload pattern.

**Upgrade Trigger**: If production monitoring shows scale-out delays causing latency spikes (>500ms) during burst windows, migrate to Functions Premium or upgrade to App Service Premium tier.

---

## Part 9: Consumption Plan Reconsidered (Relaxed FR-046)

### Context

This analysis reconsiders **Azure Functions Consumption Plan** assuming FR-046's <500ms SDK requirement is relaxed to accommodate cold starts. The question: **Is Consumption Plan cheaper than App Service S1 when cold starts are acceptable?**

### Updated FR-046 Assumption

**Relaxed Requirement**:
- SDK internal processing: <3s p95 (allows for 1-3s cold starts)
- End-to-end: <5s p95 (allows for cold start + provider latency)
- **Tradeoff**: Accept occasional 1-3s first-request latency for potential cost savings

**Impact**:
- Entra ID sync clients: Can tolerate higher latency (batch operations, not real-time)
- User experience: Admins configuring sync see occasional delays, but not user-facing

### Cost Analysis: Consumption vs App Service

#### Scenario 1: Original Traffic Estimate (9.6M requests/month)

**Azure Functions Consumption Plan**:
```
Execution cost: 9.6M requests × $0.20/1M        = $1.92/month
Compute cost: 9.6M × 0.1s × 1GB × $0.000016/GB-s = $15.36/month
─────────────────────────────────────────────────────────────────
Total:                                           = $17.28/month
```

**App Service Standard S1** (baseline):
```
Base cost:                                       = $70.00/month
```

**Winner**: **Consumption Plan** ($17.28 vs $70.00 = **75% cheaper**)

---

#### Scenario 2: Realistic Traffic Estimate (460K requests/month)

Using the 95% reduction model from earlier analysis (change-detection reduces sync frequency):

**Azure Functions Consumption Plan**:
```
Execution cost: 460K requests × $0.20/1M         = $0.09/month
Compute cost: 460K × 0.1s × 1GB × $0.000016/GB-s = $0.74/month
─────────────────────────────────────────────────────────────────
Total:                                           = $0.83/month
```

**App Service Standard S1** (baseline):
```
Base cost:                                       = $70.00/month
```

**Winner**: **Consumption Plan** ($0.83 vs $70.00 = **98.8% cheaper**)

---

### Cold Start Impact Analysis

#### Cold Start Frequency

**Consumption Plan Idle Timeout**: 20 minutes

**SCIM Sync Pattern**:
- Scheduled syncs: Every 30 minutes (8:00 AM - 6:00 PM)
- Gap between syncs: 30 minutes
- Idle timeout: 20 minutes

**Cold Start Calculation**:
- If sync takes <10 minutes to complete: Instance stays warm for 10 min after sync
- Next sync in 30 minutes: Instance has been idle for 20+ minutes → **cold start**
- **Frequency**: Potentially **every sync** experiences cold start on first request

**Monthly Cold Starts** (10 tenants, 16 syncs/day):
- Cold starts per day: ~16 (one per sync window per tenant)
- Cold starts per month: ~480 cold starts
- Warm requests: 460K - 480 = 459,520 warm requests

#### Latency Distribution

**With Cold Starts** (Consumption Plan):
- Cold start requests (480/month): 1-3 seconds (0.1% of requests)
- Warm requests (459,520/month): 50-200ms (99.9% of requests)
- **p95 latency**: ~200ms (dominated by warm requests)
- **p99 latency**: ~1.5s (includes some cold starts)
- **p99.9 latency**: ~3s (worst-case cold starts)

**Without Cold Starts** (App Service):
- All requests: 50-200ms
- **p95 latency**: ~150ms
- **p99 latency**: ~200ms
- **p99.9 latency**: ~300ms

### Cost-Benefit Comparison

| Dimension | Consumption Plan | App Service S1 | Analysis |
|-----------|------------------|----------------|----------|
| **Monthly Cost (Realistic)** | $0.83 | $70.00 | **Consumption 98.8% cheaper** |
| **Monthly Cost (High Traffic)** | $17.28 | $70.00 | **Consumption 75% cheaper** |
| **Cold Starts/Month** | ~480 | 0 | **480 degraded requests** |
| **Cold Start Impact** | 0.1% of requests | 0% | **Minimal user impact** |
| **p95 Latency** | ~200ms | ~150ms | **Nearly identical** |
| **p99 Latency** | ~1.5s | ~200ms | **Consumption 7× worse** |
| **First Request After Idle** | 1-3s | 50ms | **Consumption 20-60× worse** |
| **Developer Experience** | Functions-specific | Standard .NET | **App Service better** |
| **Local Development** | Functions emulator | Kestrel (native) | **App Service simpler** |
| **Testing Complexity** | Medium | Low | **App Service easier** |
| **Monitoring** | Per-function (excellent) | Per-endpoint (good) | **Consumption better** |
| **Scaling** | Automatic (0-200) | Manual setup required | **Consumption simpler** |
| **Predictable Costs** | Pay-per-use (variable) | Fixed $70/month | **App Service predictable** |

### Break-Even Analysis

**When does App Service become cheaper than Consumption?**

At 460K req/month realistic traffic:
- Consumption: $0.83/month
- App Service: $70.00/month
- **Break-even**: Never (Consumption always cheaper at this scale)

At 9.6M req/month high traffic:
- Consumption: $17.28/month
- App Service: $70.00/month
- **Break-even**: ~40M requests/month

**Calculation**:
```
App Service cost = Consumption cost
$70 = (requests × $0.20/1M) + (requests × 0.1s × 1GB × $0.000016/GB-s)
$70 = requests × ($0.20 + $1.60) / 1M
$70 = requests × $1.80 / 1M
requests = $70 × 1M / $1.80
requests = 38.9M requests/month
```

**Reality Check**: 38.9M requests/month = 1.3M requests/day = 54K requests/hour (continuous). This is **100× higher** than realistic SCIM workload.

### Objective Recommendation

#### If Cost is Primary Concern: **Azure Functions Consumption Plan**

**Justification**:
- **$0.83/month** vs $70.00/month = **98.8% cost reduction**
- **$840/year savings** for minimal functional impact
- Cold starts affect only 0.1% of requests (480 out of 460K)
- SCIM sync is not user-facing (admins won't notice 1-3s first-request delay)
- Entra ID sync clients are fault-tolerant (can handle occasional latency)

**Acceptable Tradeoffs**:
- ~480 cold starts/month (first request of each sync window)
- p99 latency: 1.5s vs 200ms (affects 1% of requests)
- Functions-specific development patterns (worth cost savings)

#### If Performance/Developer Experience is Primary: **App Service Standard S1**

**Justification**:
- **Zero cold starts** (consistent 50-200ms latency)
- **Standard .NET patterns** (easier development, testing, onboarding)
- **Predictable costs** ($70/month fixed, easier budgeting)
- **Better p99 latency** (200ms vs 1.5s)

**Tradeoffs**:
- **$840/year higher cost** for minimal performance benefit
- Fixed cost regardless of actual usage (wasteful if traffic is low)

### Decision Framework

**Choose Consumption Plan IF**:
- Budget is constrained (startups, side projects, proof-of-concept)
- Traffic is low/moderate (<10M req/month)
- Cold start latency is acceptable (not user-facing, batch operations)
- Cost predictability is less important than absolute cost

**Choose App Service S1 IF**:
- Performance consistency is critical (strict SLAs)
- Developer experience matters (standard .NET, easier onboarding)
- Cost predictability is important (fixed budgeting)
- Cold starts are unacceptable (even 0.1% impact)
- Future growth expected (easier to scale up within App Service tiers)

### Updated Recommendation

Given the **98.8% cost reduction** ($0.83 vs $70/month) with **minimal functional impact** (0.1% of requests affected), **Azure Functions Consumption Plan** becomes the more rational choice when FR-046 is relaxed.

**Recommendation**: Start with **Consumption Plan**, monitor p99 latency and cold start impact, and upgrade to **App Service S1** or **Functions Premium EP1** if cold starts become problematic in production.

**Migration Path**: Azure Functions code can run on Dedicated App Service Plan with minimal changes if cold starts become an issue, providing a clear upgrade path.

---

**Version**: 1.1.0 | **Date**: 2025-11-22 | **Status**: Consumption Plan Reconsidered
