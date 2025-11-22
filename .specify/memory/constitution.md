# SCIM Gateway Constitution

<!-- 
SYNC IMPACT REPORT: Constitution v1.0.0
- **Version Change**: Initial creation (0.0.0 → 1.0.0)
- **New Principles**: 5 core principles (Microsoft SCIM Compliance, Security-First, Comprehensive Audit Logging, Azure-Native Serverless, Test-First)
- **New Sections**: 3 additional sections (Compliance & Security, Azure Deployment & Operations, Quality Gates & Development Workflow)
- **Templates Updated**: None (first constitution ratification)
- **Ratification Date**: 2025-11-22
-->

## Core Principles

### I. Microsoft SCIM Specification Compliance (NON-NEGOTIABLE)
All implementation MUST conform to Microsoft's SCIM 2.0 specification as defined in the Azure AD SCIMReferenceCode repository (https://github.com/AzureAD/SCIMReferenceCode). Every SCIM endpoint, request/response schema, error format, and HTTP behavior MUST align with these specifications. Deviations require documented justification and cross-team approval.

### II. Security-First: Authentication & Authorization
Every request to the gateway MUST be authenticated using OAuth 2.0 Bearer tokens as specified by Microsoft's SCIM authentication guidelines. Authorization checks MUST verify tenant isolation and scope permissions. No endpoint is accessible without explicit authentication. Token validation failures MUST be logged with full context for security auditing.

### III. Comprehensive Audit Logging (NON-NEGOTIABLE)
Every CRUD operation (Create, Read, Update, Delete) on any SaaS system MUST be logged with full context: timestamp, actor (service principal), operation type, resource ID, old values (for updates), new values, result status, and any errors. Logs MUST be written to Azure Application Insights or equivalent Azure Monitor service. Audit logs MUST be immutable and queryable for compliance reviews.

### IV. Azure-Native Serverless Deployment
The gateway MUST run as an Azure Function (preferred) or similar serverless compute (e.g., App Service with auto-scaling). No monolithic servers or containers requiring manual scaling. All configuration MUST use Azure Key Vault for secrets. All telemetry MUST flow to Application Insights. Infrastructure MUST be Infrastructure-as-Code (Bicep or Terraform) for reproducibility.

### V. Test-First & Contract-Driven Development
All new SCIM endpoints or functionality MUST have passing contract tests that validate conformance with Microsoft's SCIM specification BEFORE implementation. Integration tests MUST verify end-to-end flows (e.g., Entra ID → Gateway → SaaS App). Unit tests MUST achieve >80% code coverage. No feature merges without test evidence of specification compliance.

## Compliance & Security Requirements

**Microsoft SCIM Compliance:**
- SCIM 2.0 Core Schema (RFC 7643) MUST be fully supported for Users and Groups
- HTTP status codes MUST match SCIM error specifications (400, 401, 403, 404, 409, 500, etc.)
- Batch operations and filtering MUST conform to specification limits
- Response times MUST meet expected SaaS performance profiles (p95 <2s for typical operations)

**Authentication & Authorization:**
- Entra ID MUST authenticate all incoming requests via OAuth 2.0 Bearer tokens
- Each tenant MUST be isolated; cross-tenant data access MUST be cryptographically impossible
- Service principal permissions MUST be scoped to specific SaaS systems via configuration
- Failed authentication attempts MUST be logged and rate-limited

**Data Protection:**
- In-transit encryption MUST use TLS 1.2+ (enforced by Azure)
- Credentials for downstream SaaS systems MUST be stored in Azure Key Vault, never in code or unencrypted storage
- Personally identifiable information (PII) in logs MUST be sanitized or redacted per compliance requirements
- Data retention policies MUST comply with customer contracts and GDPR/CCPA requirements

## Azure Deployment & Operations

**Compute:**
- Primary: Azure Functions (HTTP-triggered) with consumption or premium pricing tier
- Alternative: Azure App Service with auto-scaling based on CPU/memory metrics
- Function runtime: .NET 8+ or Node.js 20+ (performance-optimized stacks)

**Storage & Secrets:**
- Service principal credentials → Azure Key Vault (reference via managed identity)
- Audit logs → Application Insights (queryable, retentive, compliant)
- Operational state → Azure Cosmos DB (if session caching needed) or stateless design preferred
- Configuration → Azure App Configuration or environment variables via Key Vault

**Monitoring & Alerting:**
- Application Insights MUST track: request latency, error rates, SCIM compliance failures, authentication rejections, audit log successes
- Alerts MUST trigger on: error rate >1%, latency p95 >5s, authentication failure spikes, audit log write failures
- Dashboards MUST be accessible to ops team for real-time health monitoring

## Quality Gates & Development Workflow

**Code Review & Merge Gates:**
- All PRs MUST include: test evidence (contract + integration), audit log verification, security review for PII/secrets
- Specification compliance checklist MUST be signed off before merge
- No merge if code introduces authentication bypass, unlogged CRUD operations, or specification violations
- Breaking changes to SCIM endpoints MUST be documented and versioned

**Testing Requirements:**
- Contract tests MUST verify SCIM 2.0 compliance (via official SCIM test suites or custom validators)
- Integration tests MUST cover: Entra ID auth flow, tenant isolation, SaaS app connectivity, error handling
- Security tests MUST verify: no credentials in logs, token validation, rate limiting, PII redaction
- Load tests MUST demonstrate scalability (target: 1000 req/s per deployment unit) with <2s p95 latency

**Deployment:**
- Pre-prod environment MUST mirror production (same services, same compliance checks)
- Canary deployments RECOMMENDED for breaking SCIM changes
- Rollback plan REQUIRED for all production releases
- Post-deployment verification MUST validate all SCIM endpoints remain compliant

## Governance

**Constitution Authority:**
- This constitution supersedes all other coding or deployment guidelines for the SCIM Gateway project
- Any conflict between this document and external practices MUST be resolved in favor of the constitution
- Principle violations are grounds for blocking PR merges or halting deployments

**Amendment Procedure:**
- Amendments MUST be documented with: reason, affected principles, migration plan, approval (lead architect + security lead)
- Version bumps: MAJOR for principle removals/redefinitions, MINOR for new principles/requirements, PATCH for clarifications
- Amendment approval timeline: ≤2 business days for PATCH, ≤5 business days for MINOR, ≤10 days for MAJOR
- All prior versions MUST be retained in git history for audit traceability

**Compliance Review:**
- Weekly: Automated checks on audit logs for anomalies (failed auth spikes, compliance failures)
- Monthly: Manual review of test coverage, specification compliance, security incidents
- Quarterly: Comprehensive security audit by external team (if applicable)
- Annually: Constitution review and potential amendments based on evolving requirements

**Runtime Development Guidance:**
- Use `.specify/templates/` for all new feature planning and task organization
- Reference Microsoft's SCIMReferenceCode repository for endpoint examples and error handling patterns
- Use Azure best practices from official documentation (Cosmos DB, Functions, Key Vault patterns)
- Maintain this constitution as single source of truth; refer all design decisions back to these principles

---

**Version**: 1.0.0 | **Ratified**: 2025-11-22 | **Last Amended**: 2025-11-22
