# Codebase Conventions

This document outlines the coding patterns, architectural constraints, naming conventions, and testing guidelines observed in the **HrDemo** solution.

---

## 1. Project Conventions & Technology Stack

- **Target Framework**: .NET 10.0 (`net10.0`).
- **Nullability & Usings**: Global Nullable reference types (`<Nullable>enable</Nullable>`) and implicit using namespaces (`<ImplicitUsings>enable</ImplicitUsings>`) are enabled across all projects.
- **Analyzers**: Visual static analyzer `Meziantou.Analyzer` is active on build. Nullable warnings are treated as compile-time errors.
- **Dependency Management**: Central Package Management (CPM) is enabled via `Directory.Packages.props`. Individual `.csproj` files reference package names without version numbers.

---

## 2. Layer & Architectural Separation

- **Unidirectional Reference Direction**: Domain ➔ Application ➔ Infrastructure ➔ API.
- **Leakage Prevention**: No Entity Framework Core or ASP.NET Core Identity namespaces may leak into the Domain or Application projects. Data entities are mapped to abstract interfaces inside the Application project.
- **DI Registration**: Registrations must be consolidated inside `DependencyInjection.cs` files in their respective project layers (e.g. `AddApplicationServices()` in `HrDemo.Application`, `AddInfrastructureServices()` in `HrDemo.Infrastructure`). Direct registrations should not be added inline in the API `Program.cs`.

---

## 3. CQRS & Mediator Conventions

- **Mediator Pattern**: Martin Othamar's source-generated Mediator is utilized (`Mediator.SourceGenerator` and `Mediator.Abstractions`) rather than MediatR.
- **CQRS Requests**: Requests are declared as `public sealed record` types implementing:
  - `IRequest<ResponseResult>` (for commands modifying state without returning data).
  - `IRequest<ResponseResult<T>>` (for queries or commands returning created entities/keys).
- **Handlers**: Handlers are defined as `public sealed class` types implementing `IRequestHandler<TRequest, TResponse>`. Handlers run asynchronously returning a `ValueTask<TResponse>`.
- **API Invocation**: Minimal API endpoints inject `ISender` rather than `IMediator` to dispatch operations.
- **Object Mapping Standards**:
  - **Standard**: All future Domain entity features MUST implement manual mapping via custom static extension methods (e.g. `ToDto()` and `ToEntity()`) to translate entities into Dtos at the boundary, ensuring persistence models never leak.
  - **Exception**: The baseline Authentication slice is a temporary exception to this rule because it interacts directly with ASP.NET Core Identity types (`ApplicationUser` / `ApplicationRole`) encapsulated inside `IUserManager` and does not define custom Domain aggregate entities requiring mapping.


---

## 4. Response Shape & Result Pattern

- **Standard Wrapper**: Every command handler, query handler, and API endpoint must return a `ResponseResult` or `ResponseResult<T>`.
- **Properties**:
  - `Success` (bool): True if the action succeeded.
  - `Message` (string?): Optional success or warning message.
  - `Status` (ResultStatus): Enum categorization (`Success`, `ValidationError`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `Error`).
  - `StatusCode` (int): HTTP status code counterpart (e.g., 200, 201, 400, 401, 403, 404, 409, 500).
  - `Errors` (IDictionary<string, string[]>?): Field-specific validation failures.
  - `Data` (T?): The payload (for generic results).
