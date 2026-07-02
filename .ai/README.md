# HrDemo Architecture and System Documentation

This directory contains system-level documentation and codebase mappings for the **HrDemo** solution—an ASP.NET Core 10 Clean Architecture application.

## Directory Index
- [Codebase Map](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CODEBASE_MAP.md) - A file-by-file blueprint of the solution.
- [Conventions Guide](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CONVENTIONS.md) - Code style, design patterns, and testing conventions used in the repository.
- [Architectural Decisions](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/DECISIONS.md) - Context and rationale behind key architectural patterns and trade-offs.
- [Next Steps Roadmap](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/NEXT_STEPS.md) - Scheduled roadmap, feature list, and backlog tasks.
- [Changelog](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CHANGELOG.md) - History of changes made to the codebase and documentation.
- [Session Summary](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/SESSION_SUMMARY.md) - Handoff state of the current work session.

## Verified Tech Stack

| Concern | Choice | Implementation Details |
|---|---|---|
| **Primary Keys** | `int` (Identity column) | Standard auto-incrementing integer keys on all entities. |
| **Mediator / CQRS** | `Mediator` (Martin Othamar) | Compile-time source generator (`Mediator.Abstractions` / `Mediator.SourceGenerator` v3.0.2). |
| **Object Mapping** | Manual mapping | Extension methods like `ToDto()` / `ToEntity()` (mandatory standard for future Domain slices). |
| **Database** | SQL Server | LocalDB/SQL Server via `Microsoft.EntityFrameworkCore.SqlServer` (v10.0.9). |
| **Auth** | ASP.NET Core Identity | Integer-keyed Identity with JWT bearer tokens (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` v10.0.9). |
| **Authorization** | Claims-based permission gating | Policy-based gating via `.RequireAuthorization("policy")` mapping to claims (e.g. `permission:roles.assign`). |
| **API Style** | Minimal APIs | Route groups mapped via static class methods (`AuthEndpoints.cs` in presentation layer). |
| **Response Shape** | `ResponseResult<T>` | Standard response envelope containing success, status code, data, and errors. |
| **Solution File** | `.slnx` | Modern visual studio solution definition format. |
| **Validation** | `FluentValidation` | Pipeline behaviors run validations in sequence (`FluentValidation` v12.1.1). |
| **Test Framework** | `xUnit` + `FluentAssertions` | Target tests using standard assertions (xUnit v2.9.3, FluentAssertions v7.2.0). |
| **Mocking** | `NSubstitute` | Substituted dependencies in test pipelines (`NSubstitute` v5.3.0). |
| **NuGet** | Central Package Management | Central package versioning via `Directory.Packages.props`. |

---

## 1. System Architecture

The solution implements **Clean Architecture** principles. The system is split into four concentric layers with a unidirectional dependency flow pointing inwards:

```
[ HrDemo.Domain ]
      ▲
      │ (references Domain only)
[ HrDemo.Application ]
      ▲              ▲
      │              │ (references Application & Domain)
[ HrDemo.Infrastructure ]
      ▲              ▲
      │              │ (references Application & Infrastructure)
[   HrDemo.API   ]
```

### Dependency Rules
- **HrDemo.Domain**: Zero dependencies on outer layers or external frameworks (no EF Core, no Identity, no Microsoft.AspNetCore namespaces).
- **HrDemo.Application**: Depends only on `HrDemo.Domain`. Declares interfaces for persistence, security, and time.
- **HrDemo.Infrastructure**: Implements interfaces declared in `HrDemo.Application`. Handles DB persistence (EF Core, SQL Server) and ASP.NET Core Identity.
- **HrDemo.API**: Depends on `HrDemo.Application` and `HrDemo.Infrastructure`. Acts as the presentation layer using Minimal APIs.

---

## 2. Project Catalog

The solution contains 8 projects organized into `src/` and `tests/` folders:

### Core Projects (`src/`)
1. **[HrDemo.Domain](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain)**
   - **Purpose**: Holds the enterprise domain model.
   - **Contents**: Base entity abstractions (`BaseEntity`, `BaseAuditableEntity`) and the `IDomainEvent` marker interface.
2. **[HrDemo.Application](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application)**
   - **Purpose**: Implements use-case logic, behaviors, validation, and core abstractions.
   - **Key Components**:
     - `Features/Authentication/Commands`: Use cases for user management and session control.
     - `Common/Behaviors`: Mediator behaviors for logging, authorization, and validation.
     - `Common/Results`: Custom `ResponseResult` wrapper for API output standardization.
3. **[HrDemo.Infrastructure](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure)**
   - **Purpose**: External integrations, database access, and identity implementations.
   - **Key Components**:
     - `Persistence/ApplicationDbContext`: EF Core context mapping to SQL Server.
     - `Identity`: Hashed Rotating Refresh Tokens implementation and ASP.NET Core Identity user/role managers.
     - `Persistence/Interceptors`: EF Core interceptor for populating audit properties and collecting domain events.
4. **[HrDemo.API](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API)**
   - **Purpose**: Web API entry point.
   - **Key Components**:
     - `Endpoints/AuthEndpoints`: Minimal API endpoint definitions.
     - `Middleware/ExceptionHandlingMiddleware`: Centralized global exception handler mapping failures to `ResponseResult`.

### Test Projects (`tests/`)
1. **[HrDemo.Domain.UnitTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.Domain.UnitTests)**
   - **Purpose**: Reserved for pure domain unit tests.
2. **[HrDemo.Application.UnitTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.Application.UnitTests)**
   - **Purpose**: Verifies CQRS commands, pipeline behaviors, and FluentValidation rules using xUnit and NSubstitute.
3. **[HrDemo.Infrastructure.IntegrationTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.Infrastructure.IntegrationTests)**
   - **Purpose**: Verifies repository and database lifecycle events (e.g. audit interceptors, database persistence).
4. **[HrDemo.API.FunctionalTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.API.FunctionalTests)**
   - **Purpose**: End-to-end endpoint tests hosting a test server via `WebApplicationFactory` to verify HTTP responses and JSON serialization.

---

## 3. Folder Structure Map

```
HrDemo.slnx                      # Modern SLNX solution definition
Directory.Build.props            # Shared compiler settings & analyzers
Directory.Packages.props          # Central Package Management definitions
src/
  HrDemo.Domain/
    Common/                      # BaseEntity, BaseAuditableEntity
    Interfaces/                  # IDomainEvent
  HrDemo.Application/
    Abstractions/                # Authentication, DateTime, Events, Identity interfaces
    Common/
      Behaviors/                 # Logging, Validation, Authorization behaviors
      Interfaces/                # IApplicationDbContext
      Results/                   # ResponseResult response wrapping pattern
    Features/
      Authentication/
        Commands/                # Register, Login, Refresh, Logout, AssignRole, AssignClaim
        Dtos/                    # Data transfer objects (LoginResponseDto)
  HrDemo.Infrastructure/
    Identity/                    # ApplicationUser, ApplicationRole, Services, Options
    Persistence/
      Configurations/            # EF Core Entity Configurations (RefreshTokenConfiguration)
      Interceptors/              # AuditableAndDomainEventsInterceptor
      ApplicationDbContext.cs    # EF Core DB context
    Services/                    # DomainEventDispatcher, SystemClock
  HrDemo.API/
    Endpoints/                   # AuthEndpoints minimal API routing mapping
    Extensions/                  # Helper extensions (ResponseResultExtensions)
    Middleware/                  # ExceptionHandlingMiddleware
    Program.cs                   # App bootstrap, DI configurations, and middlewares
    appsettings.json             # JWT configuration & Database connection strings
tests/
  HrDemo.Domain.UnitTests/       # Unit tests targeting domain logic
  HrDemo.Application.UnitTests/  # Unit tests targeting Commands and pipeline behaviors
  HrDemo.Infrastructure.IntegrationTests/ # Integration tests targeting persistence
  HrDemo.API.FunctionalTests/    # Functional tests targeting HTTP Minimal API routes
```

---

## 4. Completed Features

### Secure Identity Management
- **Registration**: `/api/v1/auth/register` lets new users sign up, validating credentials and confirming email/username uniqueness against the database.
- **Credential Login**: `/api/v1/auth/login` checks credentials using password hashes, generates JWT access tokens, and registers a session refresh token.
- **Claims & Roles Assignment**: Gated endpoints (`/api/v1/auth/assign-role` and `/api/v1/auth/assign-claim`) allow administrators to configure credentials.

### Rotating Session Control (Single Refresh Token Policy)
- **Hashed Refresh Tokens**: Refresh tokens are stored in the database as SHA256 hashes. Plaintext tokens are never persisted or logged.
- **Single Active Session**: Each user has exactly one active refresh token at any time (1-to-1 table relation mapped via `UserId` primary key). Logging in from a new client rotates the token, immediately invalidating the previous session.
- **Rotation Optimistic Concurrency**: Rotation swaps the access/refresh token pair. If concurrent calls attempt to rotate the same session, EF Core's `RowVersion` optimistic lock throws a `DbUpdateConcurrencyException`, returning a 409 Conflict / 401 Unauthorized.
- **Logout/Revocation**: Calling `/api/v1/auth/logout` deletes the user's refresh token row, ending the session.

### Post-Commit Domain Events
- **Collection**: Entities inherit `BaseEntity` which maintains a collection of raised `IDomainEvent`s.
- **Deferred Dispatch**: Inside the `AuditableAndDomainEventsInterceptor`, domain events are collected in `SavingChangesAsync` and cleared from entities. Once `SaveChangesAsync` succeeds, the events are dispatched sequentially via Mediator in `SavedChangesAsync`.
- **Failure Gating**: If the database transaction fails, events are not dispatched, preventing eventual consistency processes from executing on failed transactions.

### Request Pipeline Interceptors
- **LoggingBehavior**: Logs the request names and execution times, masking sensitive properties (`Password`, `RefreshToken`, `AccessToken`, etc.) automatically.
- **AuthorizationBehavior**: Intercepts commands implementing `IAuthorizeRequest` and validates that the `ICurrentUser` possesses the required claim permissions, short-circuiting execution before validation behaviors execute.
- **ValidationBehavior**: Gathers all validation failures via FluentValidation and returns a `ResultStatus.ValidationError` (400 Bad Request) containing grouped field errors.

---

## 5. Database Schema & Persistence

The application integrates with SQL Server. The persistence model maps domain events and identity details using EF Core configurations.

### Key Context
- **[ApplicationDbContext](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Persistence/ApplicationDbContext.cs)**: Integrates ASP.NET Core Identity stores with `int` primary keys (`ApplicationUser : IdentityUser<int>`, `ApplicationRole : IdentityRole<int>`).

### Tables Schema
1. **AspNetUsers** (`ApplicationUser`)
   - `Id` (int, Primary Key, Identity)
   - `UserName` (nvarchar)
   - `Email` (nvarchar)
   - `PasswordHash` (nvarchar)
   - Standard Identity fields (e.g., SecurityStamp, ConcurrencyStamp).
2. **AspNetRoles** (`ApplicationRole`)
   - `Id` (int, Primary Key, Identity)
   - `Name` (nvarchar)
   - Standard Identity role fields.
3. **AspNetUserClaims**
   - `Id` (int, Primary Key, Identity)
   - `UserId` (int, Foreign Key to AspNetUsers)
   - `ClaimType` (nvarchar)
   - `ClaimValue` (nvarchar) - Used to store permission claims.
4. **RefreshTokens** (`RefreshToken`)
   - `UserId` (int, Primary Key, Foreign Key to AspNetUsers, Cascade Delete)
   - `TokenHash` (nvarchar(256), Required)
   - `JwtId` (nvarchar(128), Required)
   - `ExpiryTime` (datetimeoffset, Required)
   - `CreatedAt` (datetimeoffset, Required)
   - `CreatedByIp` (nvarchar(50), Optional)
   - `RevokedAt` (datetimeoffset?, Optional)
   - `RevokedByIp` (nvarchar(50), Optional)
   - `RowVersion` (rowversion/timestamp, Concurrency Token)

---

## 6. API Endpoints Catalog

All endpoints return a standardized `ResponseResult` or `ResponseResult<T>` serialized as JSON.

| Method | Endpoint | Description | Auth Requirement | Rate Limit |
|---|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Registers a new user. | Anonymous | AuthPolicy |
| `POST` | `/api/v1/auth/login` | Log in and return JWT token pair. | Anonymous | AuthPolicy |
| `POST` | `/api/v1/auth/refresh` | Rotates JWT Access & Refresh tokens. | Anonymous | AuthPolicy |
| `POST` | `/api/v1/auth/logout` | Revokes the active session refresh token. | Anonymous | AuthPolicy |
| `POST` | `/api/v1/auth/assign-role` | Assigns an Identity role to a user. | `roles.assign` claim | - |
| `POST` | `/api/v1/auth/assign-claim`| Assigns a permission claim to a user. | `claims.assign` claim | - |
| `GET` | `/health/live` | Basic application liveness check. | Anonymous | - |
| `GET` | `/health/ready` | Core database connectivity check. | Anonymous | - |
