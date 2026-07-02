# Changelog

All notable changes to the **HrDemo** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-07-02

### Added
- Added `IsActive` (bool) and `CreatedDate` (DateTimeOffset) columns to `ApplicationUser` in `src/HrDemo.Infrastructure/Identity/ApplicationUser.cs`.
- Created EF Core migration `AddUserCreatedDateAndIsActive` with default values (`IsActive = 1`, `CreatedDate = GetUtcDate()`) applied for seeded/existing records.
- Added `IpAddress` (string?) property to `ICurrentUser` interface and implemented it in `CurrentUser.cs` using `IHttpContextAccessor` for boundary isolation.
- Created `IdentityServiceTests.cs` in `tests/HrDemo.Infrastructure.IntegrationTests/` to verify lockout rules, inactive login/refresh rejection, refresh token revocation, and IP address logging.

### Changed
- Configured identity services via `AddIdentity<ApplicationUser, ApplicationRole>` instead of `AddIdentityCore` in Infrastructure `DependencyInjection.cs`, setting lockout rules (5 attempts, 15 min duration).
- Updated default admin seeding in `PermissionSeeder.cs` to set `IsActive = true` and `CreatedDate` using `IClock`.
- Removed `IpAddress` parameter from CQRS commands (`LoginCommand`, `RefreshCommand`, `LogoutCommand`), handlers, and related interfaces (`IUserManager`, `IRefreshTokenService`).
- Implemented brute-force mitigation in `UserManagerService.LoginAsync` by evaluating existence first, performing password checks via `SignInManager` to trigger lockout counters, and checking `IsActive` status.
- Updated `RefreshTokenService.RotateTokenAsync` to check user `IsActive` status, immediately deleting the user's refresh token row and returning `401 Unauthorized` if inactive.
- Updated unit and functional tests (`LoginTests.cs`, `RefreshTests.cs`, `LogoutTests.cs`, `AuthEndpointsTests.cs`) to match signature updates and test inactive/lockout flows.

## [1.2.0] - 2026-07-02

### Added
- Integrated `Swashbuckle.AspNetCore` into the API project to generate visual OpenAPI documentation.
- Created `SwaggerBasicAuthMiddleware.cs` inside `src/HrDemo.API/Middleware/` to protect all `/swagger/*` routes (including UI assets and the raw OpenAPI JSON document) behind Basic Authentication.
- Created `SwaggerExtensions.cs` inside `src/HrDemo.API/Extensions/` to encapsulate service registrations and middleware wiring, enabling JWT Bearer token testing in the UI.
- Created `SwaggerAuthTests.cs` inside `tests/HrDemo.API.FunctionalTests/` to test Basic Authentication credentials, verifying unauthorized access returns `401 Unauthorized` while correct credentials return `200 OK`.

### Changed
- Configured static username and password settings under the `SwaggerAuth` section in `appsettings.Development.json`.
- Enabled compile-time XML documentation generation and suppressed CS1591 warnings inside `src/HrDemo.API/HrDemo.API.csproj`.
- Wired up Swagger services registration and middleware gating inside `src/HrDemo.API/Program.cs` to run exclusively in the `Development` environment.

## [1.1.0] - 2026-07-02

### Added
- Created `PermissionSeeder.cs` inside `src/HrDemo.Infrastructure/Identity/` to handle startup database seeding for identity roles ("Admin", "User"), baseline claims, and default administrator credentials.
- Created `PermissionSeederTests.cs` in `tests/HrDemo.Infrastructure.IntegrationTests/` verifying standard role creation, admin user and claim mappings, and idempotency across multiple execution passes.
- Registered `PermissionSeeder` in the dependency injection pipeline inside `DependencyInjection.cs` of the Infrastructure project.

### Changed
- Configured `DefaultAdmin` settings block in `appsettings.Development.json` containing default seed credentials.
- Modified `src/HrDemo.API/Program.cs` to resolve `PermissionSeeder` in a scoped sequence on host startup, executing database seeding synchronously and failing startup on database/connection exceptions.
- Updated `app.Run()` to `await app.RunAsync()` in `Program.cs` to resolve compiler warning CA1849.

## [1.0.0] - 2026-07-02

### Added
- Documentation synchronization pass. Initialized/updated all `.ai` documentation files to match existing codebase state perfectly:
  - Updated `README.md` with verified tech stack details (specifically centering on .NET 10 / ASP.NET Core 10).
  - Updated `CODEBASE_MAP.md` mapping file directories and adding the **Key Types Registry** section (`AddApplicationServices`, `AddInfrastructureServices`, `BaseAuditableEntity`, etc.).
  - Updated `DECISIONS.md` documenting decisions for `NSubstitute` mocking, compile-time `Mediator` interfaces, and planned permission seeding.
  - Updated `CONVENTIONS.md` mapping standard `IRequest<TResponse>` CQRS slices and setting mandatory manual mapping standards (using static `ToDto()` and `ToEntity()` methods) for future Domain aggregate entities.
  - Updated `NEXT_STEPS.md` referencing database seeding implementation tasks.
