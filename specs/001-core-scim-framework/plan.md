# Implementation Plan: Extensible SCIM Gateway SDK/Framework

**Branch**: `001-core-scim-framework` | **Date**: 2025-11-22 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification for extensible SCIM SDK with adapter pattern, transformation engine, and bidirectional sync

## Summary

Build an extensible SCIM 2.0-compliant gateway SDK that abstracts SaaS provider integration patterns behind a standardized adapter interface. The SDK enables rapid multi-SaaS adapter development through: (1) core SCIM HTTP handling and schema validation, (2) adapter pattern for provider-specific logic, (3) group/entitlement transformation engine to handle diverse provider models, (4) bidirectional sync with change drift detection, and (5) sync direction toggle for flexible migration scenarios.

## Technical Context

**Language/Version**: .NET 8+ (C#) or Node.js 20+ LTS  
**Primary Dependencies**: 
- Azure SDK (Identity, Key Vault, Application Insights, Cosmos DB)
- SCIM schema validators (existing or custom per RFC 7643)
- HTTP framework (ASP.NET Core or Express.js)
- JSON schema validation library
- OAuth 2.0 token validation middleware

**Storage**: 
- Transient: Adapter connection pooling
- Persistent: Azure Cosmos DB for sync state (tenant, provider, lastSyncTimestamp, lastKnownState, conflictLog)
- Secrets: Azure Key Vault (adapter API credentials, signing keys)
- Audit: Application Insights (queryable via KQL)

**Testing**: 
- Unit tests: Framework logic, transformations, error handling (xUnit/.NET or Jest/Node.js)
- Contract tests: SCIM endpoint compliance (RFC 7643 validators)
- Integration tests: End-to-end adapter flow (mocked providers)
- Security tests: Token validation, tenant isolation, PII redaction

**Target Platform**: Azure Functions (HTTP-triggered, consumption/premium tier) or Azure App Service with autoscaling  
**Performance Goals**: p95 <2s latency, 1000 req/s throughput per deployment unit, >80% code coverage  
**Constraints**: SCIM 2.0 compliance mandatory, audit logging non-negotiable, tenant isolation cryptographic, no data deletion without consent  
**Scale/Scope**: Multi-tenant, multi-SaaS (3+ providers in MVP), 100k+ users across tenants, 5+ groups per user average

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Required Compliance Validations:**

- [x] **SCIM 2.0 Specification Compliance**: Spec requires all endpoints align with RFC 7643 (Users, Groups, CRUD, PATCH, filtering, error codes) — PASS
- [x] **Authentication & Authorization**: Spec requires OAuth 2.0 Bearer token validation, tenant isolation, rate limiting on auth failures — PASS
- [x] **Comprehensive Audit Logging**: Spec requires all CRUD operations logged to Application Insights with timestamp, actor, operation type, resource ID, old/new values, status, errors — PASS
- [x] **Azure Deployment**: Spec requires Azure Functions or App Service (serverless), Azure Key Vault for secrets, Application Insights for telemetry, Infrastructure-as-Code (Bicep/Terraform) — PASS
- [x] **Test Coverage**: Spec requires contract tests validating SCIM compliance before implementation, integration tests for end-to-end flows, >80% code coverage requirement — PASS
- [x] **Security Review**: Spec explicitly addresses: no auth bypass (FR-007-010), no unlogged operations (FR-011-016), no credentials in code/logs (FR-042-044), PII redaction (FR-044), managed identity (FR-043) — PASS
- [x] **Tenant Isolation**: Spec requires cryptographic tenant isolation (FR-009), tenant ID on all entities, cross-tenant verification — PASS

**Gate Status**: ✅ CONSTITUTION CHECK PASSED — All 7 validation items explicit in spec

## Project Structure

### Documentation (this feature)

```text
specs/001-core-scim-framework/
├── spec.md                    # Feature specification (53 FRs, 5 user stories, 10 success criteria)
├── plan.md                    # This file (/speckit.plan command output)
├── research.md                # Phase 0 output (to be completed)
├── data-model.md              # Phase 1 output (to be completed)
├── quickstart.md              # Phase 1 output (to be completed)
├── contracts/                 # Phase 1 output (to be completed)
│   ├── scim-user-endpoints.md
│   ├── scim-group-endpoints.md
│   ├── adapter-interface.md
│   ├── transformation-engine.md
│   └── sync-state.md
└── checklists/
    └── requirements.md         # Quality validation checklist
```

### Source Code Structure (to be created)

```text
src/
├── Core/
│   ├── ScimFramework.cs           # Main SDK class, HTTP endpoint registration
│   ├── RequestHandler.cs           # SCIM request parsing and routing
│   ├── ResponseFormatter.cs        # SCIM response formatting per RFC 7643
│   ├── SchemaValidator.cs          # RFC 7643 schema validation
│   ├── ErrorHandler.cs             # SCIM error response generation
│   └── AuditLogger.cs              # Audit logging to Application Insights
├── Authentication/
│   ├── BearerTokenValidator.cs     # OAuth 2.0 Bearer token validation
│   ├── TenantResolver.cs           # Extract and verify tenant from token
│   └── RateLimiter.cs              # Rate limiting for failed auth attempts
├── Adapters/
│   ├── IAdapter.cs                 # Adapter interface contract
│   ├── AdapterRegistry.cs          # Adapter registration and routing
│   ├── AdapterContext.cs           # Utilities provided to adapters
│   └── Providers/
│       ├── SalesforceAdapter.cs    # Example implementation
│       ├── WorkdayAdapter.cs       # Example implementation
│       └── ServiceNowAdapter.cs    # Example implementation
├── Transformations/
│   ├── TransformationEngine.cs     # Group→Entitlement mapping
│   ├── TransformationRule.cs       # Rule definition and storage
│   ├── Providers/
│   │   ├── SalesforceTransform.cs  # Salesforce-specific rules
│   │   ├── WorkdayTransform.cs     # Workday-specific rules
│   │   └── ServiceNowTransform.cs  # ServiceNow-specific rules
│   └── ReverseTransform.cs         # Entitlement→Group for drift detection
├── SyncEngine/
│   ├── SyncState.cs                # State tracking per tenant/provider
│   ├── ChangeDetector.cs           # Drift detection logic
│   ├── SyncDirectionManager.cs     # Direction toggle and enforcement
│   ├── Reconciler.cs               # Reconciliation strategies (auto/manual/ignore)
│   └── PollingService.cs           # Scheduled polling for change detection
├── Models/
│   ├── ScimUser.cs                 # SCIM User schema
│   ├── ScimGroup.cs                # SCIM Group schema
│   ├── EntitlementMapping.cs       # Provider entitlement model
│   ├── SyncDirection.cs            # Enum: ENTRA_TO_SAAS, SAAS_TO_ENTRA
│   ├── DriftReport.cs              # Drift detection results
│   └── AuditLogEntry.cs            # Audit log structure
├── Configuration/
│   ├── AdapterConfiguration.cs     # Per-adapter config (credentials, rules, retry policy)
│   ├── KeyVaultManager.cs          # Credential retrieval from Key Vault
│   └── AppConfigManager.cs         # Non-sensitive configuration from App Configuration/env
└── Utilities/
    ├── SchemaParser.cs             # Parse SCIM schemas
    ├── FilterParser.cs             # Parse SCIM filter expressions
    ├── PiiRedactor.cs              # Redact sensitive data from logs
    └── ConnectionPool.cs           # Provider API connection pooling

tests/
├── Unit/
│   ├── SchemaValidatorTests.cs
│   ├── TransformationEngineTests.cs
│   ├── ChangeDetectorTests.cs
│   ├── AdapterRegistryTests.cs
│   ├── PiiRedactorTests.cs
│   └── [more...]
├── Contract/
│   ├── ScimUserEndpointTests.cs    # RFC 7643 compliance for /Users
│   ├── ScimGroupEndpointTests.cs   # RFC 7643 compliance for /Groups
│   ├── AdapterInterfaceTests.cs    # Adapter contract validation
│   ├── TransformationRuleTests.cs  # Rule application correctness
│   └── [more...]
├── Integration/
│   ├── EntraToSalesforceFlowTests.cs   # End-to-end: user create Entra→Salesforce
│   ├── SalesforceToEntraFlowTests.cs   # End-to-end: change detection Salesforce→Entra
│   ├── GroupTransformationFlowTests.cs # End-to-end: group sync with transformation
│   ├── SyncDirectionToggleTests.cs     # Direction toggle and enforcement
│   └── [more...]
└── Security/
    ├── TokenValidationTests.cs
    ├── TenantIsolationTests.cs
    ├── PiiRedactionTests.cs
    └── [more...]
```

**Structure Decision**: Single project (.NET 8+ or Node.js monolith) with clear separation of concerns:
- **Core**: SCIM HTTP handling, schema validation, error handling (no provider-specific logic)
- **Adapters**: Plugin pattern for provider implementations (isolated dependencies per adapter)
- **Transformations**: Engine + provider-specific rules (rules configurable without code changes)
- **Sync**: Change detection, drift reporting, direction toggle (independent of provider)
- **Models**: Shared data structures (SCIM + internal state)
- **Configuration**: Centralized config management (Key Vault + App Configuration)
- **Tests**: Organized by type (unit, contract, integration, security) with clear naming conventions

This structure enables:
- Core SDK reuse across all providers
- Adapter development in parallel (isolated from core changes)
- Transformation rule updates without SDK changes
- Comprehensive testing at each layer

## Phase Breakdown

### Phase 0: Research & Design (Week 1-2)

**Objectives**: Understand SCIM 2.0 spec in detail, research provider APIs, define adapter contract

- [ ] **R1**: Document SCIM 2.0 User/Group schemas (required vs optional attributes, complex types, canonicalValues)
- [ ] **R2**: Document SCIM 2.0 HTTP semantics (CRUD, PATCH, filtering, error codes, response formats)
- [ ] **R3**: Research Salesforce user/entitlement APIs (REST, formats, rate limits, error handling)
- [ ] **R4**: Research Workday user/group APIs (REST, formats, rate limits, error handling)
- [ ] **R5**: Research ServiceNow user/group APIs (REST, formats, rate limits, error handling)
- [ ] **R6**: Document group model diversity (Salesforce=roles, Workday=org hierarchy, ServiceNow=groups)
- [ ] **R7**: Design adapter interface contract (methods, error handling, context utilities)
- [ ] **R8**: Design transformation rule format (source→target mapping, priority, conflicts)
- [ ] **R9**: Document decision criteria for Azure Functions vs App Service (cost comparison at 100k users, cold start requirements, scaling needs, stateful vs stateless, development complexity). Recommend deployment target and document rationale. Decision MUST be made before Phase 1 begins.

**Output**: `research.md` with findings, `contracts/adapter-interface.md` with contract definition

### Phase 1: Framework Foundations (Week 3-4)

**Objectives**: Implement core SCIM handling, adapter pattern, transformation engine, sync state management

- [ ] **D1**: Design data models (ScimUser, ScimGroup, EntitlementMapping, SyncState, DriftReport)
- [ ] **D2**: Design schema validation architecture (RFC 7643 validators, custom validators)
- [ ] **D3**: Design error handling and SCIM error response formatting
- [ ] **D4**: Design audit logging structure and Application Insights integration
- [ ] **D5**: Design OAuth 2.0 token validation and tenant isolation verification
- [ ] **D6**: Design adapter registry and routing logic
- [ ] **D7**: Design transformation engine (rule matching, conflict detection, reverse transforms)
- [ ] **D8**: Design sync state storage (Cosmos DB schema, state transitions, conflict log)

**Output**: `data-model.md` with entity definitions, `contracts/` directory with endpoint/interface specs, `quickstart.md` with SDK usage examples

### Phase 2: Core Implementation (Week 5-8)

**Objectives**: Build SDK core framework, authentication/authorization, audit logging, adapter pattern

**Foundational Tasks** (Blocking all user stories):
- [ ] F1: Create project structure per plan above
- [ ] F2: Setup Azure Function/App Service project template
- [ ] F3: Implement ScimFramework core class (endpoint registration, routing)
- [ ] F4: Implement SCIM schema validator (RFC 7643 User/Group schemas)
- [ ] F5: Implement SCIM error response formatter
- [ ] F6: Integrate OAuth 2.0 Bearer token validation middleware
- [ ] F7: Implement tenant resolver (extract tenant from token, verify isolation)
- [ ] F8: Integrate Application Insights and audit logging middleware
- [ ] F9: Implement rate limiting for failed authentication attempts
- [ ] F10: Implement Azure Key Vault integration for adapter credentials
- [ ] F11: Implement connection pooling for adapter communications
- [ ] F12: Create adapter interface contract and base adapter class
- [ ] F13: Setup test infrastructure (unit, contract, integration test projects)

### Phase 3: User Story 1 - Core SCIM Operations (Week 5-6, parallel with F1-F12)

**Goal**: Users and groups can be created, read, updated, deleted via SCIM endpoints

- [ ] US1-T1: Implement POST /Users endpoint (create user with SCIM validation)
- [ ] US1-T2: Implement GET /Users/{id} endpoint (read user, return SCIM format)
- [ ] US1-T3: Implement PATCH /Users/{id} endpoint (update user with RFC 6902 support)
- [ ] US1-T4: Implement DELETE /Users/{id} endpoint (soft delete or hard delete per config)
- [ ] US1-T5: Implement GET /Users endpoint (list users with filtering and pagination)
- [ ] US1-T6: Implement POST /Groups endpoint (create group with SCIM validation)
- [ ] US1-T7: Implement GET /Groups/{id} endpoint (read group, return SCIM format with members)
- [ ] US1-T8: Implement PATCH /Groups/{id} endpoint (update group members)
- [ ] US1-T9: Implement DELETE /Groups/{id} endpoint
- [ ] US1-T10: Implement GET /Groups endpoint (list groups with filtering and pagination)
- [ ] US1-T11: Contract tests for SCIM User endpoints (RFC 7643 compliance)
- [ ] US1-T12: Contract tests for SCIM Group endpoints (RFC 7643 compliance)

**Independent Test**: Run US1-specific contract tests; all SCIM endpoints operational, schema validation working, errors returned in SCIM format

### Phase 4: User Story 2 - Adapter Pattern (Week 7-8, parallel with US1)

**Goal**: Adapter interface defined, mock adapter works, SDK routes requests to adapters correctly

- [ ] US2-T1: Define IAdapter interface with CRUD methods
- [ ] US2-T2: Implement AdapterRegistry (register adapters per provider)
- [ ] US2-T3: Implement adapter routing logic (select correct adapter based on config)
- [ ] US2-T4: Implement AdapterContext (utilities provided to adapters: logging, error handling, schema validation)
- [ ] US2-T5: Create mock/dummy adapter for testing
- [ ] US2-T6: Implement adapter exception handling and error translation to SCIM errors
- [ ] US2-T7: Contract tests for adapter interface (all required methods present, correct signatures)
- [ ] US2-T8: Integration tests (SDK calls adapter methods for each CRUD operation)

**Independent Test**: Mock adapter can be registered, SDK routes requests to it, adapter methods are called with correct parameters, responses translated to SCIM format

### Phase 5: User Story 3 - Transformation Engine (Week 9-10)

**Goal**: Group/entitlement transformation rules defined, engine applies transformations, reverse transforms work

- [ ] US3-T1: Design transformation rule format (JSON/YAML DSL)
- [ ] US3-T2: Implement TransformationEngine (load rules, match group, apply transformation)
- [ ] US3-T3: Implement transformation rule validation (syntax, conflicts, missing mappings)
- [ ] US3-T4: Implement reverse transformation (entitlement→group for drift detection)
- [ ] US3-T5: Implement transformation error handling (missing mapping, ambiguous match)
- [ ] US3-T6: Implement transformation preview API (show what transformation will happen without applying)
- [ ] US3-T7: Create example transformation rules for Salesforce (group→role), Workday (group→org_hierarchy), ServiceNow (group→group)
- [ ] US3-T8: Unit tests for transformation engine (rule matching, application, reverse)
- [ ] US3-T9: Integration tests (SDK applies transformations before calling adapter)

**Independent Test**: Define transformation rule, group operation is transformed per rule, reverse transformation recovers original group representation

### Phase 6: User Story 4 - Change Detection & Drift (Week 11-12)

**Goal**: SDK can poll provider, detect changes, report drift, identify conflicts

- [ ] US4-T1: Implement SyncState (track last sync timestamp, last known state snapshot, sync status)
- [ ] US4-T2: Implement ChangeDetector (compare current provider state with last known state)
- [ ] US4-T3: Implement drift report generation (what changed, when, old vs new)
- [ ] US4-T4: Implement drift logging to Application Insights
- [ ] US4-T5: Implement drift conflict detection (same resource changed in both Entra and provider)
- [ ] US4-T6: Implement reconciliation strategies (auto-apply based on direction, manual review, ignore)
- [ ] US4-T7: Implement scheduled polling service (trigger change detection via timer)
- [ ] US4-T8: Integration tests (poll provider, detect changes, generate drift report)

**Independent Test**: Poll provider, SDK detects new/modified/deleted users and groups, drift report includes all changes with timestamps

### Phase 7: User Story 5 - Sync Direction Toggle (Week 13-14)

**Goal**: Sync direction can be toggled between push/pull, direction enforced, toggle logged

- [ ] US5-T1: Implement SyncDirection enum (ENTRA_TO_SAAS, SAAS_TO_ENTRA)
- [ ] US5-T2: Implement sync direction persistence (stored in config, survives restart)
- [ ] US5-T3: Implement direction enforcement (only apply operations from active direction)
- [ ] US5-T4: Implement direction toggle logging (log all direction changes to audit log)
- [ ] US5-T5: Implement "opposite direction" logging (log as informational when changes arrive from inactive direction)
- [ ] US5-T6: Implement graceful direction toggle during active sync (complete cycle, apply new direction next cycle)
- [ ] US5-T7: Integration tests (toggle direction, verify operations apply only for active direction)

**Independent Test**: Switch direction from push to pull, verify next sync pulls from provider; switch back to push, verify next changes push to Entra

### Phase 8: Adapters - Example Implementations (Week 15-16)

**Goal**: Adapter implementations for Salesforce, Workday, ServiceNow (example adapters, real-world tested)

- [ ] A1-T1: Salesforce adapter (createUser, readUser, updateUser, deleteUser via Salesforce REST API)
- [ ] A1-T2: Salesforce adapter (group→role mapping, mapGroupToEntitlements implementation)
- [ ] A1-T3: Salesforce adapter tests (contract, integration, error handling)
- [ ] A2-T1: Workday adapter (user/group operations via Workday REST API)
- [ ] A2-T2: Workday adapter (group→org_hierarchy mapping)
- [ ] A2-T3: Workday adapter tests
- [ ] A3-T1: ServiceNow adapter (user/group operations via ServiceNow REST API)
- [ ] A3-T2: ServiceNow adapter (group support native)
- [ ] A3-T3: ServiceNow adapter tests

**Independent Test**: Each adapter can be swapped in, end-to-end flows work (create user Entra→provider, detect changes)

### Phase 9: Security & Performance Hardening (Week 17-18)

**Goal**: Security tests pass, performance targets met, code coverage >80%

- [ ] S1: Token validation security tests (invalid token, expired token, cross-tenant token)
- [ ] S2: Tenant isolation security tests (verify cross-tenant data access impossible)
- [ ] S3: PII redaction tests (sensitive data removed from logs)
- [ ] S4: Credential handling tests (no credentials in code, logs, or config files)
- [ ] S5: Load testing (1000 req/s, p95 <2s latency)
- [ ] S6: Code coverage analysis (aim for >80%)
- [ ] S7: Security scanning (dependency vulnerabilities, SAST)

### Phase 10: Documentation & Deployment (Week 19-20)

**Goal**: Complete documentation, deployment guide, runbooks, ready for production

- [ ] D1: Write SDK developer guide (how to build adapters)
- [ ] D2: Write adapter interface contract documentation
- [ ] D3: Write transformation rule configuration guide
- [ ] D4: Write deployment guide (Azure Functions, IaC, Key Vault setup)
- [ ] D5: Write runbook for common operations (toggle direction, view drift, reconcile)
- [ ] D6: Create example adapter (template for new providers)
- [ ] D7: Generate API documentation (OpenAPI/Swagger)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitutional violations identified. All principles explicitly addressed in specification.

---

**Version**: 1.0.0 | **Date**: 2025-11-22 | **Status**: Ready for Phase 0 Research
