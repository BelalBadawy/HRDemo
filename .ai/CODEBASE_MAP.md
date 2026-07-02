# Codebase Map

This file contains a structural index of the **HrDemo** solution codebase, organizing code and test files by architectural layer.

---

## 1. Domain Layer (`src/HrDemo.Domain/`)
The Domain layer contains all enterprise logic, base abstractions, and domain event structures. It is independent of EF Core, Identity, and HTTP logic.

- **[Common/BaseEntity.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain/Common/BaseEntity.cs)**
  - Abstract base class for all aggregate roots and entities.
  - Implements ID management (`int Id`) and domain events collection (`AddDomainEvent`, `ClearDomainEvents`).
- **[Common/BaseAuditableEntity.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain/Common/BaseAuditableEntity.cs)**
  - Inherits from `BaseEntity`.
  - Declares auditing columns: `CreatedAt`, `CreatedBy`, `LastModifiedAt`, and `LastModifiedBy`.
- **[Interfaces/IDomainEvent.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain/Interfaces/IDomainEvent.cs)**
  - Simple interface marker representing a domain event.
- **[GlobalUsings.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain/GlobalUsings.cs)**
  - System-wide domain using imports.

---

## 2. Application Layer (`src/HrDemo.Application/`)
The Application layer encapsulates application use-cases (CQRS commands/queries), FluentValidation rules, pipeline behaviors, and interfaces.

### Core Structure
- **[DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/DependencyInjection.cs)**
  - Extends `IServiceCollection` with `AddApplicationServices` to register validators and pipeline behaviors.

### Abstractions
- **[Abstractions/Authentication/IJwtTokenGenerator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Authentication/IJwtTokenGenerator.cs)**
  - Generates string access tokens based on user claims.
- **[Abstractions/DateTime/IClock.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/DateTime/IClock.cs)**
  - Wraps system clock details via `UtcNow` to make logic testable.
- **[Abstractions/Events/IDomainEventDispatcher.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Events/IDomainEventDispatcher.cs)**
  - Dispatches raised domain events after a successful commit.
- **[Abstractions/Events/DomainEventNotification.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Events/DomainEventNotification.cs)**
  - Wrapper mapping `IDomainEvent` to Mediator's `INotification` structure.
- **[Abstractions/Identity/ICurrentUser.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Identity/ICurrentUser.cs)**
  - Abstracts access to the current executing user's ID and permissions.
- **[Abstractions/Identity/IRefreshTokenService.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Identity/IRefreshTokenService.cs)**
  - Manages creation, rotation, and revocation of rotating session keys.
- **[Abstractions/Identity/IUserManager.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Abstractions/Identity/IUserManager.cs)**
  - Decoupled interface to perform Identity user creation, roles/claims mapping, and login verification.

### Common Pipelines & Models
- **[Common/Behaviors/LoggingBehavior.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Behaviors/LoggingBehavior.cs)**
  - Intercepts all Mediator requests, logs execution timings, and masks sensitive property keys (e.g. passwords, refresh tokens).
- **[Common/Behaviors/AuthorizationBehavior.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Behaviors/AuthorizationBehavior.cs)**
  - Validates `IAuthorizeRequest` requirements against `ICurrentUser` claims before executing handlers.
- **[Common/Behaviors/ValidationBehavior.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Behaviors/ValidationBehavior.cs)**
  - Automatically runs FluentValidation rules over incoming requests.
- **[Common/Behaviors/IAuthorizeRequest.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Behaviors/IAuthorizeRequest.cs)**
  - Interface implemented by requests requiring claims-based permission authorization.
- **[Common/Results/ResponseResult.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Results/ResponseResult.cs)**
  - Standardized application response wrapper defining Success, Message, Errors, StatusCode, and ResultStatus.
- **[Common/Results/ResultStatus.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Results/ResultStatus.cs)**
  - Enum containing response types (Success, ValidationError, Unauthorized, Forbidden, NotFound, Conflict, Error).
- **[Common/Interfaces/IApplicationDbContext.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Common/Interfaces/IApplicationDbContext.cs)**
  - Exposes `SaveChangesAsync` to the application layer.

