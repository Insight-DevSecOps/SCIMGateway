# SCIM Group Endpoints Contract

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22  
**RFC Reference**: RFC 7643 (Schema), RFC 7644 (Protocol)

---

## 1. Overview

This document defines the HTTP API contract for SCIM 2.0 Group resource endpoints. All endpoints must comply with RFC 7643 (SCIM Core Schema) and RFC 7644 (SCIM Protocol).

**Base Path**: `/scim/v2/Groups`

---

## 2. Endpoint Definitions

### 2.1 Create Group

**Endpoint**: `POST /scim/v2/Groups`

**Description**: Create a new Group resource.

**Request Headers**:
```
Authorization: Bearer <access_token>
Content-Type: application/scim+json
```

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "externalId": "sales-team",
  "displayName": "Sales Team",
  "description": "EMEA Sales Organization",
  "members": [
    {
      "value": "2819c223-7f76-453a-919d-413861904646",
      "display": "Barbara Jensen",
      "$ref": "/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
      "type": "User"
    }
  ]
}
```

**Success Response** (201 Created):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "externalId": "sales-team",
  "displayName": "Sales Team",
  "description": "EMEA Sales Organization",
  "members": [
    {
      "value": "2819c223-7f76-453a-919d-413861904646",
      "display": "Barbara Jensen",
      "$ref": "/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
      "type": "User"
    }
  ],
  "meta": {
    "resourceType": "Group",
    "created": "2025-11-22T10:00:00.000Z",
    "lastModified": "2025-11-22T10:00:00.000Z",
    "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"1\""
  }
}
```

**Error Response** (409 Conflict):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "409",
  "scimType": "uniqueness",
  "detail": "Group with displayName 'Sales Team' already exists"
}
```

**Validation Rules**:
- `displayName` is **required** and must be unique per tenant
- `externalId` is **optional** but must be unique if provided
- `members` is **optional** (can create empty group)
- Member references must exist in the same tenant

---

### 2.2 Get Group by ID

**Endpoint**: `GET /scim/v2/Groups/{id}`

**Description**: Retrieve a single Group by ID.

**Request Headers**:
```
Authorization: Bearer <access_token>
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
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
    "lastModified": "2025-11-22T11:30:00.000Z",
    "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"2\""
  }
}
```

**Error Response** (404 Not Found):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "404",
  "detail": "Group e9e30844-f356-4344-b7c1-b9602b88e457 not found"
}
```

---

### 2.3 Update Group (PUT - Full Replacement)

**Endpoint**: `PUT /scim/v2/Groups/{id}`

**Description**: Replace entire Group resource (full update).

**Request Headers**:
```
Authorization: Bearer <access_token>
Content-Type: application/scim+json
If-Match: W/"1"
```

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "externalId": "sales-team",
  "displayName": "Sales Team (Updated)",
  "description": "Global Sales Organization",
  "members": [
    {
      "value": "2819c223-7f76-453a-919d-413861904646",
      "type": "User"
    },
    {
      "value": "a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "type": "User"
    },
    {
      "value": "c2f1a890-d5e7-4c00-b8a9-12b45c6789de",
      "type": "User"
    }
  ]
}
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "externalId": "sales-team",
  "displayName": "Sales Team (Updated)",
  "description": "Global Sales Organization",
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
    },
    {
      "value": "c2f1a890-d5e7-4c00-b8a9-12b45c6789de",
      "display": "Jane Doe",
      "$ref": "/scim/v2/Users/c2f1a890-d5e7-4c00-b8a9-12b45c6789de",
      "type": "User"
    }
  ],
  "meta": {
    "resourceType": "Group",
    "created": "2025-11-22T10:00:00.000Z",
    "lastModified": "2025-11-22T12:00:00.000Z",
    "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"2\""
  }
}
```

**Error Response** (409 Conflict - Version Mismatch):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "409",
  "scimType": "uniqueness",
  "detail": "Version mismatch: expected W/\"1\" but found W/\"2\""
}
```

**Validation Rules**:
- `If-Match` header **required** for optimistic concurrency control
- `displayName` **required** and must be unique per tenant
- Missing `members` array results in **empty group** (all members removed)
- Invalid member references return **400 Bad Request**

---

### 2.4 Partial Update Group (PATCH)

**Endpoint**: `PATCH /scim/v2/Groups/{id}`

**Description**: Partially update Group using JSON Patch operations (RFC 6902).

