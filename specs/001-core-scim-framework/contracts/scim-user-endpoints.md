# SCIM User Endpoints Contract

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22  
**Specification**: RFC 7643, RFC 7644

---

## 1. Overview

This document defines the HTTP API contract for SCIM 2.0 User endpoints per RFC 7643 and RFC 7644. All endpoints must comply with Microsoft SCIM specification requirements for Entra ID integration.

### Base Path

```
https://{gateway-host}/scim/v2/Users
```

---

## 2. Endpoint Definitions

### 2.1 Create User

**Endpoint**: `POST /scim/v2/Users`

**Headers**:
```
Content-Type: application/scim+json
Authorization: Bearer {access_token}
```

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "userName": "bjensen@example.com",
  "name": {
    "familyName": "Jensen",
    "givenName": "Barbara",
    "formatted": "Ms. Barbara J Jensen, III"
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
  "externalId": "bjensen"
}
```

**Success Response** (201 Created):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "id": "2819c223-7f76-453a-919d-413861904646",
  "externalId": "bjensen",
  "userName": "bjensen@example.com",
  "name": {
    "familyName": "Jensen",
    "givenName": "Barbara",
    "formatted": "Ms. Barbara J Jensen, III"
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
    "location": "https://gateway.com/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"a330bc81f0671071\""
  }
}
```

**Error Response** (409 Conflict - Duplicate userName):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 409,
  "scimType": "uniqueness",
  "detail": "User with userName 'bjensen@example.com' already exists."
}
```

**Validation Rules**:
- `userName` is REQUIRED and must be unique
- `userName` format: RFC 5322 email address
- `externalId` is OPTIONAL but recommended (for tenant correlation)
- `active` defaults to `true` if not provided
- `emails[primary=true]` must have exactly one primary email

---

### 2.2 Get User

**Endpoint**: `GET /scim/v2/Users/{id}`

**Headers**:
```
Authorization: Bearer {access_token}
Accept: application/scim+json
```

**Success Response** (200 OK):
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
  "groups": [
    {
      "value": "e9e30844-f356-4344-b7c1-b9602b88e457",
      "display": "Sales Team",
      "$ref": "https://gateway.com/scim/v2/Groups/e9e30844-f356-4344-b7c1-b9602b88e457"
    }
  ],
  "meta": {
    "resourceType": "User",
    "created": "2011-08-01T21:32:56.670Z",
    "lastModified": "2011-08-01T21:32:56.670Z",
    "location": "https://gateway.com/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"a330bc81f0671071\""
  }
}
```

**Error Response** (404 Not Found):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 404,
  "detail": "User with ID '2819c223-7f76-453a-919d-413861904646' not found."
}
```

---

### 2.3 Update User (PUT - Full Replacement)

**Endpoint**: `PUT /scim/v2/Users/{id}`

**Headers**:
```
Content-Type: application/scim+json
Authorization: Bearer {access_token}
If-Match: W/"a330bc81f0671071"
```

**Request Body**:
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "id": "2819c223-7f76-453a-919d-413861904646",
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
    },
    {
      "value": "bjensen.personal@example.com",
      "type": "home",
      "primary": false
    }
  ],
  "active": true
}
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "id": "2819c223-7f76-453a-919d-413861904646",
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
    },
    {
      "value": "bjensen.personal@example.com",
      "type": "home",
      "primary": false
    }
  ],
  "active": true,
  "meta": {
    "resourceType": "User",
    "created": "2011-08-01T21:32:56.670Z",
    "lastModified": "2011-08-01T22:15:30.123Z",
    "location": "https://gateway.com/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"b441ce92g1781182\""
  }
}
```

**Error Response** (409 Conflict - Version Mismatch):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 409,
  "detail": "Version mismatch. Resource has been modified."
}
```

---

### 2.4 Patch User (PATCH - Partial Update)

**Endpoint**: `PATCH /scim/v2/Users/{id}`

**Headers**:
```
Content-Type: application/scim+json
Authorization: Bearer {access_token}
If-Match: W/"a330bc81f0671071"
```

**Request Body** (Replace Operation):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "replace",
      "path": "active",
      "value": false
    },
    {
      "op": "replace",
      "path": "emails[type eq \"work\"].value",
      "value": "bjensen.new@example.com"
    }
  ]
}
```

**Request Body** (Add Operation):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "add",
      "path": "emails",
      "value": {
        "value": "bjensen.work2@example.com",
        "type": "work"
      }
    }
  ]
}
```

**Request Body** (Remove Operation):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "remove",
      "path": "phoneNumbers[type eq \"mobile\"]"
    }
  ]
}
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
  "id": "2819c223-7f76-453a-919d-413861904646",
  "userName": "bjensen@example.com",
  "active": false,
  "emails": [
    {
      "value": "bjensen.new@example.com",
      "type": "work",
      "primary": true
    }
  ],
  "meta": {
    "resourceType": "User",
    "created": "2011-08-01T21:32:56.670Z",
    "lastModified": "2011-08-01T23:45:12.890Z",
    "location": "https://gateway.com/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
    "version": "W/\"c552df03h2891293\""
  }
}
```

