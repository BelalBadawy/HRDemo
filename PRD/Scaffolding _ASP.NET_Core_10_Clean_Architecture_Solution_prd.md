# Product Requirement Document (PRD): Scaffolding ASP.NET Core 10 Clean Architecture Solution (HrDemo)

This PRD outlines the requirements, architectural standards, user stories, and testing/implementation decisions for scaffolding the `HrDemo` solution skeleton. The goal is to establish a rock-solid, secure, and testable foundation following Clean Architecture principles under .NET 10.

---

## Problem Statement

Starting a new enterprise Web API project without a clear, validated architectural foundation leads to technical debt, tight coupling, inconsistent error handling, and security vulnerabilities. Developers need a standard, buildable, and testable skeleton that establishes the Clean Architecture pattern, sets up central package management, implements standard pipeline behaviors (logging, authorization, validation), and builds a secure Authentication and Identity foundation using ASP.NET Core Identity with hashed rotating refresh tokens. Without this scaffolding, subsequent feature development will lack consistency, causing bugs, security holes, and code rot.

---

## Solution

Scaffold a complete, buildable, and testable .NET 10 Clean Architecture solution skeleton for `HrDemo`. The solution implements a strict four-layer architecture (Domain, Application, Infrastructure, API) where dependencies flow inward, exposing only clean Application abstractions to the API. It embeds a robust Authentication/Identity foundation supporting user registration, secure login, rotating and hashed refresh tokens under a Single Refresh Token Policy, claims-based permission authorization, and sequential domain event dispatching after successful database commits. All endpoints are mapped using Minimal APIs, using a source-generator-based mediator library for CQRS.

---

## User Stories

1. As an administrator, I want to seed default roles and permissions during database initialization, so that the application has a consistent and functional authorization model from startup.
2. As an administrator, I want a default Admin user to be seeded with secure credentials, so that I can log in and perform administrative tasks immediately after deployment.
3. As a prospective user, I want to register a new account by providing a username, email, and password, so that I can access the system's secured features.
4. As the registration service, I want to reject duplicate user registrations, so that email addresses and usernames remain unique across the application.
5. As a registered user, I want to authenticate using my credentials, so that I receive an access token and a refresh token to securely interact with the system.
6. As a security officer, I want the system to enforce lockouts on accounts after multiple failed login attempts, so that the system is protected against brute-force attacks.
7. As a logged-in user, I want to access resources requiring authorization using my JWT access token, so that I am authenticated based on my claims.
8. As a client application, I want to refresh a user's session using their refresh token before the access token expires, so that the user stays logged in seamlessly.
9. As a security architect, I want the system to rotate the refresh token on every refresh request, so that refresh token theft can be mitigated.
10. As a security architect, I want the system to store only the SHA256 hash of refresh tokens rather than their plain text, so that if the database is compromised, the tokens cannot be used.
11. As a security architect, I want to enforce a Single Refresh Token Policy per user, so that a user cannot have multiple active refresh tokens concurrently.
12. As a user logging in from a second device, I want my new login to replace the previous device's refresh token, so that my active session is centralized and previous device access is revoked.
13. As a user, I want to log out of the application, so that my current session and associated refresh token are destroyed and cannot be used again.
14. As an API client, I want validation errors to return a clear, structured JSON response with a validation error status, so that I can display helpful error messages to the end user.
15. As an API client, I want unexpected server errors to be captured globally and returned in a consistent JSON response format, so that internal system details are not leaked.
16. As a system administrator, I want a liveness health check endpoint that does not query the database, so that the container orchestrator knows the app service is running.
17. As a system administrator, I want a readiness health check endpoint that verifies the EF Core database connection, so that the container orchestrator knows the app is ready to serve traffic.
18. As a developer, I want to run migrations from the CLI using a design-time DbContext factory, so that I can easily update the database schema without running the main web project.
19. As an auditor, I want all auditable entities to automatically record who created or modified them and when, so that there is a reliable audit trail of data modifications.
20. As a developer, I want all asynchronous operations to accept a cancellation token, so that database queries and other async tasks can be cancelled when the HTTP request is aborted.
21. As an API developer, I want logging behavior to automatically mask sensitive fields (such as passwords, tokens, and secrets), so that we do not write sensitive information into the application logs.
22. As an API consumer, I want endpoints protected by rate limiting, so that the authentication system is not overwhelmed by denial of service attacks.
23. As a developer, I want a central package management file, so that all project dependencies use a single source of truth for their versions.
24. As a developer, I want automated tests to verify the rotation, optimistic concurrency protection, and revocation of refresh tokens, so that security regressions are caught early.

