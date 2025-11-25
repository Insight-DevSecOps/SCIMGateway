# SCIM Gateway SDK

[![CI/CD Pipeline](https://github.com/Insight-DevSecOps/SCIMGateway/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Insight-DevSecOps/SCIMGateway/actions/workflows/ci-cd.yml)

An extensible SCIM 2.0-compliant gateway SDK that abstracts SaaS provider integration patterns behind a standardized adapter interface.

## Features

- **SCIM 2.0 Compliance**: Full RFC 7643/7644 implementation for Users and Groups
- **Multi-SaaS Integration**: Adapter pattern for Salesforce, Workday, ServiceNow, and custom providers
- **Group/Entitlement Transformation**: Flexible mapping engine (EXACT, REGEX, HIERARCHICAL, CONDITIONAL)
- **Bidirectional Sync**: Change detection with drift reporting and conflict resolution
- **Enterprise Security**: OAuth 2.0, tenant isolation, comprehensive audit logging
- **Azure Native**: Key Vault, Cosmos DB, Application Insights integration

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)
- Visual Studio Code or Visual Studio 2022+

### Build & Run

```bash
# Clone the repository
git clone https://github.com/Insight-DevSecOps/SCIMGateway.git
cd SCIMGateway

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API locally
cd src/SCIMGateway.Api
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

### Project Structure

```
SCIMGateway/
├── src/
│   ├── SCIMGateway.Api/          # ASP.NET Core Web API (HTTP endpoints)
│   └── SCIMGateway.Core/         # Core SDK library (business logic)
├── tests/
│   └── SCIMGateway.Tests/        # xUnit tests (unit, integration, contract)
├── docs/                         # Documentation
├── deploy/
│   └── azure/                    # Azure deployment templates (Bicep/Terraform)
└── specs/                        # Feature specifications
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Entra ID                                  │
│                    (SCIM Client)                                 │
└─────────────────────────────────┬───────────────────────────────┘
                                  │ SCIM 2.0 (RFC 7643/7644)
                                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SCIM Gateway SDK                             │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │   Request    │ │   Schema     │ │     Authentication       │ │
│  │   Handler    │ │   Validator  │ │   (OAuth 2.0 Bearer)     │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │ Transformation│ │    Sync     │ │     Audit Logger         │ │
│  │    Engine    │ │   Engine    │ │  (Application Insights)  │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────┤
│  │                    Adapter Registry                          │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐ │
│  │  │Salesforce│ │ Workday  │ │ServiceNow│ │  Custom Adapter  │ │
│  │  │ Adapter  │ │ Adapter  │ │ Adapter  │ │                  │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘ │
│  └──────────────────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
    ┌──────────┐           ┌──────────┐           ┌──────────┐
    │Salesforce│           │ Workday  │           │ServiceNow│
    │   API    │           │   API    │           │   API    │
    └──────────┘           └──────────┘           └──────────┘
```

## SCIM Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/scim/v2/Users` | List users with filtering/pagination |
| `GET` | `/scim/v2/Users/{id}` | Get user by ID |
| `POST` | `/scim/v2/Users` | Create user |
| `PUT` | `/scim/v2/Users/{id}` | Replace user |
| `PATCH` | `/scim/v2/Users/{id}` | Update user (partial) |
| `DELETE` | `/scim/v2/Users/{id}` | Delete user |
| `GET` | `/scim/v2/Groups` | List groups with filtering/pagination |
| `GET` | `/scim/v2/Groups/{id}` | Get group by ID |
| `POST` | `/scim/v2/Groups` | Create group |
| `PUT` | `/scim/v2/Groups/{id}` | Replace group |
| `PATCH` | `/scim/v2/Groups/{id}` | Update group (partial) |
| `DELETE` | `/scim/v2/Groups/{id}` | Delete group |

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `AZURE_KEY_VAULT_URI` | Azure Key Vault URI for secrets | Yes |
| `AZURE_COSMOS_DB_CONNECTION` | Cosmos DB connection string (or use managed identity) | Yes |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string | Yes |

### appsettings.json

```json
{
  "ScimGateway": {
    "DefaultSyncDirection": "EntraToSaas",
    "AuditLogRetentionDays": 90,
    "RateLimiting": {
      "MaxRequestsPerMinute": 1000,
      "MaxFailedAuthAttempts": 5,
      "LockoutDurationMinutes": 15
    }
  }
}
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Contract"
```

### Code Formatting

```bash
# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

## Deployment

See [Deployment Guide](docs/deployment-guide.md) for detailed instructions.

### Quick Deploy to Azure

```bash
# Login to Azure
az login

# Deploy infrastructure
cd deploy/azure
az deployment group create \
  --resource-group scim-gateway-rg \
  --template-file main.bicep \
  --parameters @parameters.json
```

## Documentation

- [SDK Developer Guide](docs/sdk-developer-guide.md)
- [Adapter Interface](docs/adapter-interface.md)
- [Transformation Rules](docs/transformation-rules.md)
- [Operations Runbook](docs/operations-runbook.md)

## License

Proprietary - Insight DevSecOps

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