**Request Headers**:
```
Authorization: Bearer <access_token>
Content-Type: application/scim+json
If-Match: W/"2"
```

**Request Body** (Add Member):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "add",
      "path": "members",
      "value": [
        {
          "value": "d3e4b567-c890-4d12-b3c4-56d78e90f123",
          "type": "User"
        }
      ]
    }
  ]
}
```

**Request Body** (Remove Member):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "remove",
      "path": "members[value eq \"2819c223-7f76-453a-919d-413861904646\"]"
    }
  ]
}
```

**Request Body** (Replace displayName):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "replace",
      "path": "displayName",
      "value": "EMEA Sales Team"
    }
  ]
}
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "externalId": "sales-team",
  "displayName": "EMEA Sales Team",
  "description": "Global Sales Organization",
  "members": [
    {
      "value": "a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "display": "John Smith",
      "$ref": "/scim/v2/Users/a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "type": "User"
    },
    {
      "value": "c2f1a890-d5e7-4c00-b8a9-12b45c6789de",
      "display": "Jane Doe",
      "$ref": "/scim/v2/Users/c2f1a890-d5e7-4c00-b8a9-12b45c6789de",
      "type": "User"
    },
    {
      "value": "d3e4b567-c890-4d12-b3c4-56d78e90f123",
      "display": "Alice Brown",
      "$ref": "/scim/v2/Users/d3e4b567-c890-4d12-b3c4-56d78e90f123",
      "type": "User"
    }
  ],
  "meta": {
    "resourceType": "Group",
    "created": "2025-11-22T10:00:00.000Z",
    "lastModified": "2025-11-22T13:00:00.000Z",
    "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
    "version": "W/\"3\""
  }
}
```

**Error Response** (400 Bad Request - Invalid Path):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "400",
  "scimType": "invalidPath",
  "detail": "Path 'members[invalid]' is invalid"
}
```

**Supported PATCH Operations**:
1. **Add member**: `{"op": "add", "path": "members", "value": [...]}`
2. **Remove member**: `{"op": "remove", "path": "members[value eq \"user-id\"]"}`
3. **Replace displayName**: `{"op": "replace", "path": "displayName", "value": "..."}`
4. **Replace description**: `{"op": "replace", "path": "description", "value": "..."}`

---

### 2.5 Delete Group

**Endpoint**: `DELETE /scim/v2/Groups/{id}`

**Description**: Delete a Group resource.

**Request Headers**:
```
Authorization: Bearer <access_token>
If-Match: W/"3"
```

**Success Response** (204 No Content):
```
(Empty body)
```

**Error Response** (404 Not Found):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "404",
  "detail": "Group e9e30844-f356-4344-b7c1-b9602b88e457 not found"
}
```

**Behavior**:
- Deleting a group **does NOT delete member users** (only membership)
- Audit log captures group deletion with member list snapshot

---

### 2.6 List Groups (Query with Filters)

**Endpoint**: `GET /scim/v2/Groups?filter=...&startIndex=...&count=...&sortBy=...&sortOrder=...`

**Description**: Query Groups with filtering, pagination, and sorting.

**Request Headers**:
```
Authorization: Bearer <access_token>
```

**Query Parameters**:
| Parameter | Required | Description | Example |
|-----------|----------|-------------|---------|
| `filter` | No | SCIM filter expression | `displayName eq "Sales Team"` |
| `startIndex` | No | 1-based start index (default: 1) | `1` |
| `count` | No | Results per page (default: 100, max: 1000) | `50` |
| `sortBy` | No | Attribute to sort by | `displayName` |
| `sortOrder` | No | `ascending` or `descending` (default: `ascending`) | `ascending` |

**Filter Expression Examples**:

1. **Exact match**:
   ```
   GET /scim/v2/Groups?filter=displayName eq "Sales Team"
   ```

2. **Contains**:
   ```
   GET /scim/v2/Groups?filter=displayName co "Sales"
   ```

3. **Starts with**:
   ```
   GET /scim/v2/Groups?filter=displayName sw "Sales"
   ```

4. **Has member**:
   ```
   GET /scim/v2/Groups?filter=members[value eq "user-id-123"]
   ```

5. **Multiple conditions (AND)**:
   ```
   GET /scim/v2/Groups?filter=displayName sw "Sales" and members pr
   ```

6. **OR condition**:
   ```
   GET /scim/v2/Groups?filter=displayName eq "Sales Team" or displayName eq "Marketing Team"
   ```

7. **Sort by displayName**:
   ```
   GET /scim/v2/Groups?sortBy=displayName&sortOrder=ascending
   ```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "totalResults": 2,
  "startIndex": 1,
  "itemsPerPage": 100,
  "Resources": [
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
      "id": "e9e30844-f356-4344-b7c1-b9602b88e457",
      "externalId": "sales-team",
      "displayName": "Sales Team",
      "description": "EMEA Sales Organization",
      "members": [
        {
          "value": "2819c223-7f76-453a-919d-413861904646",
          "display": "Barbara Jensen",
          "$ref": "/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
          "type": "User"
        }
      ],
      "meta": {
        "resourceType": "Group",
        "created": "2025-11-22T10:00:00.000Z",
        "lastModified": "2025-11-22T10:00:00.000Z",
        "location": "/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457",
        "version": "W/\"1\""
      }
    },
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
      "id": "f1a23456-b789-4c01-d234-567890abcdef",
      "externalId": "marketing-team",
      "displayName": "Marketing Team",
      "description": "Global Marketing",
      "members": [],
      "meta": {
        "resourceType": "Group",
        "created": "2025-11-22T10:30:00.000Z",
        "lastModified": "2025-11-22T10:30:00.000Z",
        "location": "/scim/v2/Groups/f1a23456-b789-4c01-d234-567890abcdef",
        "version": "W/\"1\""
      }
    }
  ]
}
```