### Features Slice: Authentication
- **[Features/Authentication/Dtos/LoginResponseDto.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Dtos/LoginResponseDto.cs)**
  - DTO returning JWT `AccessToken` and string `RefreshToken` on login/refresh.
- **Register User Feature**
  - **[Commands/Register/RegisterCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Register/RegisterCommand.cs)**: Request record with fields.
  - **[Commands/Register/RegisterHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Register/RegisterHandler.cs)**: Uses `IUserManager` to persist new user.
  - **[Commands/Register/RegisterValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Register/RegisterValidator.cs)**: Asserts format and checks username/email uniqueness.
- **Login User Feature**
  - **[Commands/Login/LoginCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Login/LoginCommand.cs)**: Request record with credentials.
  - **[Commands/Login/LoginHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Login/LoginHandler.cs)**: Handles user credentials verification.
  - **[Commands/Login/LoginValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Login/LoginValidator.cs)**: Asserts formatting requirements.
- **Rotate Session Feature**
  - **[Commands/Refresh/RefreshCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Refresh/RefreshCommand.cs)**: Request record with rotating refresh token.
  - **[Commands/Refresh/RefreshHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Refresh/RefreshHandler.cs)**: Delegates rotation logic to `IRefreshTokenService`.
  - **[Commands/Refresh/RefreshValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Refresh/RefreshValidator.cs)**: Verifies presence of token.
- **Revoke Session Feature**
  - **[Commands/Logout/LogoutCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Logout/LogoutCommand.cs)**: Request record containing refresh token to revoke.
  - **[Commands/Logout/LogoutHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Logout/LogoutHandler.cs)**: Invokes `RevokeTokenAsync`.
  - **[Commands/Logout/LogoutValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/Logout/LogoutValidator.cs)**: Verifies presence of token.
- **Assign Role Feature**
  - **[Commands/AssignRole/AssignRoleCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignRole/AssignRoleCommand.cs)**: Triggers role assignment. Requires permission `roles.assign`.
  - **[Commands/AssignRole/AssignRoleHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignRole/AssignRoleHandler.cs)**: Delegates to `IUserManager`.
  - **[Commands/AssignRole/AssignRoleValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignRole/AssignRoleValidator.cs)**: Asserts field details.
- **Assign Claim Feature**
  - **[Commands/AssignClaim/AssignClaimCommand.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignClaim/AssignClaimCommand.cs)**: Triggers permission claim mapping. Requires permission `claims.assign`.
  - **[Commands/AssignClaim/AssignClaimHandler.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignClaim/AssignClaimHandler.cs)**: Delegates to `IUserManager`.
  - **[Commands/AssignClaim/AssignClaimValidator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/Features/Authentication/Commands/AssignClaim/AssignClaimValidator.cs)**: Asserts mapping parameters.

---

## 3. Infrastructure Layer (`src/HrDemo.Infrastructure/`)
Houses Ef Core, DB Interceptors, and Identity implementations.

- **[DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/DependencyInjection.cs)**
  - Configures EF Core SQL Server, ASP.NET Core Identity Core stores, Jwt authentication options, clock registries, and pipeline interceptors.
- **[Identity/ApplicationUser.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/ApplicationUser.cs)**
  - Concrete Identity User model using `int` primary keys, referencing the active `RefreshToken?`.
- **[Identity/ApplicationRole.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/ApplicationRole.cs)**
  - Concrete Identity Role model using `int` primary keys.
- **[Identity/RefreshToken.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/RefreshToken.cs)**
  - Session key table columns including `UserId` (PK/FK), `TokenHash`, and optimistic locking concurrency properties (`RowVersion`).
- **[Identity/JwtOptions.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/JwtOptions.cs)**
  - Strongly typed options config mapping for JWT options.
- **[Identity/JwtTokenGenerator.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/JwtTokenGenerator.cs)**
  - Employs token security descriptors to issue JWT access keys.
- **[Identity/UserManagerService.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/UserManagerService.cs)**
  - Implements `IUserManager` via ASP.NET Core `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>`.
- **[Identity/PermissionSeeder.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/PermissionSeeder.cs)**
  - Seeds baseline system roles ("Admin", "User"), default permission claims, and default administrator credentials on host startup.
