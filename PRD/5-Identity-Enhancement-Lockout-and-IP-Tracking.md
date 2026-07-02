# 5. Identity Enhancement: Lockout, Active Status, and IP Tracking PRD

## Problem Statement

The application's identity system has several critical gaps:
1. **User Auditing & Administration**: The `ApplicationUser` entity lacks basic tracking properties such as `CreatedDate` and `IsActive` status, making user auditing and administrative account deactivation impossible.
2. **Brute Force Protection**: The login flow does not enforce account lockout policies on repeated failed attempts, leaving the system vulnerable to automated password guessing attacks.
3. **Leaked Transport Concerns**: Client IP addresses are passed through CQRS commands (`LoginCommand`), leaking transport-layer concerns into the Application layer.
4. **Token Security for Deactivated Users**: Active refresh tokens are not checked against the user's active status, meaning deactivated users can continue to refresh and access the system using existing sessions.

## Solution

1. **Extend User Schema**: Add `CreatedDate` (using `IClock`) and `IsActive` (defaulting to `true`) columns to `ApplicationUser`. Implement an EF Core migration with explicit default values to prevent failures in existing environments. Update database seeding to set these explicitly.
2. **Configure Lockout Policies**: Configure ASP.NET Core Identity lockout options (5 max failed attempts, 15 minutes lockout duration, enabled for new users) and use `SignInManager<ApplicationUser>.CheckPasswordSignInAsync` in the login pipeline.
3. **Enforce State Validation at Login**: Implement structured checks during login to enforce active status and account lockouts, returning distinct, descriptive error messages so clients know if their credentials are invalid, their account is inactive, or their account is locked out.
4. **Isolate IP Address Retrieval**: Remove `IpAddress` from the application-layer `LoginCommand` payload. Instead, extract the IP address at the Infrastructure boundary via a new `IpAddress` property on `ICurrentUser` (using `IHttpContextAccessor`).
5. **Enforce Active Check on Refresh**: Validate the user's `IsActive` status during refresh token rotation. If the user is inactive, reject the request with a `401 Unauthorized` response and delete the stored refresh token to revoke the session immediately.

## User Stories

1. As a security auditor, I want each user account to record its creation date, so that I can audit user lifecycle events.
2. As an administrator, I want to toggle a user's active status (`IsActive = false`), so that I can immediately suspend their access without deleting their record.
3. As a developer, I want to use `CheckPasswordSignInAsync` with lockout support, so that repeated login failures are tracked by ASP.NET Core Identity.
4. As an API client, I want to receive a distinct "Account is inactive." message when attempting to log in with an inactive account, so that I understand why access is denied.
5. As an API client, I want to receive a distinct "Account is locked." message when attempting to log in with a locked-out account, so that I know why access is temporarily blocked.
6. As a locked-out user, I want my account to unlock automatically after 15 minutes, so that I can log in successfully once my lockout window has elapsed.
7. As a developer, I want client IP addresses to be captured directly from `ICurrentUser` in the Infrastructure layer instead of being passed as parameters in Application commands, so that transport details are kept at the boundary.
8. As a security architect, I want refresh token requests for deactivated users to be rejected and their refresh token revoked immediately, so that deactivated users cannot continue using active sessions.

## Implementation Decisions

- **Identity Registration**: Update `HrDemo.Infrastructure/DependencyInjection.cs` to register Identity with `AddIdentity<ApplicationUser, ApplicationRole>` instead of `AddIdentityCore` to support resolving `SignInManager`.
- **Identity Lockout Settings**: Set `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`, and `AllowedForNewUsers = true` on IdentityOptions.
- **Auditable Fields**: `ApplicationUser.CreatedDate` will be populated in memory using `IClock.UtcNow` upon instantiation, rather than using a database default constraint. It will include comments explaining that it resides in the Infrastructure layer and doesn't inherit from the Domain `BaseEntity` hierarchy.
- **Login Logical Sequencing (Account Harvesting Mitigation)**:
  1. Find user by `UserNameOrEmail`. If user is null, return `"Invalid username or password."` (Do not call `CheckPasswordSignInAsync` on a null user).
  2. If user is found, call `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.
  3. If result is `IsLockedOut`, return `"Account is locked."`.
  4. If result is `RequiresTwoFactor` (not currently supported, but noted for future design), handle accordingly.
  5. If result is `Succeeded`, check `IsActive`. If `IsActive` is false, return `"Account is inactive."`.
  6. If result is `Failed` (or any other failure state), return `"Invalid username or password."`.
- **IP Address Accessor (Architectural Win)**: Moving `IpAddress` out of `LoginCommand` and into `ICurrentUser` keeps HTTP context concerns isolated from the Application layer. By placing it in `ICurrentUser` (implemented in Infrastructure using `IHttpContextAccessor`), the Application layer can access the client IP if needed for auditing/logging without polluting the command contracts.
- **Active Check on Refresh**: In `RefreshTokenService.RotateTokenAsync`, load the user's active status before rotation. If the user is inactive, immediately delete the `RefreshToken` row from the database and return a `401 Unauthorized` result with "Account is inactive.".

## Testing Decisions

- **Good Test Criteria**: Tests must verify the end-to-end logical outcome of commands and handlers without leaking implementation details. 
- **Modules Tested**: `LoginHandler`, `RefreshTokenHandler`, and `RefreshTokenService`.
- **Prior Art**: `LoginTests.cs` and `RefreshTests.cs` using `NSubstitute` for mocking database and security configurations.
- **Specific Test Cases**:
  - Successful login with active user, correct credentials.
  - Login failure with inactive user (returns `401 Unauthorized` with "Account is inactive.").
  - Lockout trigger (5 failed attempts locks the account, 6th attempt returns `401 Unauthorized` with "Account is locked.").
  - Lockout expiration (using a simulated `IClock` or `TimeProvider` to advance time by 15 minutes, allowing subsequent successful login).
  - Refresh failure with inactive user (returns `401 Unauthorized` with "Account is inactive." and deletes the stored refresh token).
  - Assert that `LoginCommand` and its requests do not contain any `IpAddress` properties.

## Out of Scope

- Admin REST APIs for manually locking/unlocking user accounts.
- Email or SMS notifications upon account lockout.
- Support for multiple active sessions per user (the Single Refresh Token Policy remains in effect).
- UI/Frontend modifications.

## Further Notes

- **Architectural Win (IP isolation)**: Removing IP address fields from command payloads is a significant Clean Architecture improvement. It keeps HTTP/connection details out of the application layer, ensuring the Application remains fully transport-agnostic. The `ICurrentUser` abstraction successfully bridges this boundary by letting the Infrastructure implementation extract these parameters dynamically from `IHttpContextAccessor` as needed.