**Error Response** (400 Bad Request - Invalid Filter):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "400",
  "scimType": "invalidFilter",
  "detail": "Filter expression 'invalid syntax' is malformed"
}
```

---

## 3. Group Membership Operations

### 3.1 Add User to Group (PATCH)

**Endpoint**: `PATCH /scim/v2/Groups/{groupId}`

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "add",
      "path": "members",
      "value": [
        {
          "value": "user-id-123",
          "type": "User"
        }
      ]
    }
  ]
}
```

**Behavior**:
- Adds user to group if not already member
- Triggers transformation rule evaluation (group → entitlement)
- Updates user's `groups` array attribute

---

### 3.2 Remove User from Group (PATCH)

**Endpoint**: `PATCH /scim/v2/Groups/{groupId}`

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "remove",
      "path": "members[value eq \"user-id-123\"]"
    }
  ]
}
```

**Behavior**:
- Removes user from group
- Removes entitlement from provider (if mapped)
- Updates user's `groups` array attribute

---

### 3.3 Replace All Members (PUT)

**Endpoint**: `PUT /scim/v2/Groups/{groupId}`

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
  "id": "group-id",
  "displayName": "Sales Team",
  "members": [
    {"value": "user-1", "type": "User"},
    {"value": "user-2", "type": "User"}
  ]
}
```

**Behavior**:
- Replaces **entire** members array
- Removes users not in new list
- Adds users in new list
- Triggers bulk transformation rule evaluation

---

## 4. Authentication & Authorization

### 4.1 OAuth 2.0 Bearer Token

All requests **MUST** include:
```
Authorization: Bearer <access_token>
```

**Token Validation**:
1. Validate token signature with Entra ID public keys
2. Verify `iss` claim matches `https://sts.windows.net/{tenant-id}/`
3. Verify `aud` claim matches API application ID
4. Extract `tid` claim for tenant isolation
5. Extract `oid` claim for actor identification

### 4.2 Tenant Isolation

All Group operations **MUST** enforce tenant isolation:
- Filter all queries by `tenantId = tid from token`
- Reject cross-tenant operations with **403 Forbidden**

### 4.3 Rate Limiting

**Limits**:
- 1000 requests/minute per tenant
- 10,000 requests/hour per tenant

**Headers** (included in all responses):
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 995
X-RateLimit-Reset: 1700000000
```

**Error Response** (429 Too Many Requests):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "429",
  "scimType": "tooMany",
  "detail": "Rate limit exceeded. Retry after 60 seconds.",
  "Retry-After": "60"
}
```

---

## 5. Error Responses

### 5.1 Standard Error Schema

All errors follow RFC 7644 Error schema:

```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "<HTTP status code>",
  "scimType": "<SCIM error type>",
  "detail": "<Human-readable description>"
}
```

### 5.2 Error Code Mapping