**Error Response** (400 Bad Request - Invalid Path):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 400,
  "scimType": "invalidPath",
  "detail": "Path 'emails[type eq \"invalid\"]' does not exist."
}
```

**Supported PATCH Operations**:
- `add`: Add attribute or array element
- `remove`: Remove attribute or array element
- `replace`: Replace attribute value

**Path Expressions**:
- `active` - Top-level attribute
- `name.familyName` - Nested attribute
- `emails[type eq "work"]` - Array filter
- `emails[primary eq true].value` - Array filter with sub-attribute

---

### 2.5 Delete User

**Endpoint**: `DELETE /scim/v2/Users/{id}`

**Headers**:
```
Authorization: Bearer {access_token}
```

**Success Response** (204 No Content):
```
(Empty body)
```

**Error Response** (404 Not Found):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 404,
  "detail": "User with ID '2819c223-7f76-453a-919d-413861904646' not found."
}
```

---

### 2.6 List Users (with Filtering)

**Endpoint**: `GET /scim/v2/Users?filter={filter}&startIndex={start}&count={count}`

**Headers**:
```
Authorization: Bearer {access_token}
Accept: application/scim+json
```

**Query Parameters**:
- `filter` (optional): SCIM filter expression
- `sortBy` (optional): Attribute to sort by
- `sortOrder` (optional): `ascending` or `descending`
- `startIndex` (optional): Starting index (1-based, default: 1)
- `count` (optional): Max results per page (default: 100)
- `attributes` (optional): Comma-separated list of attributes to return
- `excludedAttributes` (optional): Comma-separated list of attributes to exclude

**Example Request**:
```
GET /scim/v2/Users?filter=active eq true and userType eq "Employee"&startIndex=1&count=10&sortBy=userName&sortOrder=ascending
```

**Success Response** (200 OK):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "totalResults": 245,
  "startIndex": 1,
  "itemsPerPage": 10,
  "Resources": [
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
      "id": "2819c223-7f76-453a-919d-413861904646",
      "userName": "bjensen@example.com",
      "name": {
        "familyName": "Jensen",
        "givenName": "Barbara"
      },
      "displayName": "Barbara Jensen",
      "active": true,
      "meta": {
        "resourceType": "User",
        "created": "2011-08-01T21:32:56.670Z",
        "lastModified": "2011-08-01T21:32:56.670Z",
        "location": "https://gateway.com/scim/v2/Users/2819c223-7f76-453a-919d-413861904646",
        "version": "W/\"a330bc81f0671071\""
      }
    },
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
      "id": "a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
      "userName": "jsmith@example.com",
      "name": {
        "familyName": "Smith",
        "givenName": "John"
      },
      "displayName": "John Smith",
      "active": true,
      "meta": {
        "resourceType": "User",
        "created": "2011-08-02T10:15:32.890Z",
        "lastModified": "2011-08-02T10:15:32.890Z",
        "location": "https://gateway.com/scim/v2/Users/a4d29ad1-d471-4d00-a6b8-07a27e6742f0",
        "version": "W/\"b441ce92g1781182\""
      }
    }
  ]
}
```

**Filter Examples**:
```
# Find user by userName
filter=userName eq "bjensen@example.com"

# Find active employees
filter=active eq true and userType eq "Employee"

# Find users with family name containing "Smith"
filter=name.familyName co "Smith"

# Find users with email starting with "john"
filter=emails.value sw "john"

# Find users modified after date
filter=meta.lastModified gt "2011-05-13T04:58:34Z"

# Find users with externalId present
filter=externalId pr

