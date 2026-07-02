# Tasks: Permission and Role Database Seeding

This document breaks down the database seeding implementation into independent tasks.

---

## Issue 1: Implement PermissionSeeder Class

### What to build
Create the `PermissionSeeder` service class inside the Infrastructure Identity layer to populate default roles and permission claims.

### Acceptance criteria
- [x] Create `PermissionSeeder.cs` inside `src/HrDemo.Infrastructure/Identity/`.
- [x] Seed baseline roles: `"Admin"` and `"User"`.
- [x] Define a baseline list of permission claims: `roles.create`, `roles.view`, `roles.update`, `roles.delete`, `roles.list`, `roles.assign`, and `claims.assign`.
- [x] Assign the `"Admin"` role and all baseline claims to a default admin user.
- [x] Implement database try-catch blocks and Identity validation codes to handle parallel concurrent execution safely.
- [x] Register `PermissionSeeder` in `DependencyInjection.cs` of the Infrastructure project.

### Blocked by
None - can start immediately

---

## Issue 2: Configure Application Settings & Startup Execution Hook

### What to build
Wire the `PermissionSeeder` execution hook in the application startup pipeline, drawing credentials from configuration settings.

### Acceptance criteria
- [x] Configure a `DefaultAdmin` settings block containing `UserName`, `Email`, and `Password` in `appsettings.Development.json`.
- [x] Instantiate a service scope at host startup in `src/HrDemo.API/Program.cs` and resolve the seeder.
- [x] Call `SeedAsync` synchronously and handle exceptions by critical logging and crash-on-failure execution.
- [x] Change `app.Run()` to `await app.RunAsync()` to resolve CA1849 compiler warning.

### Blocked by
- Issue 1: Implement PermissionSeeder Class

---

## Issue 3: Add Database Seeding Integration Tests

### What to build
Implement integration tests simulating database seeding behavior to check record generation and idempotency.

### Acceptance criteria
- [x] Create `PermissionSeederTests.cs` in `tests/HrDemo.Infrastructure.IntegrationTests/`.
- [x] Register `AddDataProtection()` in the service collection within test fixtures.
- [x] Assert that `"Admin"` and `"User"` roles are successfully created.
- [x] Assert that the default admin user exists, has the `"Admin"` role, and possesses all 7 standard permission claims.
- [x] Assert that calling the seeder subsequent times does not duplicate database entries (idempotency test).

### Blocked by
- Issue 1: Implement PermissionSeeder Class
- Issue 2: Configure Application Settings & Startup Execution Hook

---

## Issue 4: Sync System Documentation

### What to build
Sync codebase map indexes, decisions, roadmap next steps, and changelog records to reflect the completed seeding pass.

### Acceptance criteria
- [x] Document the seeder in `CODEBASE_MAP.md` and Key Types Registry.
- [x] Mark permission seeding decision as fully implemented in `DECISIONS.md`.
- [x] Set database seeding to completed and update active backlog priority in `NEXT_STEPS.md`.
- [x] Append changelog entry `[1.1.0]` in `CHANGELOG.md`.
- [x] Rewrite `SESSION_SUMMARY.md` tracking completed tasks and test results.

### Blocked by
- Issue 1: Implement PermissionSeeder Class
- Issue 2: Configure Application Settings & Startup Execution Hook
- Issue 3: Add Database Seeding Integration Tests
