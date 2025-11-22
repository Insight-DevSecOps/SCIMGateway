# Feature Specification: Extensible SCIM Gateway SDK/Framework

**Feature Branch**: `001-core-scim-framework`  
**Created**: 2025-11-22  
**Status**: Draft  
**Input**: Build extensible SCIMGateway SDK for multi-SaaS integration with bidirectional sync and entitlement transformation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Core SCIM Framework Foundation (Priority: P1)

**Scenario**: As a platform engineer, I need an extensible SDK that standardizes SCIM provisioning operations so that I can build adapters for different SaaS providers without reimplementing core SCIM logic.

**Why this priority**: Foundation for all other features; blocks adapter development; enables code reuse across providers

**Independent Test**: SDK core can be loaded, provides standard SCIM operations (create/read/update/delete users and groups), validates RFC 7643 schemas, and logs all operations. Can be verified with unit tests alone.

**Acceptance Scenarios**:

1. **Given** an HTTP request to `/Users` endpoint **When** a valid SCIM user creation request is received **Then** SDK parses request, validates against SCIM 2.0 schema, passes to adapter layer, and returns SCIM-compliant response
2. **Given** an HTTP request to `/Groups` endpoint **When** a valid SCIM group creation request is received **Then** SDK parses request, validates against SCIM 2.0 schema, and passes to adapter layer
3. **Given** any CRUD operation **When** operation completes **Then** SDK logs operation to Application Insights with timestamp, actor, operation type, resource ID, result status
4. **Given** an invalid request **When** SDK receives malformed JSON or violates SCIM schema **Then** SDK returns SCIM-compliant error response (400/422) with descriptive error message
5. **Given** a request without Bearer token **When** request is received **Then** SDK rejects with 401 Unauthorized
6. **Given** a Bearer token **When** token is invalid or expired **Then** SDK rejects with 401, logs rejection, increments rate limit counter

---

### User Story 2 - Adapter Pattern for Multi-SaaS Integration (Priority: P1)

**Scenario**: As a SaaS integration specialist, I need a standardized adapter interface so that I can plug in provider-specific implementations (Salesforce, Workday, ServiceNow, etc.) without modifying core gateway code.

**Why this priority**: Core MVP requirement; enables SDK extensibility; unblocks adapter development in parallel

**Independent Test**: Define adapter interface contract, implement mock adapter, verify core SDK calls adapter methods correctly for each CRUD operation. Adapter can be tested independently via contract tests.

**Acceptance Scenarios**:

1. **Given** an adapter implementing the standard interface **When** SDK processes a user create operation **Then** adapter's `createUser()` method is called with normalized SCIM user data
2. **Given** an adapter **When** SDK processes a user read operation **Then** adapter's `readUser(id)` method is called and result is transformed to SCIM format
3. **Given** an adapter **When** SDK processes a user update operation **Then** adapter's `updateUser(id, changes)` method is called with delta changes
4. **Given** an adapter **When** SDK processes a group operation **Then** adapter can transform SCIM group representation to provider-specific format
5. **Given** an adapter **When** operation fails on provider **Then** adapter returns structured error; SDK translates to SCIM error response
6. **Given** multiple adapters **When** each is registered for different providers **Then** SDK routes requests to correct adapter based on configuration
7. **Given** an adapter **When** any operation succeeds or fails **Then** adapter operation is logged with full context to audit log

---

### User Story 3 - Group/Entitlement Transformation Engine (Priority: P1)

**Scenario**: As an integration architect, I need a transformation engine so that SCIM groups (which have a specific structure in Entra ID) can be mapped to diverse SaaS entitlement models (roles, permissions, org hierarchies, department assignments) that differ by provider.

**Why this priority**: Solves core problem of group abstraction; enables support for SaaS providers with non-standard group concepts; required for MVP

**Independent Test**: Define transformation rules for group→role mapping, test transformation logic independently, verify SDK applies transformations correctly before calling adapter. Can be tested with unit and contract tests.