# Complex filter
filter=(userName eq "bjensen@example.com") or (emails.value sw "bjensen")
```

---

## 3. Enterprise User Extension

**Schema**: `urn:ietf:params:scim:schemas:extension:enterprise:2.0:User`

**Request Body** (Create with Enterprise Extension):
```json
{
  "schemas": [
    "urn:ietf:params:scim:schemas:core:2.0:User",
    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"
  ],
  "userName": "jsmith@example.com",
  "name": {
    "familyName": "Smith",
    "givenName": "John"
  },
  "emails": [
    {
      "value": "jsmith@example.com",
      "type": "work",
      "primary": true
    }
  ],
  "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User": {
    "employeeNumber": "701984",
    "costCenter": "4130",
    "organization": "Sales",
    "division": "APAC",
    "department": "Field Sales",
    "manager": {
      "value": "26118915-6090-4610-87e4-49d8ca9f808d",
      "displayName": "Jane Manager",
      "$ref": "https://gateway.com/scim/v2/Users/26118915-6090-4610-87e4-49d8ca9f808d"
    }
  }
}
```

**Enterprise Extension Attributes**:
- `employeeNumber`: Employee ID
- `costCenter`: Cost center code
- `organization`: Organization name
- `division`: Division name
- `department`: Department name
- `manager`: Reference to manager (User ID)

---

## 4. Authentication & Authorization

### 4.1 OAuth 2.0 Bearer Token

All endpoints require Bearer token authentication:

```
Authorization: Bearer {access_token}
```

**Token Validation**:
1. Token must be issued by Entra ID for this tenant
2. Token must be valid (not expired)
3. Token must have SCIM provisioning scope: `SCIM.Provisioning`
4. Token claims must include:
   - `oid` (Object ID): Service principal ID
   - `tid` (Tenant ID): Tenant ID
   - `iss` (Issuer): `https://sts.windows.net/{tenantId}/`

### 4.2 Tenant Isolation

All operations are scoped to the authenticated tenant:
- User IDs are scoped per tenant (multi-tenant architecture)
- Cross-tenant queries return empty results
- Tenant ID is extracted from Bearer token `tid` claim

### 4.3 Rate Limiting

**Rate Limits**:
- 1000 requests per minute per tenant
- 429 (Too Many Requests) with `Retry-After` header on limit exceeded

**Response Headers**:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 945
X-RateLimit-Reset: 1638360000
```

---

## 5. Error Responses

### 5.1 Standard Error Schema

```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 400,
  "scimType": "invalidSyntax",
  "detail": "Request body is malformed JSON."
}
```

### 5.2 Error Codes

| HTTP Status | scimType | Description |
|-------------|----------|-------------|
| 400 | invalidSyntax | Malformed request body |
| 400 | invalidFilter | Invalid filter expression |
| 400 | invalidPath | PATCH path doesn't exist |
| 401 | - | Missing or invalid Bearer token |
| 403 | - | Authenticated but no permission |
| 404 | - | Resource not found |
| 409 | uniqueness | Duplicate userName |
| 409 | - | Version mismatch (concurrency) |
| 412 | - | Required If-Match header missing |
| 422 | - | Validation failed |
| 429 | tooMany | Rate limit exceeded |
| 500 | serverUnavailable | Internal server error |
| 501 | - | Feature not implemented |

---

## 6. Audit Logging

All operations are logged to Application Insights:

**Log Entry Structure**:
```json
{
  "timestamp": "2025-11-22T10:00:00.000Z",
  "tenantId": "tenant-123",
  "actorId": "service-principal-456",
  "operationType": "CREATE_USER",
  "resourceType": "USER",
  "resourceId": "2819c223-7f76-453a-919d-413861904646",
  "userName": "bjensen@example.com",
  "httpStatus": 201,
  "responseTimeMs": 1250,
  "requestId": "req-789",
  "details": {
    "externalId": "bjensen",
    "active": true
  }
}
```

**PII Redaction**:
- Email addresses: `bjensen@example.com` → `b***n@example.com`
- Phone numbers: `+1-555-0123` → `+1-***-0123`
- Addresses: Full redaction → `[REDACTED]`

---

## 7. Versioning & Concurrency

### 7.1 Optimistic Concurrency

Use `If-Match` header with version tag:

```
If-Match: W/"a330bc81f0671071"
```

**Version Mismatch Response** (409 Conflict):
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": 409,
  "detail": "Version mismatch. Resource has been modified by another client."
}
```

### 7.2 Version Tags

Version tags are included in `meta.version`:

```json
{
  "meta": {
    "version": "W/\"a330bc81f0671071\""
  }
}
```

---

## 8. Performance Requirements

| Metric | Target |
|--------|--------|
| Latency (p95) | < 2 seconds |
| Throughput | 1000 req/s per deployment unit |
| Availability | 99.9% SLA |
| Error Rate | < 1% |

---

## 9. Contract Tests

### 9.1 Create User Test

```gherkin
Scenario: Create a new user
  Given I have a valid Bearer token
  When I POST to /scim/v2/Users with:
    """
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
      "userName": "test@example.com",
      "name": {"familyName": "Test", "givenName": "User"},
      "active": true
    }
    """
  Then the response status should be 201
  And the response should contain "id"
  And the response should contain "meta.created"
  And the response should contain "meta.version"
```

### 9.2 Filter Users Test

```gherkin
Scenario: Filter users by userName
  Given I have created user "bjensen@example.com"
  When I GET /scim/v2/Users?filter=userName eq "bjensen@example.com"
  Then the response status should be 200
  And "totalResults" should be 1
  And "Resources[0].userName" should be "bjensen@example.com"
```

---

**Next**: See `scim-group-endpoints.md` for Group endpoint specifications.

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
