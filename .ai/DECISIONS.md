# Architectural Decisions

This document details the architectural decisions and design patterns evident in the **HrDemo** codebase, outlining the context, decisions, and trade-offs of each.

---

## 1. Clean Architecture (Four-Layer Separation)

- **Context**: The application requires a maintainable, extensible structure that prevents business logic from coupling to database technologies or transport frameworks.
- **Decision**: Implemented a four-project structure: `Domain` ➔ `Application` ➔ `Infrastructure` ➔ `API`.
- **Trade-offs**:
  - *Advantages*: Clear boundaries, independent layer testing, and direct enforcement of dependency directions. Interfaces are declared in the Application layer and implemented in Infrastructure, allowing the database to be swapped or mocked easily.
  - *Disadvantages*: Requires mapping objects or writing wrapper structures across layer boundaries (e.g. converting database configurations, exposing commands as DTOs).

---

## 2. Compile-Time Mediator Source Generator (`Mediator.SourceGenerator`)

- **Context**: Traditional CQRS handlers using MediatR rely on runtime reflection to locate and bind commands/queries to handlers, increasing cold-start times and memory usage.
- **Decision**: Adopted Martin Othamar's compile-time `Mediator` implementation.
- **Trade-offs**:
  - *Advantages*: Zero runtime reflection; command and handler bindings are resolved at compile time, leading to faster startup times and lower CPU overhead.
  - *Disadvantages*: Relies on third-party generators which may have slower build times and fewer community plugins than the standard MediatR package.

---

## 3. Centralized Standardized Response (`ResponseResult`)

- **Context**: APIs often return mixed response shapes (e.g., raw arrays, standard types, or custom errors), making it difficult for clients to parse results consistently.
- **Decision**: Wrapped all Minimal API responses in a unified `ResponseResult` or `ResponseResult<T>` structure.
- **Trade-offs**:
  - *Advantages*: Consistent payload structure for success (data returned) and failure (validation dictionary or status messages). Centralizes HTTP status code mapping.
  - *Disadvantages*: Adds minor boilerplate code for handlers and endpoints. Prevents direct integration with standard `ProblemDetails` specifications without translation.

---

## 4. ASP.NET Core Identity with Integer Keys

- **Context**: The default ASP.NET Core Identity installation maps tables to Guid-string primary keys, which can cause index fragmentation and slower join query speeds in relational databases.
- **Decision**: Explicitly configured identity options to use `int` keys (`IdentityUser<int>`, `IdentityRole<int>`).
- **Trade-offs**:
  - *Advantages*: Higher database indexing performance and smaller storage size for primary/foreign keys in SQL Server.
  - *Disadvantages*: Integer primary keys are sequential and guessable (which requires securing endpoints so users cannot guess ID values to perform ID harvesting).

---

## 5. Single Refresh Token Policy & Cryptographic Rotation

- **Context**: Persistent sessions must be secure against token theft. Storing raw tokens in database tables is vulnerable to SQL injection exposures. Additionally, multiple active session tokens per user can cause session bloat.
- **Decision**:
  - Stored a SHA256 cryptographic hash (`TokenHash`) of the refresh token. Raw plaintext tokens are never logged or stored.
  - Implemented a **Single Active Session** policy. The `RefreshToken` table uses `UserId` as both the primary key and foreign key (1-to-1 relationship). Any login from a new client rotates the session, writing over the previous row and invalidating prior sessions.
- **Trade-offs**:
  - *Advantages*: High security (compromised database backups do not expose usable credentials). Eliminates stale session accumulation.
  - *Disadvantages*: Users cannot maintain simultaneous active sessions on multiple devices (e.g., phone and laptop) without logging each other out.

---

## 6. Concurrency Token Control on Session Rotation

- **Context**: Fast, concurrent client retries (e.g. from network failures or multiple simultaneous requests using the same refresh token) can lead to race conditions during token rotation.
- **Decision**: Configured a row version concurrency token (`RowVersion` byte array) on the `RefreshToken` table.
- **Trade-offs**:
  - *Advantages*: Catches database concurrency updates safely (`DbUpdateConcurrencyException`), allowing the application to reject replayed rotation requests with a 409 Conflict / 401 Unauthorized status rather than creating multiple active sessions.
  - *Disadvantages*: Requires client-side handlers to recover from concurrency conflicts.