**Acceptance Scenarios**:

1. **Given** a SCIM group with name "Sales-Americas" **When** transformation rules map groups to provider roles **Then** group is transformed to provider-specific role representation (e.g., Salesforce role ID)
2. **Given** a SCIM group **When** provider doesn't support groups but supports roles/departments **Then** transformation engine converts group membership to role/department assignment
3. **Given** a SCIM group **When** provider has nested org hierarchy **Then** transformation engine maps group to correct hierarchy level based on rules
4. **Given** a group membership operation **When** user is added to group **Then** transformation engine applies rules to determine provider-specific entitlement change
5. **Given** transformation rules **When** new provider adapter added **Then** rules can be configured per-provider without modifying core transformation engine
6. **Given** a transformation rule **When** rule cannot be applied (no mapping exists) **Then** system logs warning, returns error, and admin can resolve via configuration
7. **Given** transformation rules **When** user queries group membership **Then** system applies reverse transformation to return SCIM-compliant group representation

---

### User Story 4 - Bidirectional Sync with Change Detection (Priority: P2)

**Scenario**: As an operations manager, I need the gateway to detect changes in the SaaS provider so that I can identify drift from Entra ID and choose when to reconcile or reverse-sync changes.

**Why this priority**: Enables pull-based sync; improves change detection; supports P2 direction (SaaS→Entra) as backup; high value for operations

**Independent Test**: SDK can poll provider via adapter, detect changes from last sync state, compare with Entra state, flag drift with audit trail. Can be tested with integration tests.

**Acceptance Scenarios**:

1. **Given** a scheduled sync job **When** job polls SaaS provider via adapter **Then** adapter returns current state of all users/groups
2. **Given** current provider state and last known state **When** sync compares states **Then** SDK detects added, modified, and deleted users/groups
3. **Given** drift detected (provider state differs from Entra ID state) **When** comparison completes **Then** SDK logs drift with details (what changed, when, expected vs actual)
4. **Given** a drift situation **When** operations team queries drift report **Then** system shows user/group, old value, new value, timestamp, and suggested remediation
5. **Given** drift exists **When** reconciliation is triggered **Then** system applies configured resolution (Entra→Provider, Provider→Entra, or manual)
6. **Given** a full sync cycle **When** completed **Then** system updates "last sync state" and "last sync timestamp" for next cycle
7. **Given** sync errors occur **When** adapter cannot reach provider **Then** system logs error, retries per configured policy, alerts operations team

---

### User Story 5 - Sync Direction Toggle (Priority: P2)

**Scenario**: As a migration specialist, I need to toggle the sync direction so that I can switch the "master" system between Entra ID (push to SaaS) and SaaS provider (pull from provider) during migrations without code changes.

**Why this priority**: Enables flexible migration scenarios; reduces operational friction; supports pilot→production transition

**Independent Test**: Configuration can be toggled, SDK behavior changes correctly based on direction setting, operations are logged with direction metadata. Can be tested with integration and configuration tests.

**Acceptance Scenarios**:

1. **Given** sync direction configured as "ENTRA_TO_SAAS" (push) **When** user changes in Entra **Then** gateway pushes changes to SaaS provider via adapter
2. **Given** sync direction configured as "SAAS_TO_ENTRA" (pull) **When** scheduled sync runs **Then** gateway polls SaaS provider and detects changes
3. **Given** sync direction toggle **When** changed from push to pull **Then** system logs direction change, new direction applies to next sync
4. **Given** sync running in push direction **When** new changes arrive from SaaS provider **Then** system logs as informational "drift" (not applied)
5. **Given** sync in pull direction **When** Entra changes arrive **Then** system logs as informational "ignored" (not applied)
6. **Given** direction toggle **When** changed **Then** system does NOT retroactively apply old direction changes
7. **Given** sync direction configuration **When** persisted to system **Then** direction survives restart; system logs direction on startup

---

### Edge Cases