| HTTP Status | scimType | Description | Example |
|-------------|----------|-------------|---------|
| 400 | invalidSyntax | Malformed request | Missing required attribute |
| 400 | invalidFilter | Invalid filter expression | `filter=invalid syntax` |
| 400 | invalidPath | Invalid PATCH path | `path=members[invalid]` |
| 401 | - | Unauthorized | Missing or invalid token |
| 403 | - | Forbidden | Cross-tenant access attempt |
| 404 | - | Not Found | Group does not exist |
| 409 | uniqueness | Duplicate resource | Group displayName exists |
| 409 | mutability | Version mismatch | If-Match header mismatch |
| 412 | - | Precondition Failed | If-Match header missing |
| 422 | - | Unprocessable Entity | Member reference invalid |
| 429 | tooMany | Rate limit exceeded | > 1000 req/min |
| 500 | - | Internal Server Error | Unhandled exception |
| 501 | - | Not Implemented | Unsupported feature |

---

## 6. Audit Logging

### 6.1 Log Structure

All Group operations **MUST** be logged to Application Insights:

```json
{
  "timestamp": "2025-11-22T10:00:00.000Z",
  "tenantId": "tenant-123",
  "actorId": "service-principal-456",
  "actorType": "SERVICE_PRINCIPAL",
  "operationType": "CREATE_GROUP",
  "resourceType": "GROUP",
  "resourceId": "e9e30844-f356-4344-b7c1-b9602b88e457",
  "displayName": "Sales Team",
  "httpStatus": 201,
  "responseTimeMs": 850,
  "requestId": "req-789",
  "adapterId": null,
  "oldValue": null,
  "newValue": {
    "displayName": "Sales Team",
    "memberCount": 1
  },
  "errorCode": null,
  "errorMessage": null
}
```

### 6.2 PII Redaction

**No PII in Group logs** (groups don't have PII attributes like email/phone).

---

## 7. Versioning & Concurrency

### 7.1 Optimistic Concurrency Control

**ETag Header**:
```
ETag: W/"1"
```

**If-Match Header** (required for PUT/PATCH/DELETE):
```
If-Match: W/"1"
```

**Version Increment**:
- Every update increments `meta.version` by 1
- `meta.version` format: `W/"<integer>"`

### 7.2 Conflict Resolution

**Scenario**: Two concurrent updates to same group

**Request 1** (If-Match: W/"1"):
```
PATCH /scim/v2/Groups/{id}
{"op": "add", "path": "members", "value": [{"value": "user-1"}]}
```

**Request 2** (If-Match: W/"1"):
```
PATCH /scim/v2/Groups/{id}
{"op": "add", "path": "members", "value": [{"value": "user-2"}]}
```

**Outcome**:
- First request succeeds → version becomes W/"2"
- Second request fails with **409 Conflict**
- Client must re-fetch group, resolve conflict, and retry with W/"2"

---

## 8. Performance Requirements

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Latency (p95)** | < 2 seconds | Time from request to response |
| **Throughput** | 1000 req/s | Concurrent group operations |
| **Availability** | 99.9% SLA | Uptime per month |
| **Error Rate** | < 1% | Failed requests / total requests |

---

## 9. Contract Tests

### 9.1 Test: Create Group

**Given**: Valid OAuth token for tenant-123  
**When**: POST `/scim/v2/Groups` with valid group data  
**Then**:
- Response status is 201 Created
- Response contains `id`, `meta.created`, `meta.version = W/"1"`
- Response `displayName` matches request

### 9.2 Test: List Groups with Filter

**Given**: 5 groups exist in tenant-123  
**When**: GET `/scim/v2/Groups?filter=displayName co "Sales"`  
**Then**:
- Response status is 200 OK
- `totalResults` matches filtered count
- All `Resources[*].displayName` contain "Sales"

### 9.3 Test: Add Member to Group

**Given**: Group "Sales Team" exists with 2 members  
**When**: PATCH `/scim/v2/Groups/{id}` with add member operation  
**Then**:
- Response status is 200 OK
- Response `members` array contains 3 members
- `meta.version` incremented to W/"2"

### 9.4 Test: Remove Member from Group

**Given**: Group "Sales Team" exists with 3 members  
**When**: PATCH `/scim/v2/Groups/{id}` with remove member operation  
**Then**:
- Response status is 200 OK
- Response `members` array contains 2 members
- Removed member no longer in array

---

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