---

## 7. Granular Claims-Based Permissions

- **Context**: Gating API endpoints using coarse role boundaries (e.g., `[Authorize(Roles = "Admin")]`) lacks the granularity needed for scaling business access rules.
- **Decision**: Gated routes with fine-grained claim permissions (e.g. `roles.assign`). The `AuthorizationBehavior` intercepts mediator commands, verifying permissions claims of the `ICurrentUser` before executing validators or command handlers.
- **Trade-offs**:
  - *Advantages*: Extremely granular access control. Decouples endpoint restrictions from user role names. Gating at the mediator level prevents unauthorized database updates.
  - *Disadvantages*: Requires extra claim mappings and policy configurations in the API startup pipeline.

---

## 8. Post-Commit Domain Event Dispatch

- **Context**: Raising domain events during database updates can result in side-effects (e.g. dispatching emails, notifying queues) executing even if the database transaction fails and rolls back.
- **Decision**: Implemented an EF Core Interceptor (`AuditableAndDomainEventsInterceptor`) that collects raised events during `SavingChangesAsync` and dispatches them via Mediator strictly *after* a successful transaction commit (`SavedChangesAsync`).
- **Trade-offs**:
  - *Advantages*: Guarantees database operations succeed before event side-effects run.
  - *Disadvantages*: If a post-commit handler fails, the database change is already committed. Eventual consistency must be resolved out-of-band or via an Outbox Pattern (which is planned on the future roadmap).

---

## 9. Adopted Mocking Library: NSubstitute

- **Context**: Unit testing commands, queries, and behaviors requires double/mock objects to isolate code under test.
- **Decision**: Adopted `NSubstitute` (v5.3.0) as the exclusive mocking framework.
- **Trade-offs**:
  - *Advantages*: Extremely clean and readable syntax using C# features (`Substitute.For<T>()` and Fluent syntax like `.Returns()`). Active ecosystem and compatibility.
  - *Disadvantages*: Requires learning specific syntax for argument matching (`Arg.Any<T>()`) and calls tracking.

---

## 10. Adopted Compile-Time Mediator Interfaces

- **Context**: Standard CQRS setup is built on top of Martin Othamar's source-generated Mediator.
- **Decision**: Adopted Mediator's built-in compile-time interfaces:
  - `IRequest<TResponse>` (represented by requests/commands/queries that return a standard `ResponseResult` or `ResponseResult<T>`).
  - `IRequestHandler<TRequest, TResponse>` (the handler signature).
- **Trade-offs**:
  - *Advantages*: Allows Mediator to source-generate the mapping and dispatch pipelines at compile time, eliminating runtime reflection.
  - *Disadvantages*: Restricts choices to the custom generator API (e.g. returning `ValueTask<T>` instead of `Task<T>`).

---

## 11. Permission Seeding Location & Mechanism (Implemented)

- **Context**: Granular claims-based authorization policies require a baseline list of permission claims to be seeded to specific roles or users upon system startup.
- **Decision**: Implemented `PermissionSeeder` class in `src/HrDemo.Infrastructure/Identity/` and wired it up during application startup in `Program.cs`. The seeder handles role creation, admin user creation using configurations in `appsettings.Development.json`, and granular permission claim assignments. It is built to be completely concurrency-safe.
- **Trade-offs**:
  - *Advantages*: Consolidates identity and authorization data management within the Infrastructure Identity project layer. It handles concurrent app instances safely without database unique constraint collisions.
  - *Disadvantages*: Requires explicit seeding execution logic on application start, which blocks web host initialization if database connections are slow (mitigated by crash-on-failure policy to avoid insecure startup).

---

## 12. Swagger Basic Auth Protection & Environment Gating