---

## Implementation Decisions

### Architectural Decisions & Project Structure
- **Four-Layer Clean Architecture Layout**:
  - `HrDemo.Domain` references nothing. It contains pure aggregates, entities, value objects, exceptions, and events.
  - `HrDemo.Application` references `HrDemo.Domain` only. It handles use cases (CQRS Commands/Queries), validation, and abstracts infrastructure logic.
  - `HrDemo.Infrastructure` implements Application interfaces (persistence, token generation, identity, clock). It is the *only* project referencing EF Core, SQL Server, and ASP.NET Core Identity.
  - `HrDemo.API` references `HrDemo.Application` and `HrDemo.Infrastructure`. It exposes Minimal API endpoints, configures middlewares, and maps endpoints.
- **CQRS and Mediator Implementation**:
  - Use `Mediator.SourceGenerator` and `Mediator.Abstractions` for compile-time source-generated mediation. The generator package is only referenced in the API project, and abstractions are referenced in Application.
  - Endpoint handlers inject `ISender` instead of `IMediator`.
- **Response Result Pattern**:
  - All application use cases return a unified `ResponseResult` or `ResponseResult<T>` containing success status, data, error details, and status codes.
  - The Domain layer is completely free of `ResponseResult` references.
- **Pipeline Behaviors Order**:
  - Outermost: `LoggingBehavior` (tracks request execution time, logs requests/responses, and masks sensitive fields like `Password`, `RefreshToken`, `AccessToken`, `Jwt`, `Authorization`, `Secret`, `ClientSecret`).
  - Middle: `AuthorizationBehavior` (intercepts requests implementing `IAuthorizeRequest` and evaluates permission claim policies before validating input, ensuring unauthorized users are rejected early).
  - Innermost: `ValidationBehavior` (executes FluentValidation rules; prevents command handler execution if validation fails).

### Security & Authentication Decisions
- **Single Refresh Token Policy and Hash Storage**:
  - The database stores a `RefreshToken` entity with a one-to-one relationship to `ApplicationUser` using `UserId` as both primary key and foreign key.
  - Plaintext refresh tokens are never persisted or logged. A SHA256 hash is generated and stored in `TokenHash`.
  - Every login replaces/creates the record. Every refresh rotates the hash and updates the metadata (Client IP, User Agent, expiration, access token JTI). Every logout deletes the record.
- **Concurrency Control in Rotation**:
  - `RefreshToken` entity includes a `RowVersion` byte array for optimistic concurrency.
  - If concurrent refresh requests occur (e.g. from rapid client retries), `DbUpdateConcurrencyException` is caught inside the service/handler and mapped to a `ResultStatus.Conflict` or `ResultStatus.Unauthorized` instead of throwing a 500 error.