- What happens when both Entra and SaaS have changes for same user in opposite direction? → Log conflict, flag for manual review based on configured policy
- How does system handle provider that goes offline? → Retry logic, exponential backoff, alert operations team, queue changes for retry
- What happens when group transformation rule doesn't exist for provider? → Log warning with context, admin configures rule, no users affected until rule added
- How does system handle user deleted on provider but still in Entra? → Drift detection catches deletion, flags based on direction (push=recreate, pull=remove from Entra)
- What if adapter returns non-SCIM compliant data? → SDK validates response, returns error, logs with provider details for debugging

## Requirements *(mandatory)*

### Functional Requirements

**SCIM Compliance & Core Operations:**
- **FR-001**: SDK MUST implement SCIM 2.0 Core Schema (RFC 7643) for Users and Groups with all required and optional attributes
- **FR-002**: SDK MUST expose HTTP endpoints `/Users`, `/Users/{id}`, `/Groups`, `/Groups/{id}`, `/Me`, `/.well-known/scim-configuration` per SCIM spec
- **FR-003**: SDK MUST support SCIM CREATE, READ, UPDATE, DELETE (CRUD) operations on Users and Groups
- **FR-004**: SDK MUST support SCIM PATCH operations for partial updates per RFC 6902 JSON Patch
- **FR-005**: SDK MUST support SCIM filter expressions on Users and Groups (e.g., `filter=userName eq "john.doe"`)
- **FR-006**: SDK MUST validate all incoming requests against SCIM 2.0 schema and reject invalid requests with SCIM error responses

**Authentication & Authorization:**
- **FR-007**: SDK MUST enforce OAuth 2.0 Bearer token authentication on all endpoints; no unauthenticated access
- **FR-008**: SDK MUST validate Bearer tokens via Entra ID per Microsoft's token validation guidelines
- **FR-009**: SDK MUST verify tenant isolation; requests MUST only access data for the authenticated tenant
- **FR-010**: SDK MUST implement rate limiting for failed authentication attempts; lock account after N failures per configured policy

**Audit Logging:**
- **FR-011**: SDK MUST log all CRUD operations to Application Insights with: timestamp, actor (service principal), operation type, resource ID, old values (for updates), new values, HTTP status, response time
- **FR-012**: SDK MUST log all authentication attempts (success and failure) with timestamp, principal, status, source IP
- **FR-013**: SDK MUST log all errors with: error code, error message, context (operation type, resource, adapter), remediation hints
- **FR-014**: SDK MUST log all drift detections with: drift type (user added/modified/deleted/group changed), old state, new state, timestamp, affected users/groups
- **FR-015**: SDK MUST log all sync direction changes with: previous direction, new direction, timestamp, triggered by (manual/auto)
- **FR-016**: SDK MUST NOT log sensitive data (passwords, tokens, PII); logs MUST be sanitized/redacted

**Adapter Pattern & Extensibility:**
- **FR-017**: SDK MUST define a standard Adapter interface with methods: `createUser()`, `readUser()`, `updateUser()`, `deleteUser()`, `createGroup()`, `readGroup()`, `updateGroup()`, `deleteGroup()`, `mapGroupToEntitlements()`
- **FR-018**: SDK MUST allow adapters to be registered per SaaS provider; routing based on configuration
- **FR-019**: SDK MUST provide adapter context/utilities: SCIM schema validation, error handling, logging, key management (Azure Key Vault integration)
- **FR-020**: SDK MUST catch and handle adapter exceptions gracefully; translate adapter errors to SCIM error responses
- **FR-021**: SDK MUST allow multiple adapters to be active simultaneously for multi-SaaS environments