- **[Identity/RefreshTokenService.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/RefreshTokenService.cs)**
  - Implements `IRefreshTokenService`. Handles cryptographically hashed session rotation and revocation.
- **[Persistence/ApplicationDbContext.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Persistence/ApplicationDbContext.cs)**
  - Main DB context class mapping configurations and custom db sets.
- **[Persistence/Configurations/RefreshTokenConfiguration.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs)**
  - Entity configuration mapping properties, lengths, foreign keys, and optimistic lock rowversion constraints.
- **[Persistence/Interceptors/AuditableAndDomainEventsInterceptor.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Persistence/Interceptors/AuditableAndDomainEventsInterceptor.cs)**
  - Interceptor updating auditable fields and collecting raised events prior to saving, executing dispatcher events post-commit.
- **[Services/SystemClock.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Services/SystemClock.cs)**
  - Implementation of `IClock` returning `DateTimeOffset.UtcNow`.
- **[Services/DomainEventDispatcher.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Services/DomainEventDispatcher.cs)**
  - Dispatches events collected from interceptors wrapping them as notifications inside Mediator.

---

## 4. API Layer (`src/HrDemo.API/`)
The Minimal API layer hosting configuration settings and HTTP controllers.

- **[Program.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API/Program.cs)**
  - Defines the pipeline middleware, rate limiters, auth policies, database connection, compression, and mapped routes.
- **[Endpoints/AuthEndpoints.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API/Endpoints/AuthEndpoints.cs)**
  - Mapped Minimal API group routes (`/api/v1/auth/*`) invoking Mediator command publishers.
- **[Extensions/ResponseResultExtensions.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API/Extensions/ResponseResultExtensions.cs)**
  - Maps `ResponseResult` status values to ASP.NET Core Minimal `IResult` JSON responses with appropriate HTTP status codes.
- **[Middleware/ExceptionHandlingMiddleware.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API/Middleware/ExceptionHandlingMiddleware.cs)**
  - Captures unhandled runtime errors globally, outputting a sanitized 500 error standard payload.

---

## 5. Test Suite (`tests/`)

- **[HrDemo.Application.UnitTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.Application.UnitTests)**
  - Tests command handlers, input validations, and early auth gate behavior pipeline components.
- **[HrDemo.Infrastructure.IntegrationTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.Infrastructure.IntegrationTests)**
  - Verifies database persistency mapping, post-commit interceptor domain event dispatch loops, and startup database permission seeding logic.
- **[HrDemo.API.FunctionalTests](file:///d:/_MyFolder/MyWorkSpace/HRDemo/tests/HrDemo.API.FunctionalTests)**
  - Verifies liveness check routes and authorization access permissions over HTTP.

---

## 6. Key Types Registry

This registry tracks the exact C# locations of central types, service registrations, and security configurations:

| Identifier / Concern | Target File Path | Description / Fields |
|---|---|---|
| **`AddApplicationServices`** | [DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/DependencyInjection.cs#L10) | Registers FluentValidation validators and sequential pipeline behaviors (Logging ➔ Authorization ➔ Validation). |
| **`AddInfrastructureServices`** | [DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/DependencyInjection.cs#L22) | Configures SQL Server context, Identity framework stores, Clocks, JWT authentication, and the database interceptor. |
| **`BaseAuditableEntity`** | [BaseAuditableEntity.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Domain/Common/BaseAuditableEntity.cs#L3) | Base auditable class exposing: `CreatedAt` (`DateTimeOffset`), `CreatedBy` (`string?`), `LastModifiedAt` (`DateTimeOffset?`), and `LastModifiedBy` (`string?`). |
| **Permission Seeding** | [PermissionSeeder.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/Identity/PermissionSeeder.cs) | Implements permission and role database seeding on application host startup, creating roles ("Admin", "User"), baseline permissions, and the default admin user. |
| **`ToHttpResult()`** | [ResponseResultExtensions.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.API/Extensions/ResponseResultExtensions.cs#L8) | Extension method mapping `ResponseResult` status values to ASP.NET Core Minimal `IResult` JSON responses with status codes. |