- **Context**: Visual API documentation and testing via Swagger UI is extremely valuable for developers and QA engineers. However, exposing the Swagger UI or OpenAPI document schema to the public creates a security vulnerability by mapping out the API footprint.
- **Decision**: Integrated Swashbuckle (Swagger UI) but restricted its registration and middleware to execution only in the `Development` environment. All requests matching `/swagger/*` (UI assets, pages, and the `/swagger/v1/swagger.json` document) are intercepted by a custom `SwaggerBasicAuthMiddleware` validating static credentials (`HrAdmin`/`HR@20226$`) defined in `appsettings.Development.json`.
- **Trade-offs**:
  - *Advantages*: Prevents public exposure of the API blueprint. Keeps developers productive in Development while ensuring complete protection. Gating both the UI pages and the raw JSON schema prevents bypassing the UI to download the blueprint directly.
  - *Disadvantages*: Requires manual credentials configuration and prevents Swagger UI usage in other environments (e.g. staging or production) without code changes.

---

## 13. Identity Lockout and Active User Policies

- **Context**: The identity system needs to defend against brute force attacks and allow administrators to deactivate accounts without deletion. Furthermore, deactivated users must not hold active sessions.
- **Decision**:
  - Configured ASP.NET Core Identity Lockout: 5 failed attempts, 15 minutes lockout duration, enabled for new users by default.
  - Added `IsActive` (bool) and `CreatedDate` (DateTimeOffset) properties to `ApplicationUser`.
  - Implemented account harvesting protection by performing a strict checking order at Login: Find user first (if null, return 401 "Invalid username or password."), then call `CheckPasswordSignInAsync`, then check lockout, and check `IsActive` only after a successful password verification.
  - Enforced `IsActive` verification during Refresh Token rotation. If the user is inactive at refresh time, the rotation fails and the user's stored refresh token row is immediately deleted (revoking the session).
- **Trade-offs**:
  - *Advantages*: High security against password guessing and immediate revocation of active sessions for suspended users. Distinct error responses ensure users understand why their access is blocked.
  - *Disadvantages*: Requires tracking lockout count on the database and revoking refresh tokens on a separate write operation during a read-refresh request.

---

## 14. Transport Gating for Client IP Address (Architectural Win)

- **Context**: The Single Refresh Token Policy tracks client IP addresses (`CreatedByIp`). However, passing IP addresses through CQRS command payloads (`LoginCommand`) leaks transport-layer details (HTTP HttpContext) into the Application layer, violating Clean Architecture principles.
- **Decision**: Removed `IpAddress` from all CQRS command payloads (`LoginCommand`, `RefreshCommand`, `LogoutCommand`). Instead, exposed the IP address on `ICurrentUser` interface (`IpAddress` property) and implemented it in the Infrastructure layer `CurrentUser` class using `IHttpContextAccessor`. The `RefreshTokenService` (Infrastructure) reads it directly from `ICurrentUser` during token operations.
- **Trade-offs**:
  - *Advantages*: Maintains a clean separation of concerns. The Application layer commands remain transport-agnostic (can be invoked from queue processors, gRPC, CLI, etc., without requiring fake IP arguments).
  - *Disadvantages*: Requires resolving `IHttpContextAccessor` in the CurrentUser dependency chain.

---

## 15. String Enum Serialization for ResponseResult Status

- **Context**: The `ResponseResult` status field represents enum options defined in `ResultStatus`. By default, these values were serialized as integers in JSON responses (e.g., `2` for `Unauthorized`), making them difficult to understand for client developers and API consumers without mapping documentation.
- **Decision**: Decorated the `ResultStatus` enum itself with `[JsonConverter(typeof(JsonStringEnumConverter))]`. This forces System.Text.Json to serialize the enum as strings (e.g., `"ValidationError"`, `"Unauthorized"`) universally.
- **Trade-offs**:
  - *Advantages*: Significantly improves API readability, debuggability, and clarity. Self-documenting JSON payloads. Fully compatible with bidirectional deserialization in functional tests.
  - *Disadvantages*: Increases payload size by a few bytes per request (negligible).