- **HTTP Mapping**: API endpoints convert a `ResponseResult` into an `IResult` using the `result.ToHttpResult()` extension method defined in `ResponseResultExtensions.cs`. This serializes the result wrapper as JSON and attaches the corresponding HTTP status code.
- **Enum Serialization**: Enums in API responses (specifically `ResultStatus` in the `Status` property) are serialized as string values (e.g., `"ValidationError"`, `"Unauthorized"`) rather than integers. This is configured via the `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute on the enum declaration.

---

## 5. Input Validation

- **Framework**: FluentValidation is used exclusively.
- **Registration**: All validator definitions implementing `AbstractValidator<T>` are registered automatically via `AddValidatorsFromAssembly`.
- **Pipeline Interception**: The `ValidationBehavior` intercepts requests at the Mediator level, running all matching validators in parallel before executing command handlers.
- **Validation Errors**: Validator failures are grouped by field name and mapped to a `ResultStatus.ValidationError` (HTTP 400 Bad Request) containing the dictionary list of failures.

---

## 6. Security & Database Conventions

- **Primary Keys**: Every database entity uses a standard auto-incrementing `int` primary key (`Identity` column in SQL Server). GUID primary keys are avoided.
- **Identity Lockout settings**: Gated under ASP.NET Core Identity: max 5 failed attempts, 15 minutes lockout duration, enabled for new users by default. Lockout tracking uses `SignInManager.CheckPasswordSignInAsync` (without cookie authentication effects).
- **Identity Active Status & Auditing**: The `ApplicationUser` entity has `IsActive` (bool, default true) and `CreatedDate` (DateTimeOffset, populated in memory via `IClock` at instantiation). `IsActive` is validated at Login and Refresh. If an inactive user attempts to refresh, the rotation fails and the refresh token is deleted immediately.
- **Login Error Message Policy**: Login returns distinct, descriptive error messages for invalid credentials (`"Invalid username or password."`), inactive status (`"Account is inactive."`), and account lockout (`"Account is locked."`). A strict verification sequence prevents user account harvesting.
- **Single Refresh Token Policy (IP capture)**: Client IP addresses (`CreatedByIp` on `RefreshToken`) are resolved at the Infrastructure boundary via `ICurrentUser.IpAddress` (which resolves client IP from `HttpContext` via `IHttpContextAccessor`) instead of leaking connection details into Application layer CQRS command payloads.
- **Hashed Refresh Tokens**: Rotating session keys must be cryptographically hashed (using SHA256) before storing them in the database (`TokenHash` column). Plaintext refresh tokens are never persisted or logged.
- **Concurrency Protection**: The `RefreshToken` entity has a `RowVersion` byte array configured as a concurrency token (`IsRowVersion()` in EF Core config). Concurrency update conflicts throw a `DbUpdateConcurrencyException` which is caught in the rotation service and returned as a 409 Conflict.
- **Permission Policy**: Gated routes use claims-based permissions (e.g. `permission: roles.assign`). Endpoints call `.RequireAuthorization("policyName")` mapping to an authorization policy requiring the `"permission"` claim.
- **Swagger UI Protection**: In the `Development` environment, the Swagger UI and OpenAPI schema (under `/swagger/*`) are gated behind a custom Basic Authentication middleware (`SwaggerBasicAuthMiddleware`) using static credentials. Swagger is disabled in other environments to prevent API blueprint exposure.
- **Swagger JWT Support**: Swagger is configured with a global JWT Bearer security scheme definition to support authorized endpoint testing directly within the UI.

---

## 7. Domain Events & Auditing

- **Base Class**: Aggregate roots inherit from `BaseEntity`, exposing a protected list of `IDomainEvent` objects.
- **Entity Lifecycle**: Events are collected on the aggregate and cleared using `ClearDomainEvents()` prior to persistence operations.
- **Interception & Auditing**:
  - The `AuditableAndDomainEventsInterceptor` intercepts EF Core saving states.
  - In `SavingChangesAsync`, audit columns (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`) on entities inheriting `BaseAuditableEntity` are populated automatically using `IClock` and `ICurrentUser` values.
  - Domain events are collected in `SavingChangesAsync` and sequentially dispatched using `IDomainEventDispatcher` strictly *after* a successful transaction commit (`SavedChangesAsync`).

---

## 8. Test Conventions

- **Libraries**: xUnit is used for test structures, FluentAssertions for assertions, and NSubstitute for mocking dependencies.
- **Unit Tests**:
  - Located in `HrDemo.Application.UnitTests`.
  - Follow the **Arrange-Act-Assert** pattern.
  - Test command handlers and FluentValidation scenarios.
- **Integration Tests**:
  - Located in `HrDemo.Infrastructure.IntegrationTests`.
  - Interact with actual database instances using custom connection strings pointing to LocalDB.
- **Functional Tests**:
  - Located in `HrDemo.API.FunctionalTests`.
  - Inherit from `IClassFixture<WebApplicationFactory<Program>>`.
  - Substitute interface mock definitions using `builder.ConfigureTestServices()`.