**Group/Entitlement Transformation:**
- **FR-022**: SDK MUST provide a transformation engine that maps SCIM groups to provider-specific entitlement models (roles, permissions, org hierarchy, departments)
- **FR-023**: SDK MUST support transformation rules per SaaS provider; rules MUST be configurable without code changes
- **FR-024**: SDK MUST apply transformations to group operations before calling adapter; transformations transparent to adapter
- **FR-025**: SDK MUST support transformation rules for: group→role, group→department, group→permission, group→org_hierarchy mappings
- **FR-026**: SDK MUST provide reverse transformation to convert provider entitlements back to SCIM group representation for drift detection
- **FR-027**: SDK MUST log transformation operations (rule applied, mapping result, conflicts)
- **FR-028**: SDK MUST allow admins to preview transformation results before applying to production

**Change Detection & Drift:**
- **FR-029**: SDK MUST support scheduled polling of SaaS provider via adapter to detect changes
- **FR-030**: SDK MUST maintain state history (last known state, last sync timestamp, sync status)
- **FR-031**: SDK MUST compare current provider state against last known state to detect drift
- **FR-032**: SDK MUST detect drift types: users added/deleted/modified on provider, groups changed, entitlements changed
- **FR-033**: SDK MUST provide drift report API with details: resource type, resource ID, old value, new value, timestamp, recommended action
- **FR-034**: SDK MUST support reconciliation modes: auto-apply (based on direction), manual review, ignore
- **FR-035**: SDK MUST NOT delete or modify data without explicit action; drift detection is read-only unless reconciliation triggered

**Bidirectional Sync & Direction Toggle:**
- **FR-036**: SDK MUST support two sync directions: ENTRA_TO_SAAS (Entra pushes to provider) and SAAS_TO_ENTRA (provider pulls to Entra)
- **FR-037**: SDK MUST allow sync direction to be toggled via configuration; direction change applies to next sync cycle
- **FR-038**: SDK MUST log sync direction changes with timestamp, previous/new direction, triggered by
- **FR-039**: SDK MUST NOT apply operations from inactive direction; opposite direction changes logged as "informational only"
- **FR-040**: SDK MUST store sync direction in persistent configuration (survives restart)
- **FR-041**: SDK MUST handle direction toggle gracefully during active sync (complete current cycle, apply new direction next cycle)

**Data Protection & Security:**
- **FR-042**: SDK MUST store adapter credentials (API keys, OAuth tokens for providers) in Azure Key Vault; never in code or config files
- **FR-043**: SDK MUST use managed identity to access Key Vault
- **FR-044**: SDK MUST sanitize/redact PII from logs and error responses
- **FR-045**: SDK MUST enforce TLS 1.2+ for all external communications

**Performance & Scalability:**
- **FR-046**: SDK MUST achieve p95 latency <2s for typical CRUD operations
- **FR-047**: SDK MUST handle 1000 concurrent requests per deployment unit
- **FR-048**: SDK MUST support batch operations (create multiple users/groups in single request)
- **FR-049**: SDK MUST implement connection pooling and caching for adapter connections

**Configuration & Deployment:**
- **FR-050**: SDK MUST run on Azure Functions (HTTP-triggered) or Azure App Service
- **FR-051**: SDK MUST be deployable via Infrastructure-as-Code (Bicep or Terraform)
- **FR-052**: SDK MUST support environment-based configuration (dev, staging, production)
- **FR-053**: SDK MUST expose health check endpoint for monitoring

### Key Entities *(data model)*

**User**
- SCIM attributes: id, userName, displayName, name (familyName, givenName), emails, phoneNumbers, active, groups, roles, externalId, meta (created, lastModified, location, version)
- Internal attributes: tenantId, adapterId, providerUserId, lastSyncTimestamp, syncDirection, state

**Group**
- SCIM attributes: id, displayName, members (user references), description, externalId, meta (created, lastModified, location, version)
- Internal attributes: tenantId, adapterId, providerGroupId, lastSyncTimestamp, entitlementMapping, transformedRepresentation

**Entitlement** (Provider-Specific)
- Attributes: id, name, type (role/permission/department/org_level), provider, mappedGroups, priority
- Transformation: SCIM group → provider entitlement model

**Transformation Rule**
- Attributes: id, providerId, sourceType (SCIM group), targetType (provider role/dept/permission), mapping (group name pattern → target), priority, enabled, externalId
- Configuration: per-provider rules, configurable via API/UI

