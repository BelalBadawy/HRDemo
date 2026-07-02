# Tasks: Identity Enhancement (Lockout, Active Status, and IP Tracking)

This document breaks down the Identity system enhancements and security refactoring into independent tasks.

---

## Issue 1: Update ApplicationUser Schema, EF Migration, and Seeding

### What to build
Extend the `ApplicationUser` entity with `CreatedDate` and `IsActive` properties, add an EF Core migration with safe defaults, and update database seeding to set these properties.

### Acceptance criteria
- [x] Add `DateTimeOffset CreatedDate` to `src/HrDemo.Infrastructure/Identity/ApplicationUser.cs` with code comments explaining why it resides in Infrastructure and doesn't inherit from the Domain `BaseEntity`.
- [x] Add `bool IsActive` (defaulting to `true`) to `ApplicationUser.cs`.
- [x] Generate an EF Core migration `AddUserCreatedDateAndIsActive` using `dotnet ef migrations add`.
- [x] Edit the migration to ensure existing records are populated with `IsActive = true` and `CreatedDate` = current UTC timestamp to avoid validation failures.
- [x] Update `PermissionSeeder.cs` to set `IsActive = true` and `CreatedDate = clock.UtcNow` explicitly when seeding the default admin user.

### Blocked by
None - can start immediately

---

## Issue 2: Register Identity and Configure Lockout Options

### What to build
Update the DI container registration in Infrastructure to use `AddIdentity<ApplicationUser, ApplicationRole>` instead of `AddIdentityCore` to support resolving `SignInManager<ApplicationUser>`, and configure Identity lockout options.

### Acceptance criteria
- [x] Modify `src/HrDemo.Infrastructure/DependencyInjection.cs` to replace `AddIdentityCore<ApplicationUser>` with `AddIdentity<ApplicationUser, ApplicationRole>()`.
- [x] Ensure EF stores, roles, and default token providers are configured correctly on the new registration.
- [x] Configure `Lockout` options:
  - `MaxFailedAccessAttempts = 5`
  - `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`
  - `AllowedForNewUsers = true`

### Blocked by
- Issue 1: Update ApplicationUser Schema, EF Migration, and Seeding

---

## Issue 3: Refactor IP Address Tracking into ICurrentUser

### What to build
Expose the client IP address on `ICurrentUser` at the Infrastructure boundary and remove it from Application CQRS commands.

### Acceptance criteria
- [x] Add `string? IpAddress { get; }` to `src/HrDemo.Application/Abstractions/Identity/ICurrentUser.cs`.
- [x] Implement `IpAddress` in `src/HrDemo.Infrastructure/Identity/CurrentUser.cs` via `_httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()`.
- [x] Remove `IpAddress` from `LoginCommand.cs` and `LoginValidator.cs`.
- [x] Remove `ipAddress` parameter from `IUserManager.LoginAsync` signature.
- [x] In `RefreshTokenService.cs`, fetch the client's IP from `ICurrentUser.IpAddress` rather than passing it as a parameter from Application.

### Blocked by
None - can run in parallel with Issues 1-2

---

## Issue 4: Enhance Login Flow in UserManagerService

### What to build
Update the login flow to use `SignInManager.CheckPasswordSignInAsync` and return distinct, descriptive results for invalid credentials, inactive status, and lockouts.

### Acceptance criteria
- [x] Inject `SignInManager<ApplicationUser>` into `UserManagerService`.
- [x] In `UserManagerService.LoginAsync`, execute the login check sequence in this exact order to prevent account harvesting:
  1. Retrieve the user by UserNameOrEmail. If null, return `"Invalid username or password."` (Do not call `CheckPasswordSignInAsync` on a null user).
  2. Call `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.
  3. If check fails due to lockout (`result.IsLockedOut`), return `"Account is locked."` (HTTP 401).
  4. If check succeeds (`result.Succeeded`), check active status: if `!user.IsActive`, return `"Account is inactive."` (HTTP 401).
  5. If check fails (`result.Succeeded == false`), return `"Invalid username or password."` (HTTP 401).
- [x] Ensure raw IP logging is removed from the Application-layer login flow, and remains handled at the Infrastructure boundary.

### Blocked by
- Issue 2: Register Identity and Configure Lockout Options
- Issue 3: Refactor IP Address Tracking into ICurrentUser

---

## Issue 5: Enforce IsActive Check and Revocation in Refresh Flow

### What to build
Validate that the user is active during token rotation, revoking the session and deleting the refresh token record if the user is inactive.

### Acceptance criteria
- [x] In `RefreshTokenService.RotateTokenAsync` (or wherever refresh rotation is implemented), fetch the user from the database or via `UserManager` and verify `user.IsActive`.
- [x] If the user is inactive, immediately delete the `RefreshToken` row from the database (force log out).
- [x] Return a `ResultStatus.Unauthorized` response indicating the account is inactive.
- [x] Update `RefreshTokenHandler.cs` (if needed) to handle the failed rotation.

### Blocked by
- Issue 1: Update ApplicationUser Schema, EF Migration, and Seeding

---

## Issue 6: Add and Refactor Unit Tests

### What to build
Update existing tests to reflect the removed IP address command parameters and write comprehensive unit tests covering the new lockout, inactive, and refresh-revocation scenarios.

### Acceptance criteria
- [x] Update `LoginTests.cs` to remove `IpAddress` from command instantiation.
- [x] Add unit test: Successful login for active user with valid credentials.
- [x] Add unit test: Inactive user login attempt with valid credentials (assert `401 Unauthorized` / `"Account is inactive."`).
- [x] Add unit test: Lockout logic (5 failed attempts lock the account, 6th attempt returns `401 Unauthorized` / `"Account is locked."`).
- [x] Add unit test: Lockout expiration (advancing a mocked time provider by 15 minutes enables login again).
- [x] Add unit test: Refresh token rotation fails and deletes token record if user is inactive.
- [x] Verify that `LoginCommand` and `LoginRequest` do not contain `IpAddress`.

### Blocked by
- Issue 4: Enhance Login Flow in UserManagerService
- Issue 5: Enforce IsActive Check and Revocation in Refresh Flow

---

## Issue 7: Sync System Documentation

### What to build
Update `.ai/` files to match the new identity, lockout, IP tracking conventions, and task completion.

### Acceptance criteria
- [x] Update `CODEBASE_MAP.md` to reflect new user properties, deleted IP parameters, and updated interfaces.
- [x] Update `CONVENTIONS.md` to document Identity Lockout, `IsActive` logic, and the distinct error message login policy.
- [x] Add changelog entries to `CHANGELOG.md`.
- [x] Overwrite `SESSION_SUMMARY.md` tracking work done and build/test verification results.

### Blocked by
- Issue 6: Add and Refactor Unit Tests