- **Domain Event Dispatch Timing**:
  - Domain events are defined as pure `IDomainEvent` markers.
  - Events are collected in entities and dispatched *sequentially* (via Mediator's `Publish` method inside a loop after wrapping in a `DomainEventNotification<TEvent>` wrapper) only after a successful database commit in `SaveChangesAsync`.
- **Database Migrations and CLI Tooling**:
  - EF Core migrations are placed in `HrDemo.Infrastructure`.
  - A design-time factory (`IDesignTimeDbContextFactory<ApplicationDbContext>`) is implemented in `Infrastructure` so that CLI tooling can run migrations without running the API host.
- **Central Package Management**:
  - Use a centralized `Directory.Packages.props` file to enforce package versions across all projects.
- **Health Checks Design**:
  - `/health/live`: Fast check returning 200 without DB query.
  - `/health/ready`: Ready status check that queries the DB using EF Core's built-in DbContext check.
- **Rate Limiting**:
  - Configure rate limiters on `/api/v1/auth/login` and `/api/v1/auth/refresh` using ASP.NET Core's rate-limiting middleware to guard against abuse.

---

## Testing Decisions

- **Test Naming Convention**: All test names follow the `Given_When_Then` pattern.
- **Testing Seams**:
  - **Unit Testing (Domain & Application)**: Test domain logic, entity creation, validator logic, pipeline behaviors, and CQRS handlers. External dependencies are mocked using NSubstitute.
  - **Integration Testing (Infrastructure)**: Test database configurations, Identity stores, JWT token generation, and the DbContext interceptors with a real SQL Server database instance.
  - **Functional Testing (API)**: End-to-end HTTP tests utilizing `WebApplicationFactory` to spin up an in-memory test host, making HTTP requests to Minimal API endpoints and asserting the JSON results.
- **External Behavior Testing**:
  - Tests will focus on verifying external behavior and contracts (e.g. correct HTTP status codes, correct ResponseResult shapes, correct claims structure, and expected side effects like database changes) rather than testing private implementation details.
- **Specific Scenarios to Verify**:
  1. Refresh token rotation invalidates the previous token.
  2. Concurrent refresh requests (optimistic concurrency `DbUpdateConcurrencyException` captured and mapped to 409/401 result).
  3. Login from a second device replaces the first refresh token.
  4. Logout removes the refresh token record.
  5. Domain events are adapted into `DomainEventNotification` wrappers and dispatched sequentially via a foreach loop only after successful database commit.
  6. Authorization behavior short-circuits execution before validation.
  7. Validation behavior prevents handler execution.
  8. `CancellationToken` is propagated to all async operations.
  9. Logging behavior masks sensitive fields (Password, Tokens, Secrets).
  10. Refresh token hashes are never persisted or logged in plain text.
  11. Login with expired password (lockout validations).
  12. Refresh using an expired token returns 401 Unauthorized.
  13. Refresh with an invalid jti returns 401 Unauthorized.
  14. Refresh after password change (security stamp mismatch) fails validation.
  15. Refresh after logout fails (no token record found).
  16. Role assignment updates authorization claims immediately in the next access token.
  17. Duplicate user registration returns 409 Conflict.
  18. Validator failures short-circuit request execution before database access is made.
  19. Database infrastructure connection failures are safely translated into a 500 ResponseResult.
  20. Domain events are not dispatched if `SaveChangesAsync` fails.

---

## Out of Scope

- Implementation of business features (e.g., employee directory, payroll, leave requests).
- Integration with external email services (SES, SendGrid) or SMS providers (only placeholders/interfaces).
- Background jobs scheduling infrastructure (Quartz, Hangfire) beyond interface placeholders.
- Real caching infrastructure (Redis/Distributed Cache) or outbox processor logic (beyond basic outbox message entity scaffolding).
- Real production database provisioning (local DB or SQL Server Docker container only).

---

## Further Notes

- **Consistency Trade-off**: The eventual consistency trade-off of dispatching domain events post-save (commit) means that if the event handling fails, the original database transaction is already committed. In subsequent phases, a transaction outbox pattern should be implemented to ensure reliable processing of events.
- **Secrets Management**: All secrets like JWT signing key, database connection strings, and seed passwords must be loaded from user-secrets or environment variables in development, and never hardcoded in settings or source files.
