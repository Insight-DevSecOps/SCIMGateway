# Tasks: Extensible SCIM Gateway SDK/Framework

**Input**: Design documents from `/specs/001-core-scim-framework/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- All file paths are from repository root

---

## Phase 1: Setup (Shared Infrastructure) ✅ COMPLETE

**Purpose**: Project initialization and basic structure

- [x] T001 Create project structure per plan.md (.NET 8+ or Node.js 20+ LTS project)
- [x] T002 Initialize .NET 8+ project with ASP.NET Core Web API template or Node.js project with Express.js
- [x] T003 [P] Configure linting (ESLint for Node.js or .editorconfig for .NET)
- [x] T004 [P] Configure formatting tools (Prettier for Node.js or dotnet format for .NET)
- [x] T005 [P] Setup xUnit test project (.NET) or Jest test framework (Node.js) in tests/
- [x] T006 [P] Setup CI/CD pipeline skeleton (GitHub Actions or Azure DevOps)
- [x] T007 Create .gitignore with appropriate exclusions (bin/, obj/, node_modules/, .env)
- [x] T008 Create README.md with project overview and setup instructions
- [x] T009 [P] Setup dependency injection container (built-in ASP.NET Core or inversify for Node.js)
- [x] T010 [P] Configure Azure SDK for .NET or @azure/identity for Node.js

**Completion**: November 25, 2025 | Commit: 6bf4fb2

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### 2A: Contract Tests (Test-First per Constitution) ✅ COMPLETE

- [x] T011a Contract test for AdapterConfiguration model in tests/Contract/AdapterConfigurationTests.cs (validate required fields: credentials, endpoints, transformation rules, rate limits, timeouts, retry policy, verify schema)
- [x] T012a Contract test for KeyVaultManager in tests/Contract/KeyVaultManagerTests.cs (mock Azure SDK, verify managed identity authentication, credential retrieval)
- [x] T013a Contract test for BearerTokenValidator in tests/Contract/BearerTokenValidatorTests.cs (OAuth 2.0 token validation per RFC, verify claims validation, signature verification, expired token rejection)
- [x] T014a Contract test for TenantResolver in tests/Contract/TenantResolverTests.cs (verify tid extraction, enforce tenant isolation, cross-tenant access rejection)
- [x] T015a Contract test for Application Insights SDK integration in tests/Contract/ApplicationInsightsTests.cs (verify telemetry event schema, custom events for CRUD operations)
- [x] T016a Contract test for AuditLogger in tests/Contract/AuditLoggerTests.cs (verify log structure: timestamp, actor, operation, resource ID, old/new values, status, errors)
- [x] T017a Contract test for RateLimiter in tests/Contract/RateLimiterTests.cs (verify token bucket algorithm, per-tenant limits, lock after N failures)
- [x] T018a Contract test for ScimUser model in tests/Contract/ScimUserModelTests.cs (validate RFC 7643 User schema compliance, required attributes, optional attributes, internal attributes)
- [x] T019a Contract test for ScimGroup model in tests/Contract/ScimGroupModelTests.cs (validate RFC 7643 Group schema compliance, members array, internal attributes)
- [x] T020a Contract test for EntitlementMapping model in tests/Contract/EntitlementMappingTests.cs (validate required fields: providerId, providerEntitlementId, name, type, mappedGroups, priority)
- [x] T021a Contract test for SyncState model in tests/Contract/SyncStateTests.cs (validate state transitions, required fields: tenantId, providerId, lastSyncTimestamp, syncDirection, lastKnownState)
- [x] T022a Contract test for AuditLogEntry model in tests/Contract/AuditLogEntryTests.cs (validate required fields per FR-011: timestamp, tenantId, actorId, operationType, resourceType, resourceId, httpStatus, responseTimeMs)
- [x] T023a Contract test for SchemaValidator in tests/Contract/SchemaValidatorTests.cs (verify RFC 7643 validation rules, required attributes, email format RFC 5322, multi-valued attributes, rejection of invalid schemas)
- [x] T024a Contract test for ErrorHandler in tests/Contract/ErrorHandlerTests.cs (verify SCIM error response format per RFC 7644, all status codes: 400/401/403/404/409/412/422/429/500/501, scimType mappings)
- [x] T025a Contract test for ResponseFormatter in tests/Contract/ResponseFormatterTests.cs (verify SCIM ListResponse schema, resource location URIs, pagination metadata)
- [x] T026a Contract test for PiiRedactor in tests/Contract/PiiRedactorTests.cs (verify email/phone/address redaction patterns, partial masking, full redaction per GDPR/CCPA)
- [x] T027a Contract test for CosmosDbClient in tests/Contract/CosmosDbClientTests.cs (mock Azure SDK, verify connection with managed identity, database/container references)
- [x] T028a Contract test for Cosmos DB schema in tests/Contract/CosmosDbSchemaTests.cs (validate partition keys /tenantId, indexing policies, TTL configuration for audit-logs container)
- [x] T029a Contract test for ConnectionPool in tests/Contract/ConnectionPoolTests.cs (verify HTTP client pooling, per-adapter management, connection reuse, timeout handling)
- [x] T030a Contract test for RequestHandler in tests/Contract/RequestHandlerTests.cs (verify SCIM request parsing, routing logic, tenant extraction, schema validation)
- [x] T031a Contract test for FilterParser in tests/Contract/FilterParserTests.cs (verify all 11 SCIM filter operators: eq, ne, co, sw, ew, pr, gt, ge, lt, le, and, or, not per RFC 7644)
- [x] T032a Contract test for AuthenticationMiddleware in tests/Contract/AuthenticationMiddlewareTests.cs (verify token validation, tenant extraction, tenant isolation enforcement, rate limiting)
- [x] T033a Contract test for AuditMiddleware in tests/Contract/AuditMiddlewareTests.cs (verify all CRUD operations captured, log to Application Insights with full context)

**Completion**: November 25, 2025 | 23 contract tests created, all compiling

**Checkpoint**: All contract tests created - ready for Phase 2B implementation

### 2B: Implementation (Only After Tests Pass)

- [ ] T011 Create AdapterConfiguration model in src/Configuration/AdapterConfiguration.cs (credentials, endpoints, transformation rules, rate limits, timeouts, retry policy) [depends on T011a passing]
- [ ] T012 [P] Setup Azure Key Vault integration in src/Configuration/KeyVaultManager.cs (managed identity authentication, credential retrieval) [depends on T012a passing]
- [ ] T013 [P] Implement BearerTokenValidator in src/Authentication/BearerTokenValidator.cs (OAuth 2.0 token validation per Microsoft SCIM spec, signature verification, claims validation) [depends on T013a passing]
- [ ] T014 [P] Implement TenantResolver in src/Authentication/TenantResolver.cs (extract tid claim from token, enforce tenant isolation) [depends on T014a passing]
- [ ] T015 [P] Setup Application Insights SDK in src/Core/AuditLogger.cs (telemetry client initialization, custom events for CRUD operations) [depends on T015a passing]
- [ ] T016 [P] Implement AuditLogger in src/Core/AuditLogger.cs (log timestamp, actor from oid claim, operation type, resource ID, old/new values, status, errors to Application Insights) [depends on T016a passing]
- [ ] T017 [P] Implement RateLimiter in src/Authentication/RateLimiter.cs (token bucket algorithm for failed auth attempts, per-tenant limits) [depends on T017a passing]
- [ ] T018 [P] Create ScimUser model in src/Models/ScimUser.cs per RFC 7643 (id, userName, displayName, name, emails, phoneNumbers, active, groups, roles, externalId, meta, plus internal: tenantId, adapterId, syncState) [depends on T018a passing]
- [ ] T019 [P] Create ScimGroup model in src/Models/ScimGroup.cs per RFC 7643 (id, displayName, members, description, externalId, meta, plus internal: tenantId, providerMappings, entitlementMapping) [depends on T019a passing]
- [ ] T020 [P] Create EntitlementMapping model in src/Models/EntitlementMapping.cs (providerId, providerEntitlementId, name, type, mappedGroups, priority) [depends on T020a passing]
- [ ] T021 [P] Create SyncState model in src/Models/SyncState.cs (id, tenantId, providerId, lastSyncTimestamp, syncDirection, lastKnownState, driftLog, conflictLog, errorLog) [depends on T021a passing]
- [ ] T022 [P] Create AuditLogEntry model in src/Models/AuditLogEntry.cs (timestamp, tenantId, actorId, actorType, operationType, resourceType, resourceId, userName, httpStatus, responseTimeMs, requestId, adapterId, oldValue, newValue, errorCode, errorMessage) [depends on T022a passing]
- [ ] T023 Implement SchemaValidator in src/Core/SchemaValidator.cs (validate SCIM User/Group schemas per RFC 7643, required attributes, email format RFC 5322, multi-valued attributes) [depends on T023a passing]
- [ ] T024 [P] Implement ErrorHandler in src/Core/ErrorHandler.cs (translate exceptions to SCIM error responses per RFC 7644, status codes 400/401/403/404/409/412/422/429/500/501) [depends on T024a passing]
- [ ] T025 [P] Implement ResponseFormatter in src/Core/ResponseFormatter.cs (format SCIM responses per RFC 7643, ListResponse schema, resource location URIs) [depends on T025a passing]
- [ ] T026 [P] Implement PiiRedactor in src/Utilities/PiiRedactor.cs (redact email partial mask, phone partial mask, address full redaction per GDPR/CCPA) [depends on T026a passing]
- [ ] T027 [P] Setup Azure Cosmos DB client in src/Configuration/CosmosDbClient.cs (connection with managed identity, database/container references for sync-state, users, groups, transformation-rules, audit-logs) [depends on T027a passing]
- [ ] T028 [P] Create Cosmos DB schema per contracts/cosmos-db-schema.md (5 containers with /tenantId partition key, indexing policies, TTL for audit-logs: 90 days minimum configurable per tenant per FR-016a, other containers: -1 never expire) [depends on T028a passing]
- [ ] T029 [P] Implement ConnectionPool in src/Utilities/ConnectionPool.cs (HTTP client pooling for provider API calls, per-adapter connection management) [depends on T029a passing]
- [ ] T030 Create RequestHandler in src/Core/RequestHandler.cs (parse SCIM requests, route to appropriate handler, extract tenant from token, validate schema) [depends on T030a passing]
- [ ] T031 [P] Implement FilterParser in src/Utilities/FilterParser.cs (parse SCIM filter expressions per RFC 7644, support eq/ne/co/sw/ew/pr/gt/ge/lt/le/and/or/not operators) [depends on T031a passing]
- [ ] T032 [P] Setup authentication middleware in src/Core/Middleware/AuthenticationMiddleware.cs (validate Bearer token on all requests, extract tenant/actor, enforce tenant isolation, rate limit failures) [depends on T032a passing]
- [ ] T033 [P] Setup audit logging middleware in src/Core/Middleware/AuditMiddleware.cs (capture all CRUD operations, log to Application Insights with full context) [depends on T033a passing]

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Core SCIM Framework Foundation (Priority: P1) 🎯 MVP

**Goal**: Users and groups can be created, read, updated, deleted via SCIM endpoints per RFC 7643/7644

**Independent Test**: All SCIM User and Group endpoints operational, schema validation working, errors returned in SCIM format, all operations logged to Application Insights

### Implementation for User Story 1

- [ ] T034 [P] [US1] Implement POST /scim/v2/Users endpoint in src/Core/Endpoints/UsersEndpoint.cs (create user with SCIM validation per contracts/scim-user-endpoints.md, return 201 Created)
- [ ] T035 [P] [US1] Implement GET /scim/v2/Users/{id} endpoint in src/Core/Endpoints/UsersEndpoint.cs (retrieve user by ID, return SCIM format with groups array, 404 if not found)
- [ ] T036 [P] [US1] Implement PUT /scim/v2/Users/{id} endpoint in src/Core/Endpoints/UsersEndpoint.cs (full replacement with If-Match header for optimistic concurrency, return 200 OK, 409 version mismatch)
- [ ] T037 [P] [US1] Implement PATCH /scim/v2/Users/{id} endpoint in src/Core/Endpoints/UsersEndpoint.cs (partial update with JSON Patch operations per RFC 6902, support add/remove/replace ops, path expressions)
- [ ] T038 [P] [US1] Implement DELETE /scim/v2/Users/{id} endpoint in src/Core/Endpoints/UsersEndpoint.cs (delete user, return 204 No Content)
- [ ] T039 [P] [US1] Implement GET /scim/v2/Users?filter=... endpoint in src/Core/Endpoints/UsersEndpoint.cs (list users with filtering, pagination startIndex/count, sorting sortBy/sortOrder, return ListResponse schema)
- [ ] T040 [P] [US1] Implement POST /scim/v2/Groups endpoint in src/Core/Endpoints/GroupsEndpoint.cs (create group with SCIM validation per contracts/scim-group-endpoints.md, return 201 Created)
- [ ] T041 [P] [US1] Implement GET /scim/v2/Groups/{id} endpoint in src/Core/Endpoints/GroupsEndpoint.cs (retrieve group by ID with members array, 404 if not found)
- [ ] T042 [P] [US1] Implement PUT /scim/v2/Groups/{id} endpoint in src/Core/Endpoints/GroupsEndpoint.cs (full replacement with If-Match header, return 200 OK)
- [ ] T043 [P] [US1] Implement PATCH /scim/v2/Groups/{id} endpoint in src/Core/Endpoints/GroupsEndpoint.cs (partial update for members add/remove operations)
- [ ] T044 [P] [US1] Implement DELETE /scim/v2/Groups/{id} endpoint in src/Core/Endpoints/GroupsEndpoint.cs (delete group, return 204 No Content)
- [ ] T045 [P] [US1] Implement GET /scim/v2/Groups?filter=... endpoint in src/Core/Endpoints/GroupsEndpoint.cs (list groups with filtering and pagination)
- [ ] T046 [US1] Implement Enterprise User Extension in src/Core/Extensions/EnterpriseUserExtension.cs (urn:ietf:params:scim:schemas:extension:enterprise:2.0:User with employeeNumber, costCenter, organization, division, department, manager reference)
- [ ] T047 [US1] Implement SCIM error response generation in src/Core/ErrorHandler.cs (standard error schema with status/scimType/detail, 11 error code mappings per contracts)
- [ ] T048 [US1] Add validation for User operations in src/Core/SchemaValidator.cs (userName required/unique, email format RFC 5322, active boolean, displayName string)
- [ ] T049 [US1] Add validation for Group operations in src/Core/SchemaValidator.cs (displayName required/unique, members array with value/type/display)
- [ ] T050 [US1] Implement versioning/concurrency control in src/Core/ConcurrencyManager.cs (optimistic concurrency with If-Match header, meta.version increment, 409 conflict on mismatch)
- [ ] T051 [US1] Add User endpoint audit logging in src/Core/AuditLogger.cs (log all User CRUD operations with PII redaction per contracts/scim-user-endpoints.md)
- [ ] T052 [US1] Add Group endpoint audit logging in src/Core/AuditLogger.cs (log all Group CRUD operations)

### Tests for User Story 1

- [ ] T053 [P] [US1] Contract test for POST /Users in tests/Contract/ScimUserEndpointTests.cs (verify RFC 7643 compliance, required attributes, 201 Created response)
- [ ] T054 [P] [US1] Contract test for GET /Users/{id} in tests/Contract/ScimUserEndpointTests.cs (verify response schema, groups array, 404 handling)
- [ ] T055 [P] [US1] Contract test for PUT /Users/{id} in tests/Contract/ScimUserEndpointTests.cs (verify If-Match header, version increment, 409 version mismatch)
- [ ] T056 [P] [US1] Contract test for PATCH /Users/{id} in tests/Contract/ScimUserEndpointTests.cs (verify JSON Patch operations, path expressions, 400 invalid path)
- [ ] T057 [P] [US1] Contract test for DELETE /Users/{id} in tests/Contract/ScimUserEndpointTests.cs (verify 204 No Content, 404 not found)
- [ ] T058 [P] [US1] Contract test for GET /Users?filter=... in tests/Contract/ScimUserEndpointTests.cs (verify 11 filter expressions, pagination, sorting)
- [ ] T059 [P] [US1] Contract test for POST /Groups in tests/Contract/ScimGroupEndpointTests.cs (verify RFC 7643 compliance, displayName required/unique, 201 Created)
- [ ] T060 [P] [US1] Contract test for GET /Groups/{id} in tests/Contract/ScimGroupEndpointTests.cs (verify members array, 404 handling)
- [ ] T061 [P] [US1] Contract test for PATCH /Groups/{id} in tests/Contract/ScimGroupEndpointTests.cs (verify member add/remove operations)
- [ ] T062 [P] [US1] Integration test for User lifecycle in tests/Integration/UserLifecycleTests.cs (create → read → update → delete flow, verify audit logs)
- [ ] T063 [P] [US1] Integration test for Group lifecycle in tests/Integration/GroupLifecycleTests.cs (create group → add members → remove members → delete)
- [ ] T064 [P] [US1] Security test for authentication in tests/Security/TokenValidationTests.cs (missing token → 401, invalid token → 401, expired token → 401)
- [ ] T065 [P] [US1] Security test for tenant isolation in tests/Security/TenantIsolationTests.cs (verify cross-tenant access impossible, tenant filter on all queries)

**Checkpoint**: At this point, User Story 1 should be fully functional - all SCIM endpoints operational, schema validation working, audit logging complete

---

## Phase 4: User Story 2 - Adapter Pattern for Multi-SaaS Integration (Priority: P1) 🎯 MVP

**Goal**: Adapter interface defined, mock adapter works, SDK routes requests to adapters correctly

**Independent Test**: Define adapter interface contract, implement mock adapter, verify core SDK calls adapter methods correctly for each CRUD operation. Can be tested independently via contract tests.

### Implementation for User Story 2

- [ ] T066 [P] [US2] Define IAdapter interface in src/Adapters/IAdapter.cs (18 methods per contracts/adapter-interface.md: CreateUserAsync, GetUserAsync, UpdateUserAsync, DeleteUserAsync, ListUsersAsync, CreateGroupAsync, GetGroupAsync, UpdateGroupAsync, DeleteGroupAsync, ListGroupsAsync, AddUserToGroupAsync, RemoveUserFromGroupAsync, GetGroupMembersAsync, MapGroupToEntitlementAsync, MapEntitlementToGroupAsync, CheckHealthAsync, GetCapabilities)
- [ ] T067 [P] [US2] Create AdapterConfiguration type in src/Adapters/AdapterConfiguration.cs (apiBaseUrl, credentials, transformationRules, rateLimiting, timeouts, retryPolicy)
- [ ] T068 [P] [US2] Create QueryFilter type in src/Adapters/QueryFilter.cs (filter, attributes, sortBy, sortOrder, startIndex, count)
- [ ] T069 [P] [US2] Create PagedResult<T> type in src/Adapters/PagedResult.cs (resources, totalResults, startIndex, itemsPerPage)
- [ ] T070 [P] [US2] Create AdapterHealthStatus type in src/Adapters/AdapterHealthStatus.cs (status, connectivity, auth, rateLimits, responseTime, errorRate)
- [ ] T071 [P] [US2] Create AdapterCapabilities type in src/Adapters/AdapterCapabilities.cs (feature support flags: supportsGroups, supportsRoles, supportsOrgHierarchy, etc.)
- [ ] T072 [P] [US2] Create AdapterException in src/Adapters/AdapterException.cs (with ScimErrorType mapping: InvalidSyntax, Uniqueness, Mutability, InvalidFilter, NoTarget, TooMany, ServerUnavailable)
- [ ] T073 [P] [US2] Create AdapterBase abstract class in src/Adapters/AdapterBase.cs (common helper methods: GetAccessTokenAsync, TranslateError, LogOperation)
- [ ] T074 [US2] Implement AdapterRegistry in src/Adapters/AdapterRegistry.cs (register adapters per provider, route requests to correct adapter based on tenantId/providerId configuration)
- [ ] T075 [US2] Implement AdapterContext in src/Adapters/AdapterContext.cs (utilities provided to adapters: logging, error handling, schema validation, tenant info)
- [ ] T076 [US2] Create MockAdapter in src/Adapters/Providers/MockAdapter.cs for testing (implement all IAdapter methods with in-memory storage, no external dependencies)
- [ ] T077 [US2] Implement adapter routing logic in src/Core/RequestHandler.cs (extract providerId from request/config, call AdapterRegistry.GetAdapter, invoke adapter method)
- [ ] T078 [US2] Implement adapter error translation in src/Core/ErrorHandler.cs (translate AdapterException to SCIM error response, preserve provider error context)
- [ ] T079 [US2] Add adapter operation logging in src/Core/AuditLogger.cs (log adapter method calls, provider responses, errors with adapterId context)

### Tests for User Story 2

- [ ] T080 [P] [US2] Contract test for IAdapter interface in tests/Contract/AdapterInterfaceTests.cs (verify all 18 required methods present, correct signatures, async patterns)
- [ ] T081 [P] [US2] Unit test for AdapterRegistry in tests/Unit/AdapterRegistryTests.cs (register multiple adapters, route to correct adapter, handle adapter not found)
- [ ] T082 [P] [US2] Unit test for AdapterBase in tests/Unit/AdapterBaseTests.cs (test helper methods, error translation)
- [ ] T083 [P] [US2] Integration test for MockAdapter in tests/Integration/MockAdapterTests.cs (verify all CRUD operations work, error handling, health checks)
- [ ] T084 [P] [US2] Integration test for adapter routing in tests/Integration/AdapterRoutingTests.cs (SDK routes User operations to adapter, Group operations to adapter, verify adapter methods called with correct parameters)
- [ ] T085 [P] [US2] Integration test for adapter error handling in tests/Integration/AdapterErrorTests.cs (adapter throws exception, SDK translates to SCIM error, audit log captures error)

**Checkpoint**: At this point, adapter pattern should work - MockAdapter registered, SDK routes requests correctly, adapter methods invoked, responses translated to SCIM format

---

## Phase 5: User Story 3 - Group/Entitlement Transformation Engine (Priority: P1) 🎯 MVP

**Goal**: Group/entitlement transformation rules defined, engine applies transformations, reverse transforms work

**Independent Test**: Define transformation rule, group operation is transformed per rule, reverse transformation recovers original group representation. Can be tested with unit and contract tests.

### Implementation for User Story 3

- [ ] T086 [P] [US3] Create TransformationRule model in src/Transformations/TransformationRule.cs (id, tenantId, providerId, ruleType: EXACT/REGEX/HIERARCHICAL/CONDITIONAL, sourcePattern, targetMapping, priority, enabled, conflictResolution, examples per contracts/transformation-engine.md)
- [ ] T087 [P] [US3] Create Entitlement model in src/Transformations/Entitlement.cs (providerEntitlementId, name, type: ROLE/PERMISSION_SET/PROFILE/ORG_UNIT/GROUP, mappedGroups, priority, metadata)
- [ ] T088 [P] [US3] Create TransformationExample model in src/Transformations/TransformationExample.cs (input, expectedOutput, passed)
- [ ] T089 [P] [US3] Create TransformationTestResult model in src/Transformations/TransformationTestResult.cs (allPassed, results array with input/expectedOutput/actualOutput/passed/errorMessage)
- [ ] T090 [P] [US3] Define ITransformationEngine interface in src/Transformations/ITransformationEngine.cs (TransformGroupToEntitlementsAsync, TransformEntitlementToGroupsAsync, GetRulesAsync, CreateRuleAsync, UpdateRuleAsync, DeleteRuleAsync, TestRuleAsync, ResolveConflictsAsync)
- [ ] T091 [US3] Implement TransformationEngine in src/Transformations/TransformationEngine.cs (load rules from Cosmos DB, match group displayName to rules by priority, apply transformation, handle conflicts)
- [ ] T092 [US3] Implement EXACT pattern matching in src/Transformations/TransformationEngine.cs (case-sensitive string match, direct mapping)
- [ ] T093 [US3] Implement REGEX pattern matching in src/Transformations/TransformationEngine.cs (regex pattern with capture groups, template variable substitution ${1}, ${2}, pre-compile regex for performance)
- [ ] T094 [US3] Implement HIERARCHICAL pattern matching in src/Transformations/TransformationEngine.cs (path parsing Company/Division/Department/Team, level extraction ${level0}, ${level1}, etc.)
- [ ] T095 [US3] Implement CONDITIONAL pattern matching in src/Transformations/TransformationEngine.cs (conditional logic based on group name patterns, metadata evaluation)
- [ ] T096 [US3] Implement reverse transformation in src/Transformations/ReverseTransform.cs (entitlement → group mapping for drift detection, reverse-engineer input from template)
- [ ] T097 [US3] Implement conflict resolution strategies in src/Transformations/TransformationEngine.cs (UNION: assign all, FIRST_MATCH: priority-based, HIGHEST_PRIVILEGE: privilege ranking, MANUAL_REVIEW: flag for admin, ERROR: fail operation)
- [ ] T098 [US3] Implement transformation rule validation in src/Transformations/TransformationEngine.cs (syntax validation, conflict detection, missing mapping checks, regex compilation errors)
- [ ] T099 [US3] Implement transformation rule caching in src/Transformations/TransformationEngine.cs (in-memory cache, 5-minute refresh from Cosmos DB)
- [ ] T100 [US3] Create transformation rule storage in Cosmos DB transformation-rules container (CRUD operations, query by tenantId/providerId/enabled/priority)
- [ ] T101 [US3] Create example transformation rules in src/Transformations/Providers/SalesforceTransform.cs (Salesforce: group→role mapping patterns for Sales_Representative, Sales_Manager, etc.)
- [ ] T102 [P] [US3] Create example transformation rules in src/Transformations/Providers/WorkdayTransform.cs (Workday: group→org_hierarchy patterns with level extraction)
- [ ] T103 [P] [US3] Create example transformation rules in src/Transformations/Providers/ServiceNowTransform.cs (ServiceNow: group→group direct mapping, native group support)
- [ ] T104 [US3] Integrate transformation engine with adapter calls in src/Core/RequestHandler.cs (apply transformations before calling adapter CreateGroupAsync/AddUserToGroupAsync)
- [ ] T105 [US3] Add transformation operation logging in src/Core/AuditLogger.cs (log rule matches, transformations applied, conflicts detected)

### Tests for User Story 3

- [ ] T106 [P] [US3] Unit test for EXACT pattern in tests/Unit/TransformationEngineTests.cs (verify exact match, case sensitivity, no match on extra text)
- [ ] T107 [P] [US3] Unit test for REGEX pattern in tests/Unit/TransformationEngineTests.cs (verify capture groups, template substitution, pattern mismatch)
- [ ] T108 [P] [US3] Unit test for HIERARCHICAL pattern in tests/Unit/TransformationEngineTests.cs (verify path parsing, level extraction, insufficient levels handling)
- [ ] T109 [P] [US3] Unit test for CONDITIONAL pattern in tests/Unit/TransformationEngineTests.cs (verify condition evaluation, fallback behavior)
- [ ] T110 [P] [US3] Unit test for reverse transformation in tests/Unit/ReverseTransformTests.cs (verify entitlement→group recovery, template reverse-engineering)
- [ ] T111 [P] [US3] Unit test for UNION conflict resolution in tests/Unit/TransformationEngineTests.cs (verify all entitlements assigned)
- [ ] T112 [P] [US3] Unit test for FIRST_MATCH conflict resolution in tests/Unit/TransformationEngineTests.cs (verify priority-based selection)
- [ ] T113 [P] [US3] Unit test for HIGHEST_PRIVILEGE conflict resolution in tests/Unit/TransformationEngineTests.cs (verify privilege ranking)
- [ ] T114 [P] [US3] Contract test for transformation rules in tests/Contract/TransformationRuleTests.cs (verify rule format, required fields, validation)
- [ ] T115 [P] [US3] Integration test for Salesforce transformation in tests/Integration/SalesforceTransformTests.cs (group "Sales-EMEA" → role "Sales_EMEA_Rep", verify adapter receives correct entitlement)
- [ ] T116 [P] [US3] Integration test for Workday transformation in tests/Integration/WorkdayTransformTests.cs (group "Acme Corp/Sales/EMEA" → org "ORG-EMEA", verify hierarchical parsing)
- [ ] T117 [P] [US3] Integration test for transformation preview in tests/Integration/TransformationPreviewTests.cs (POST /api/transform/preview with sample group name, verify returns transformed result without persisting, verify response includes: matchedRuleId, transformedEntitlement, conflicts[], appliedAt=null)

**Checkpoint**: At this point, transformation engine should work - rules loaded, patterns matched, transformations applied, reverse transforms functional

---

## Phase 6: User Story 4 - Bidirectional Sync with Change Detection (Priority: P2)

**Goal**: SDK can poll provider, detect changes, report drift, identify conflicts

**Independent Test**: Poll provider, SDK detects new/modified/deleted users and groups, drift report includes all changes with timestamps. Can be tested with integration tests.

### Implementation for User Story 4

- [ ] T118 [P] [US4] Create DriftReport model in src/SyncEngine/DriftReport.cs (driftId, timestamp, driftType, resourceType, resourceId, details, reconciled)
- [ ] T119 [P] [US4] Create ConflictReport model in src/SyncEngine/ConflictReport.cs (conflictId, timestamp, conflictType, resourceType, resourceId, entraChange, providerChange, resolution, resolved)
- [ ] T120 [P] [US4] Implement SyncState storage in src/SyncEngine/SyncState.cs (create/update/query sync state in Cosmos DB sync-state container per contracts/cosmos-db-schema.md)
- [ ] T121 [US4] Implement ChangeDetector in src/SyncEngine/ChangeDetector.cs (compare current provider state with lastKnownState from Cosmos DB, detect added/modified/deleted users and groups)
- [ ] T122 [US4] Implement drift detection logic in src/SyncEngine/ChangeDetector.cs (identify drift: provider state differs from Entra state, generate drift report with old vs new values)
- [ ] T123 [US4] Implement conflict detection in src/SyncEngine/ChangeDetector.cs (detect dual modification: same resource changed in both Entra and provider, flag for manual review)
- [ ] T124 [US4] Implement drift logging in src/Core/AuditLogger.cs (log all drift detections to Application Insights, capture resource ID, old value, new value, timestamp)
- [ ] T125 [US4] Implement Reconciler in src/SyncEngine/Reconciler.cs (reconciliation strategies: auto-apply based on sync direction, manual review, ignore)
- [ ] T126 [US4] Implement reconciliation strategy AUTO_APPLY in src/SyncEngine/Reconciler.cs (if direction=ENTRA_TO_SAAS, overwrite provider with Entra state; if direction=SAAS_TO_ENTRA, overwrite Entra with provider state)
- [ ] T127 [US4] Implement reconciliation strategy MANUAL_REVIEW in src/SyncEngine/Reconciler.cs (create conflict log entry, notify operations team, block auto-sync for conflicted resource, expose POST /api/drift/{driftId}/reconcile endpoint for admin approval with selected direction)
- [ ] T128 [US4] Implement reconciliation strategy IGNORE in src/SyncEngine/Reconciler.cs (log drift as informational, do not apply changes)
- [ ] T129 [US4] Implement PollingService in src/SyncEngine/PollingService.cs (scheduled polling via timer trigger or cron job, call adapter ListUsersAsync/ListGroupsAsync, invoke ChangeDetector)
- [ ] T130 [US4] Implement sync state snapshot in src/SyncEngine/SyncState.cs (capture snapshotChecksum, snapshotTimestamp, userCount, groupCount after each sync)
- [ ] T131 [US4] Implement error handling for polling failures in src/SyncEngine/PollingService.cs (log adapter unavailable, retry per configured policy, alert operations team)
- [ ] T132 [US4] Add drift report API endpoint GET /api/drift in src/Core/Endpoints/DriftEndpoint.cs (query drift reports by tenantId/providerId/reconciled status, return drift details)
- [ ] T133 [US4] Add conflict report API endpoint GET /api/conflicts in src/Core/Endpoints/DriftEndpoint.cs (query conflicts by tenantId/providerId/resolved status, return conflict details with suggested remediation)

### Tests for User Story 4

- [ ] T134 [P] [US4] Unit test for ChangeDetector in tests/Unit/ChangeDetectorTests.cs (detect user added, user modified, user deleted, group added, group modified, group deleted)
- [ ] T135 [P] [US4] Unit test for drift detection in tests/Unit/ChangeDetectorTests.cs (compare provider state with Entra state, identify drift, generate drift report)
- [ ] T136 [P] [US4] Unit test for conflict detection in tests/Unit/ChangeDetectorTests.cs (detect dual modification, flag for manual review)
- [ ] T137 [P] [US4] Unit test for AUTO_APPLY reconciliation in tests/Unit/ReconcilerTests.cs (verify correct direction enforcement, apply changes)
- [ ] T138 [P] [US4] Unit test for MANUAL_REVIEW reconciliation in tests/Unit/ReconcilerTests.cs (verify conflict log creation, block auto-sync)
- [ ] T139 [P] [US4] Integration test for polling service in tests/Integration/PollingServiceTests.cs (trigger poll, verify adapter called, verify change detection, verify drift report)
- [ ] T140 [P] [US4] Integration test for drift reconciliation in tests/Integration/DriftReconciliationTests.cs (detect drift, apply reconciliation strategy, verify state updated)
- [ ] T141 [P] [US4] Integration test for error handling in tests/Integration/PollingErrorTests.cs (adapter unavailable, verify retry logic, verify error logging, verify operations team alert)

**Checkpoint**: At this point, change detection should work - polling service triggers, adapter returns state, drift detected, drift reports generated, reconciliation strategies applied

---

## Phase 7: User Story 5 - Sync Direction Toggle (Priority: P2)

**Goal**: Sync direction can be toggled between push/pull, direction enforced, toggle logged

**Independent Test**: Switch direction from push to pull, verify next sync pulls from provider; switch back to push, verify next changes push to Entra. Can be tested with integration and configuration tests.

### Implementation for User Story 5

- [ ] T142 [P] [US5] Create SyncDirection enum in src/Models/SyncDirection.cs (ENTRA_TO_SAAS, SAAS_TO_ENTRA)
- [ ] T143 [US5] Implement SyncDirectionManager in src/SyncEngine/SyncDirectionManager.cs (persist direction in Cosmos DB sync-state, load direction on startup, DEFAULT to ENTRA_TO_SAAS if no prior configuration exists, enforce direction during sync, log default direction selection on first startup per FR-041a)
- [ ] T144 [US5] Implement direction enforcement in src/SyncEngine/PollingService.cs (if direction=ENTRA_TO_SAAS, ignore changes from provider; if direction=SAAS_TO_ENTRA, apply changes from provider)
- [ ] T145 [US5] Implement "opposite direction" logging in src/Core/AuditLogger.cs (if change arrives from inactive direction, log as informational, do not apply)
- [ ] T146 [US5] Implement direction toggle API endpoint POST /api/sync-direction in src/Core/Endpoints/SyncDirectionEndpoint.cs (accept new direction, persist to Cosmos DB, log direction change to audit log)
- [ ] T147 [US5] Implement graceful direction toggle in src/SyncEngine/SyncDirectionManager.cs (complete current sync cycle, apply new direction next cycle, avoid partial sync)
- [ ] T148 [US5] Add direction metadata to all sync operations in src/Core/AuditLogger.cs (include syncDirection in audit log entries for traceability)

### Tests for User Story 5

- [ ] T149 [P] [US5] Unit test for SyncDirectionManager in tests/Unit/SyncDirectionManagerTests.cs (persist direction, load direction, enforce direction)
- [ ] T150 [P] [US5] Unit test for direction enforcement in tests/Unit/PollingServiceTests.cs (verify operations apply only for active direction, opposite direction ignored)
- [ ] T151 [P] [US5] Integration test for direction toggle in tests/Integration/DirectionToggleTests.cs (toggle from ENTRA_TO_SAAS to SAAS_TO_ENTRA, verify next sync pulls from provider)
- [ ] T152 [P] [US5] Integration test for graceful toggle in tests/Integration/DirectionToggleTests.cs (toggle during active sync, verify current cycle completes, new direction applies next cycle)
- [ ] T153 [P] [US5] Integration test for "opposite direction" logging in tests/Integration/OppositeDirectionTests.cs (change arrives from inactive direction, verify logged as informational, not applied)

**Checkpoint**: At this point, sync direction toggle should work - direction can be changed, enforcement works, operations logged with direction metadata

---

## Phase 8: Adapters - Example Implementations (Priority: P2)

**Goal**: Adapter implementations for Salesforce, Workday, ServiceNow (example adapters, real-world tested)

**Dependencies**: Requires Phase 2 (Foundational infrastructure) + Phase 4 (IAdapter interface) + Phase 5 (Transformation rules T101-T103) COMPLETE before starting. Phase 5 transformation engine MUST be operational because adapters rely on group→entitlement transformations. Individual adapters (Salesforce, Workday, ServiceNow) can be built in parallel once dependencies are met.

**Independent Test**: Each adapter can be swapped in, end-to-end flows work (create user Entra→provider, detect changes)

### Salesforce Adapter

- [ ] T154 [P] [A1] Implement SalesforceAdapter in src/Adapters/Providers/SalesforceAdapter.cs (implement IAdapter, extend AdapterBase)
- [ ] T155 [P] [A1] Implement CreateUserAsync in src/Adapters/Providers/SalesforceAdapter.cs (call Salesforce REST API POST /services/data/v60.0/sobjects/User, map SCIM user to Salesforce user fields)
- [ ] T156 [P] [A1] Implement GetUserAsync in src/Adapters/Providers/SalesforceAdapter.cs (call Salesforce REST API GET /services/data/v60.0/sobjects/User/{id}, map Salesforce user to SCIM user)
- [ ] T157 [P] [A1] Implement UpdateUserAsync in src/Adapters/Providers/SalesforceAdapter.cs (call Salesforce REST API PATCH /services/data/v60.0/sobjects/User/{id})
- [ ] T158 [P] [A1] Implement DeleteUserAsync in src/Adapters/Providers/SalesforceAdapter.cs (call Salesforce REST API DELETE /services/data/v60.0/sobjects/User/{id})
- [ ] T159 [P] [A1] Implement ListUsersAsync in src/Adapters/Providers/SalesforceAdapter.cs (call Salesforce REST API query endpoint with SOQL, implement pagination)
- [ ] T160 [P] [A1] Implement MapGroupToEntitlementAsync in src/Adapters/Providers/SalesforceAdapter.cs (map SCIM group to Salesforce role using transformation engine, assign role via UserRole object)
- [ ] T161 [P] [A1] Implement Salesforce rate limiting in src/Adapters/Providers/SalesforceAdapter.cs (respect Salesforce-specific rate limits, handle 429 responses)
- [ ] T162 [P] [A1] Implement Salesforce error handling in src/Adapters/Providers/SalesforceAdapter.cs (translate Salesforce errors to AdapterException with SCIM error types)

### Workday Adapter

- [ ] T163 [P] [A2] Implement WorkdayAdapter in src/Adapters/Providers/WorkdayAdapter.cs (implement IAdapter, extend AdapterBase)
- [ ] T164 [P] [A2] Implement CreateUserAsync in src/Adapters/Providers/WorkdayAdapter.cs (call Workday REST API POST /ccx/service/customreport2/tenant/Worker, map SCIM user to Workday Worker)
- [ ] T165 [P] [A2] Implement GetUserAsync in src/Adapters/Providers/WorkdayAdapter.cs (call Workday REST API GET /ccx/service/customreport2/tenant/Worker/{id})
- [ ] T166 [P] [A2] Implement UpdateUserAsync in src/Adapters/Providers/WorkdayAdapter.cs (call Workday REST API PUT /ccx/service/customreport2/tenant/Worker/{id})
- [ ] T167 [P] [A2] Implement DeleteUserAsync in src/Adapters/Providers/WorkdayAdapter.cs (call Workday REST API DELETE /ccx/service/customreport2/tenant/Worker/{id})
- [ ] T168 [P] [A2] Implement ListUsersAsync in src/Adapters/Providers/WorkdayAdapter.cs (call Workday custom report endpoint, implement pagination)
- [ ] T169 [P] [A2] Implement MapGroupToEntitlementAsync in src/Adapters/Providers/WorkdayAdapter.cs (map SCIM group to Workday org hierarchy level using transformation engine)
- [ ] T170 [P] [A2] Implement Workday error handling in src/Adapters/Providers/WorkdayAdapter.cs (translate Workday errors to AdapterException)

### ServiceNow Adapter

- [ ] T171 [P] [A3] Implement ServiceNowAdapter in src/Adapters/Providers/ServiceNowAdapter.cs (implement IAdapter, extend AdapterBase)
- [ ] T172 [P] [A3] Implement CreateUserAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API POST /api/now/table/sys_user, map SCIM user to ServiceNow user)
- [ ] T173 [P] [A3] Implement GetUserAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API GET /api/now/table/sys_user/{id})
- [ ] T174 [P] [A3] Implement UpdateUserAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API PATCH /api/now/table/sys_user/{id})
- [ ] T175 [P] [A3] Implement DeleteUserAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API DELETE /api/now/table/sys_user/{id})
- [ ] T176 [P] [A3] Implement ListUsersAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API with query parameters, implement pagination)
- [ ] T177 [P] [A3] Implement CreateGroupAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API POST /api/now/table/sys_user_group, native group support)
- [ ] T178 [P] [A3] Implement GetGroupAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API GET /api/now/table/sys_user_group/{id})
- [ ] T179 [P] [A3] Implement AddUserToGroupAsync in src/Adapters/Providers/ServiceNowAdapter.cs (call ServiceNow Table API POST /api/now/table/sys_user_grmember, add user to group)
- [ ] T180 [P] [A3] Implement ServiceNow error handling in src/Adapters/Providers/ServiceNowAdapter.cs (translate ServiceNow errors to AdapterException)

### Adapter Tests

- [ ] T181 [P] [A1] Contract test for SalesforceAdapter in tests/Contract/SalesforceAdapterTests.cs (verify IAdapter contract compliance, all methods implemented)
- [ ] T182 [P] [A1] Integration test for SalesforceAdapter in tests/Integration/SalesforceIntegrationTests.cs (end-to-end: create user Entra→Salesforce, verify user created, detect changes)
- [ ] T183 [P] [A2] Contract test for WorkdayAdapter in tests/Contract/WorkdayAdapterTests.cs (verify IAdapter contract compliance)
- [ ] T184 [P] [A2] Integration test for WorkdayAdapter in tests/Integration/WorkdayIntegrationTests.cs (end-to-end: create user Entra→Workday, verify org hierarchy mapping)
- [ ] T185 [P] [A3] Contract test for ServiceNowAdapter in tests/Contract/ServiceNowAdapterTests.cs (verify IAdapter contract compliance)
- [ ] T186 [P] [A3] Integration test for ServiceNowAdapter in tests/Integration/ServiceNowIntegrationTests.cs (end-to-end: create user Entra→ServiceNow, verify group membership)

**Checkpoint**: At this point, all 3 example adapters should work - Salesforce/Workday/ServiceNow can be swapped in, end-to-end flows functional

---

## Phase 9: Security & Performance Hardening (Priority: P3)

**Goal**: Security tests pass, performance targets met, code coverage >80%

- [ ] T187 [P] [S] Security test for invalid token in tests/Security/TokenValidationTests.cs (invalid signature → 401, malformed token → 401)
- [ ] T188 [P] [S] Security test for expired token in tests/Security/TokenValidationTests.cs (exp claim < current time → 401 with retry-after)
- [ ] T189 [P] [S] Security test for cross-tenant token in tests/Security/TenantIsolationTests.cs (token tid=tenant-A, try to access tenant-B resource → 403)
- [ ] T190 [P] [S] Security test for missing tid claim in tests/Security/TenantIsolationTests.cs (token without tid → 401)
- [ ] T191 [P] [S] Security test for PII redaction in tests/Security/PiiRedactionTests.cs (verify email partial mask, phone partial mask, address full redaction in all logs)
- [ ] T192 [P] [S] Security test for credentials in logs in tests/Security/CredentialLeakTests.cs (verify no API keys, passwords, tokens in Application Insights logs)
- [ ] T193 [P] [S] Security test for credentials in code in tests/Security/CredentialLeakTests.cs (verify no hardcoded credentials, all from Key Vault)
- [ ] T194 [P] [S] Security test for rate limiting in tests/Security/RateLimitTests.cs (exceed 1000 req/min → 429 Too Many Requests with Retry-After header)
- [ ] T195 [P] [P] Load test for 1000 req/s in tests/Performance/LoadTests.cs (simulate 1000 concurrent requests with mocked provider responses, measure SDK internal p95 latency, target <500ms per FR-046, measure end-to-end with simulated provider latency, document provider impact)
- [ ] T196 [P] [P] Load test for bulk operations in tests/Performance/BulkLoadTests.cs (create 1000 users in batch, measure throughput, verify no performance degradation)
- [ ] T197 [P] [P] Performance test for transformation engine in tests/Performance/TransformationPerformanceTests.cs (apply 100 rules to 1000 groups, verify rule caching works, measure latency)
- [ ] T198 [P] Code coverage analysis (run xUnit with code coverage, verify >80% coverage across all modules)
- [ ] T199 [P] SAST security scanning (run static analysis tools, fix high/critical vulnerabilities)
- [ ] T200 [P] Dependency vulnerability scanning (run npm audit or dotnet list package --vulnerable, update vulnerable dependencies)

---

## Phase 10: Documentation & Deployment (Priority: P3)

**Goal**: Complete documentation, deployment guide, runbooks, ready for production

- [ ] T201 [P] Write SDK developer guide in docs/sdk-developer-guide.md (how to build adapters, IAdapter interface contract, examples)
- [ ] T202 [P] Write adapter interface documentation in docs/adapter-interface.md (method signatures, error handling, testing strategies)
- [ ] T203 [P] Write transformation rule configuration guide in docs/transformation-rules.md (rule types, pattern syntax, conflict resolution, examples)
- [ ] T204 [P] Write deployment guide in docs/deployment-guide.md (Azure Functions setup, IaC with Bicep/Terraform, Key Vault configuration, Application Insights setup, Cosmos DB provisioning)
- [ ] T205 [P] Write operations runbook in docs/operations-runbook.md (toggle sync direction, view drift reports, reconcile conflicts, review audit logs with KQL queries)
- [ ] T206 [P] Create adapter template in src/Adapters/Providers/TemplateAdapter.cs (example skeleton for new providers, comments with TODOs)
- [ ] T207 [P] Generate OpenAPI/Swagger documentation for SCIM endpoints (use Swashbuckle for .NET or swagger-jsdoc for Node.js)
- [ ] T208 Create quickstart.md in specs/001-core-scim-framework/quickstart.md (5-minute setup guide, test scenarios per contracts)
- [ ] T209 [P] Create Azure Resource Manager (ARM) templates in deploy/azure/ (or Bicep files for Function App, Key Vault, Cosmos DB, Application Insights)
- [ ] T210 [P] Create CI/CD pipeline in .github/workflows/ci-cd.yml (build, test, deploy to Azure)

---

## Appendix A: Requirements Traceability Matrix

| FR | Requirement Summary | Task IDs | Status |
|----|---------------------|----------|--------|
| FR-001 | SCIM schema compliance | T018, T019, T023, T048, T049 | ✅ Full |
| FR-002 | SCIM endpoints | T034-T045 | ✅ Full |
| FR-003 | CRUD operations | T034-T045 | ✅ Full |
| FR-004 | PATCH operations | T037, T043, T056 | ✅ Full |
| FR-005 | Filter expressions | T031, T031a, T058, T058a | ✅ Full |
| FR-006 | Schema validation | T023, T023a, T048, T049 | ✅ Full |
| FR-007-010 | OAuth authentication | T013, T013a, T014, T014a, T017, T017a, T032, T032a, T064 | ✅ Full |
| FR-011-016 | Audit logging | T015, T015a, T016, T016a, T026, T026a, T033, T033a, T051, T052, T124 | ✅ Full |
| FR-016a | Audit log retention | T028 | ✅ Full |
| FR-017-021 | Adapter pattern | T066-T079 | ✅ Full |
| FR-022-028 | Transformations | T086-T105, T117 | ✅ Full |
| FR-029-035 | Change detection | T118-T133 | ✅ Full |
| FR-036-041a | Sync direction | T142-T148 | ✅ Full |
| FR-042-045 | Security | T001a, T012, T012a, T026, T026a, T187-T194 | ✅ Full |
| FR-046-049 | Performance | T029, T029a, T195-T197 | ✅ Full |
| FR-050-053 | Deployment | T001-T010, T008a, T209-T210 | ✅ Full |

### Additional Coverage Tasks

- [ ] T001a [P] Verify TLS 1.2+ enforcement in Azure Functions/App Service configuration (Azure enforces by default, document in deployment guide per FR-045)
- [ ] T008a Create health check endpoint /health in src/Core/HealthEndpoint.cs (return 200 OK with system status, adapter connectivity, Cosmos DB status per FR-053)
- [ ] T058a [P] [US1] Extended contract test for FilterParser in tests/Contract/FilterParserTests.cs (verify ALL 11 operators: eq, ne, co, sw, ew, pr, gt, ge, lt, le, and, or, not per FR-005)

**Note**: T026a was added in Phase 2A as part of test-first restructuring to verify PII redaction across all contexts per FR-016.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - No dependencies on other stories
- **User Story 2 (Phase 4)**: Depends on Foundational completion - No dependencies on other stories (can run parallel with US1)
- **User Story 3 (Phase 5)**: Depends on Foundational completion - May integrate with US2 adapters but independently testable
- **User Story 4 (Phase 6)**: Depends on Foundational completion + US2 (needs adapters) + US3 (needs transformation engine for group drift detection)
- **User Story 5 (Phase 7)**: Depends on US4 (needs sync engine)
- **Adapters (Phase 8)**: Depends on US2 (needs IAdapter interface) + US3 (needs transformation engine for group mapping)
- **Security/Performance (Phase 9)**: Depends on all user stories complete
- **Documentation/Deployment (Phase 10)**: Depends on all implementation complete

### Parallel Opportunities

- **Phase 1 (Setup)**: All tasks marked [P] can run in parallel (T003, T004, T005, T006, T009, T010)
- **Phase 2 (Foundational)**: Many tasks marked [P] can run in parallel (T012-T033, different files, no dependencies)
- **Phase 3 (US1)**: Endpoint implementations (T034-T045) can run in parallel (different files), tests (T053-T065) can run in parallel
- **Phase 4 (US2)**: Type definitions (T067-T073) can run in parallel, tests (T080-T085) can run in parallel
- **Phase 5 (US3)**: Model definitions (T086-T089) can run in parallel, provider transforms (T101-T103) can run in parallel, tests (T106-T117) can run in parallel
- **Phase 6 (US4)**: Model definitions (T118-T119) can run in parallel, tests (T134-T141) can run in parallel
- **Phase 7 (US5)**: Tests (T149-T153) can run in parallel
- **Phase 8 (Adapters)**: All 3 adapters (Salesforce T154-T162, Workday T163-T170, ServiceNow T171-T180) can be built in parallel by different developers
- **Phase 9 (Security/Performance)**: All tests (T187-T200) can run in parallel
- **Phase 10 (Documentation)**: All documentation tasks (T201-T210) can run in parallel

---

## Implementation Strategy

**Timeline Notes**: Phases overlap extensively after Phase 2 (Foundational) completes. Phases 3-7 represent user stories that can progress in parallel once foundational infrastructure (Phase 2) is ready. Phase 8 (Adapters) depends on Phase 5 (Transformation rules) completion. Phase 9 (Security/Perf) runs continuously alongside implementation phases. Phase 10 (Documentation) tracks all phases. See "Parallel Team Strategy" below for team coordination guidance.

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup (T001-T010)
2. Complete Phase 2: Foundational (T011-T033) - CRITICAL BLOCKING PHASE
3. Complete Phase 3: User Story 1 - Core SCIM Framework (T034-T065)
4. Complete Phase 4: User Story 2 - Adapter Pattern (T066-T085)
5. Complete Phase 5: User Story 3 - Transformation Engine (T086-T117)
6. **STOP and VALIDATE**: Test US1+US2+US3 independently, deploy MVP
7. Demo: Create user via SCIM → adapter routes to provider → transformation applied → user provisioned

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (Weeks 1-4)
2. Add User Story 1 → Test independently → Deploy (Week 5-6) - **MVP v0.1: Basic SCIM endpoints**
3. Add User Story 2 → Test independently → Deploy (Week 7-8) - **MVP v0.2: Multi-provider support**
4. Add User Story 3 → Test independently → Deploy (Week 9-10) - **MVP v0.3: Group transformations**
5. Add User Story 4 → Test independently → Deploy (Week 11-12) - **v1.0: Change detection**
6. Add User Story 5 → Test independently → Deploy (Week 13-14) - **v1.1: Direction toggle**
7. Add Adapters (Phase 8) → Deploy (Week 15-16) - **v1.2: Real-world adapters**
8. Security/Performance hardening (Phase 9) → Deploy (Week 17-18) - **v2.0: Production-ready**
9. Documentation/Deployment (Phase 10) → Release (Week 19-20) - **v2.0 GA**

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (Weeks 1-4)
2. **Once Foundational is done** (Week 5+):
   - **Developer A**: User Story 1 (Core SCIM endpoints)
   - **Developer B**: User Story 2 (Adapter pattern)
   - **Developer C**: User Story 3 (Transformation engine)
3. **Week 11+**: All 3 developers work on User Story 4 (needs US2+US3 integration)
4. **Week 13+**: Developer A works on User Story 5 (needs US4)
5. **Week 15+**: 
   - **Developer A**: Salesforce adapter
   - **Developer B**: Workday adapter
   - **Developer C**: ServiceNow adapter
6. **Week 17+**: All developers work on Security/Performance/Documentation in parallel

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story (US1, US2, US3, US4, US5, A1, A2, A3, S, P) for traceability
- Each user story should be independently completable and testable
- Phase 2 (Foundational) is CRITICAL - blocks all user story work
- Stop at any checkpoint to validate story independently before proceeding
- Commit after each task or logical group of related tasks
- All file paths assume single project structure - adjust based on plan.md
- Constitution compliance validated: SCIM 2.0 spec, OAuth 2.0 auth, comprehensive audit logging, Azure deployment, test coverage >80%, security review, tenant isolation

---

**Total Tasks**: 210  
**MVP Tasks (US1-US3)**: ~117 (T001-T117)  
**Full Feature Tasks (US1-US5 + Adapters)**: ~186 (T001-T186)  
**Production-Ready (All Phases)**: 210 (T001-T210)

**Estimated Timeline**:
- MVP (US1-US3): 10 weeks
- Full Feature (US1-US5 + Adapters): 16 weeks
- Production-Ready (All Phases): 20 weeks
