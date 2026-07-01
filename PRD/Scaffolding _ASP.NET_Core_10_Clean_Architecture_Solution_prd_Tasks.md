# Implementation Plan: Scaffolding ASP.NET Core 10 Clean Architecture Solution (HrDemo) - Tasks

## Overview

This plan details the task breakdown for scaffolding a complete, buildable, and testable skeleton for the `HrDemo` solution using Clean Architecture principles under .NET 10. The plan covers setting up the core solution layout, central package management, a global exception handler, user registration, JWT login, rotating and hashed refresh tokens under a Single Refresh Token Policy with concurrency protection, session revocation/logout, claims-based permission authorization, and post-commit domain event dispatching.

---

## Architecture Decisions

- **Four-Layer Flow**: Strictly enforce Domain -> Application -> Infrastructure -> API dependency flow. No EF Core or Identity leakage above the Infrastructure layer.
- **Mediator Source Generation**: Use Martin Othamar's `Mediator` to generate CQRS handlers at compile-time, optimizing runtime execution. Endpoints inject `ISender` instead of `IMediator`.
- **Response Result Pattern**: Wrap all endpoint and application responses in a unified `ResponseResult<T>` structure to keep domain entities free of transport concerns.
- **Pipeline Behaviors Order**: Execute logging outermost (with sensitive field masking), authorization middle (short-circuiting early), and validation innermost.
- **Hashed Refresh Token & Single Token Policy**: Store a SHA256 hash of the token. A user has a single refresh token record mapped 1-to-1 in the database.
- **Optimistic Concurrency**: Catch `DbUpdateConcurrencyException` on token rotation and map it to a 409/401 ResponseResult.
- **Post-Commit Event Dispatch**: Collect domain events inside the entities and dispatch them sequentially via the mediator only after a successful database commit.

---

## Task List

### Phase 1: Solution Infrastructure & Base Structure

#### Task 1: Setup Solution Projects and Central Package Management (CPM)
- **Description**: Initialize the `HrDemo.slnx` solution file, central build settings in `Directory.Build.props` (adding warning rules and the analyzer package), and the centralized package list in `Directory.Packages.props`. Scaffold the projects (`Domain`, `Application`, `Infrastructure`, `API`, and tests).
- **Acceptance criteria**:
  - [x] `HrDemo.slnx` compiles cleanly.
  - [x] `Directory.Packages.props` is used as the single source of version numbers.
  - [x] Static analyzer Meziantou is active on build.
- **Verification**:
  - [x] `dotnet restore` succeeds.
  - [x] `dotnet build` succeeds with zero errors/warnings.
- **Dependencies**: None
- **Files likely touched**:
  - `HrDemo.slnx`
  - `Directory.Build.props`
  - `Directory.Packages.props`
  - `src/HrDemo.Domain/HrDemo.Domain.csproj`
  - `src/HrDemo.Application/HrDemo.Application.csproj`
  - `src/HrDemo.Infrastructure/HrDemo.Infrastructure.csproj`
  - `src/HrDemo.API/HrDemo.API.csproj`
- **Estimated scope**: Medium (6-8 configuration files)

#### Task 2: Health Checks and Global Exception Middleware
- **Description**: Add liveness (`/health/live`) and readiness (`/health/ready`) checks in API `Program.cs`. Implement `ExceptionHandlingMiddleware` to catch all unexpected errors and return a 500 status `ResponseResult`.
- **Acceptance criteria**:
  - [x] `/health/live` returns a 200 OK status immediately.
  - [x] `/health/ready` queries the DB context (using a placeholder DbContext) and verifies database health.
  - [x] Unhandled exceptions are mapped to 500 JSON response result.
- **Verification**:
  - [x] Functional test targeting health endpoints returns success.
  - [x] Request triggering exception returns 500 `ResponseResult` payload.
- **Dependencies**: Task 1
- **Files likely touched**:
  - `src/HrDemo.API/Program.cs`
  - `src/HrDemo.API/Middleware/ExceptionHandlingMiddleware.cs`
  - `tests/HrDemo.API.FunctionalTests/HealthCheckTests.cs`
- **Estimated scope**: Small (3 files)

#### Checkpoint: Foundation
- [x] Solutions and projects restore and build without warnings.
- [x] Liveness and readiness endpoints return appropriate status codes.