**Sync State**
- Attributes: tenantId, providerId, lastSyncTimestamp, lastKnownState (snapshot), syncStatus (success/failed/partial), direction, conflictLog, driftLog
- Storage: Cosmos DB or app state (configurable)

**Audit Log Entry**
- Attributes: timestamp, tenantId, actorId, operationType (CREATE/READ/UPDATE/DELETE/PATCH), resourceType (User/Group), resourceId, oldValue, newValue, status, responseTime, errorCode, errorMessage
- Destination: Application Insights (primary), optional Cosmos DB for long-term retention

**Adapter Configuration**
- Attributes: adapterId, providerId, providerName, credentialKeyVaultPath, enabled, transformationRulesPath, retryPolicy, timeoutSeconds, batchSize

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: SDK processes 1000 concurrent SCIM requests with p95 latency <2s (verifiable via load testing)
- **SC-002**: 100% of SCIM endpoints conform to RFC 7643 validation (verifiable via SCIM compliance test suite)
- **SC-003**: All CRUD operations logged with full context to Application Insights within 100ms (verifiable via audit log tests)
- **SC-004**: Adapters for 3+ providers (Salesforce, Workday, ServiceNow) successfully built using SDK without modifications to core code (verifiable via adapter test suites)
- **SC-005**: Group transformations reduce integration time per provider from weeks to days (qualitative: measurable via project retrospective)
- **SC-006**: Change drift detection identifies 100% of user/group changes within 5 minutes of change occurring on provider (verifiable via drift detection tests)
- **SC-007**: Sync direction toggle successfully switches between push/pull with zero data loss (verifiable via integration tests with dual direction scenarios)
- **SC-008**: Zero security incidents related to credential exposure, cross-tenant access, or unauthenticated requests (verifiable via security testing and audit logs)
- **SC-009**: >80% code coverage via unit and integration tests (verifiable via coverage reports)
- **SC-010**: 100% of adapters successfully pass SCIM compliance contract tests before being marked "ready for production" (verifiable via PR merge gates)

---

## Assumptions

1. **Azure Infrastructure Available**: Organization has Azure subscription with capacity to deploy Functions/App Service, Key Vault, Application Insights, Cosmos DB (optional)
2. **Entra ID Tenant**: Organization has Entra ID tenant configured for token validation
3. **SaaS Provider APIs Available**: Target SaaS providers (Salesforce, Workday, etc.) expose REST APIs with user/group management capabilities
4. **Configuration Management**: System uses Azure Key Vault for secrets and Azure App Configuration (or environment variables) for non-sensitive config
5. **Initial Sync Scope**: First implementation focuses on Users and Groups; other SCIM resource types (Devices, Schemas) deferred to future iterations
6. **No Real-Time Sync**: Initial implementation uses polling for change detection; real-time webhooks deferred to P2
7. **Single Direction per Sync Cycle**: Direction toggle applies to next cycle; current cycle completes with existing direction
8. **Transformation Rules**: Admin must configure transformation rules per provider initially; auto-discovery deferred to future

---

## Constraints

1. **SCIM Compliance Non-Negotiable**: All implementations MUST pass RFC 7643 validation; no exceptions
2. **Audit Logging Mandatory**: All CRUD operations MUST be logged; no silent failures
3. **Tenant Isolation Hard Requirement**: Cross-tenant data access MUST be cryptographically prevented
4. **Azure-Only Deployment**: No support for on-premises or other cloud providers in initial release
5. **No Data Deletion Without Consent**: Reconciliation MUST require explicit approval; no automatic data deletion
6. **Test Coverage Minimum**: All features MUST have >80% code coverage before merge
7. **Documentation Requirement**: Every adapter MUST have integration documentation; every transformation rule MUST be documented

---

**Version**: 1.0.0 | **Created**: 2025-11-22 | **Status**: Ready for Clarification & Planning