---

### Phase 2: Authentication Core & Persistence

#### Task 3: Base Entities, Database Persistence, and User Registration
- **Description**: Implement `BaseEntity`, `BaseAuditableEntity`, and `ApplicationUser`. Setup `IApplicationDbContext` and `ApplicationDbContext` with auditing interception. Implement `RegisterCommand` handler, validator, and endpoint (`/api/v1/auth/register`).
- **Acceptance criteria**:
  - [x] `RegisterCommand` validates input and registers the user.
  - [x] Audit fields (`CreatedAt`, `CreatedBy`) are populated automatically via interceptor.
  - [x] Duplicate registrations are rejected and return a 409 Conflict.
- **Verification**:
  - [x] Tests verify registration success, validation failures, and database persistence.
- **Dependencies**: Task 2
- **Files likely touched**:
  - `src/HrDemo.Domain/Common/BaseEntity.cs`
  - `src/HrDemo.Domain/Common/BaseAuditableEntity.cs`
  - `src/HrDemo.Domain/Entities/ApplicationUser.cs`
  - `src/HrDemo.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `src/HrDemo.Infrastructure/Persistence/Interceptors/AuditableAndDomainEventsInterceptor.cs`
  - `src/HrDemo.Application/Features/Authentication/Commands/Register/...`
  - `src/HrDemo.API/Endpoints/AuthEndpoints.cs`
  - `tests/HrDemo.Application.UnitTests/Features/Authentication/RegisterTests.cs`
- **Estimated scope**: Large (8-10 files across layers)

#### Task 4: JWT Generation and Login Flow
- **Description**: Implement the `JwtTokenGenerator` in the infrastructure layer. Securely load parameters from configuration. Add Login CQRS Command and the endpoint `/api/v1/auth/login`. Set up JWT bearer authentication options in `Program.cs` with zero clock skew.
- **Acceptance criteria**:
  - [x] Valid credentials return an access token and a refresh token.
  - [x] Secrets (signing keys, etc.) are sourced from configuration, not code.
  - [x] Invalid credentials return 401 Unauthorized.
- **Verification**:
  - [x] Test verifying that login produces valid JWT token with user claim details.
- **Dependencies**: Task 3
- **Files likely touched**:
  - `src/HrDemo.Application/Abstractions/Authentication/IJwtTokenGenerator.cs`
  - `src/HrDemo.Infrastructure/Authentication/JwtTokenGenerator.cs`
  - `src/HrDemo.Application/Features/Authentication/Commands/Login/...`
  - `src/HrDemo.API/Endpoints/AuthEndpoints.cs`
  - `tests/HrDemo.Application.UnitTests/Features/Authentication/LoginTests.cs`
- **Estimated scope**: Medium (5-6 files)

#### Checkpoint: Authentication Core
- [x] User can register and log in via the HTTP endpoints.
- [x] Database updates audit fields automatically during registration.

---

### Phase 3: Session Management

#### Task 5: Single Refresh Token Policy & Concurrency Rotation
- **Description**: Scaffold the `RefreshToken` entity (1-to-1 relationship with `ApplicationUser` using `UserId` as PK/FK and `RowVersion` for concurrency). Implement the hashing logic for refresh tokens (SHA256). Add the Refresh command/endpoint. Implement rate-limiting middleware on auth endpoints. Catch `DbUpdateConcurrencyException` inside the handler and return a 409/401.
- **Acceptance criteria**:
  - [x] Plain text refresh tokens are never persisted or logged.
  - [x] Rotation updates the database record atomically with new hash, JTI, and metadata.
  - [x] Concurrency exception on simultaneous refresh calls returns 409/401.
- **Verification**:
  - [x] Concurrency tests simulating simultaneous refresh requests map correctly to non-500 errors.
- **Dependencies**: Task 4
- **Files likely touched**:
  - `src/HrDemo.Infrastructure/Identity/RefreshToken.cs`
  - `src/HrDemo.Application/Abstractions/Identity/IRefreshTokenService.cs`
  - `src/HrDemo.Infrastructure/Identity/RefreshTokenService.cs`
  - `src/HrDemo.Application/Features/Authentication/Commands/Refresh/...`
  - `src/HrDemo.API/Endpoints/AuthEndpoints.cs`
  - `tests/HrDemo.Infrastructure.IntegrationTests/RefreshTokenTests.cs`
- **Estimated scope**: Medium (6-8 files)

#### Task 6: Session Revocation & Logout
- **Description**: Implement the Logout CQRS Command and API endpoint `/api/v1/auth/logout` to delete the refresh token row.
- **Acceptance criteria**:
  - [x] Successful logout removes the user's refresh token row.
  - [x] Subsequent refresh attempts using the revoked token return 401.
- **Verification**:
  - [x] Functional test target logout, checking database state and verification of revoked token failures.
- **Dependencies**: Task 5
- **Files likely touched**:
  - `src/HrDemo.Application/Features/Authentication/Commands/Logout/...`
  - `src/HrDemo.API/Endpoints/AuthEndpoints.cs`
  - `tests/HrDemo.API.FunctionalTests/AuthLogoutTests.cs`
- **Estimated scope**: Small (3 files)

#### Checkpoint: Session Management
- [x] Hashed token rotation defends against concurrent request races.
- [x] Logout invalidates the session completely.

---

### Phase 4: Advanced Scaffolding & Events

#### Task 7: Authorization Behavior & Permission-Based Gating
- **Description**: Build the claims-based permission authorization handler. Implement `AuthorizationBehavior` within the mediator pipeline. Setup endpoints to require authorization policies (e.g. `/api/v1/auth/assign-role`).
- **Acceptance criteria**:
  - [x] `AuthorizationBehavior` short-circuits execution before validation behavior runs for unauthorized requests.
  - [x] AssignRole and AssignClaim endpoints update identity claims correctly.
- **Verification**:
  - [x] Test verifying validator bypass and early rejection of unauthorized mediator requests.
- **Dependencies**: Task 4
- **Files likely touched**:
  - `src/HrDemo.Application/Common/Behaviors/AuthorizationBehavior.cs`
  - `src/HrDemo.Application/Features/Authentication/Commands/AssignRole/...`
  - `src/HrDemo.Application/Features/Authentication/Commands/AssignClaim/...`
  - `tests/HrDemo.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs`
- **Estimated scope**: Medium (6-8 files)

#### Task 8: Post-Commit Domain Event Dispatcher
- **Description**: Define pure `IDomainEvent` marker. Build the `DomainEventNotification<TEvent>` and the post-commit event dispatcher. Configure `AuditableAndDomainEventsInterceptor` to dispatch events post-commit.
- **Acceptance criteria**:
  - [ ] Domain events are collected on the aggregate root and dispatched sequentially after database transaction success.
  - [ ] Events are not dispatched if SaveChanges fails.
- **Verification**:
  - [ ] Integration tests verify sequential post-commit execution of notifications.
- **Dependencies**: Task 3
- **Files likely touched**:
  - `src/HrDemo.Domain/Interfaces/IDomainEvent.cs`
  - `src/HrDemo.Application/Abstractions/Events/DomainEventNotification.cs`
  - `src/HrDemo.Infrastructure/Persistence/Interceptors/AuditableAndDomainEventsInterceptor.cs`
  - `src/HrDemo.Infrastructure/Persistence/Services/DomainEventDispatcher.cs`
  - `tests/HrDemo.Infrastructure.IntegrationTests/DomainEventTests.cs`
- **Estimated scope**: Medium (5-6 files)

#### Checkpoint: Solution Complete
- [ ] Full test suite execution is passing (`dotnet test`).
- [ ] Solution compiles cleanly, meets format rules, and database migrations are fully updated.

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Package leakage across boundaries | High | Enforce package dependencies and compile-time rules using visual inspection during code review. |
| JWT signing key exposure | High | Load credentials purely via configuration settings and throw exception on startup if default developer keys are found in production environment profiles. |
| Concurrency conflicts during token rotation | Med | Enforce EF Core optimistic concurrency tokens (`RowVersion`) and catch conflicts in application-level middleware to return appropriate status codes. |

---

## Open Questions

- Should we include a basic Dockerfile for containerization in the API project scaffolding?
- Should the default Admin user credentials be generated dynamically during database seeding and printed in the logs, or read from standard development environment options?
