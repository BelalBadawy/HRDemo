# 3. Permission and Role Database Seeding PRD

## Problem Statement

The application utilizes granular permission claims-based policies (e.g. `roles.assign`, `claims.assign`) to secure Minimal API endpoints. However, there is no database initialization or seeding mechanism to populate the database with baseline roles, permission claims, and a default admin user. Without this mechanism, developers and automated integration/functional test pipelines cannot authenticate or verify the authorization gates.

## Solution

Create a robust database initialization and seeding service that executes automatically during application host startup. The seeder will:
1. Seed standard roles (`Admin` and `User`).
2. Seed baseline permission claims covering CRUD operations for roles and claims.
3. Seed a default admin user reading development credentials from configuration.
4. Safely handle concurrent startups (such as parallel test runs) without database constraint collision failures.
5. Abort host startup immediately if seeding fails to prevent running in an insecure, unseeded state.

## User Stories

1. As a developer, I want the system to automatically seed default roles (`Admin` and `User`) on application startup, so that the environment is ready for testing immediately.
2. As a security architect, I want a fixed baseline list of permission claims (such as `roles.create`, `roles.view`, `roles.update`, `roles.delete`, `roles.list`, `roles.assign`, and `claims.assign`) to be seeded and assigned to the default Admin user, so that claims-based authorization behavior works out of the box.
3. As a developer, I want the default Admin user credentials to be read from a development configuration file (`appsettings.Development.json`) rather than being hardcoded, so that security credentials are not leaked in source control.
4. As an operations engineer, I want the seeding logic to be concurrency-safe, so that scaling up multiple web host instances or running parallel test fixtures does not cause database unique constraint collisions.
5. As a security officer, I want database seeding failures (e.g. database connection failures or schema mismatches) to immediately crash the host process on startup, so that the application never runs in an insecure, unseeded state.
6. As a tester, I want the seeding process to be completely idempotent, so that subsequent application restarts or runs do not duplicate roles, users, or claim records in the database.

## Implementation Decisions

- **Infrastructure Integration**: Seeding logic is encapsulated within the Infrastructure layer's Identity module, keeping it separate from Domain and Application logic.
- **Startup Wiring**: The seeder is registered in dependency injection and resolved within a service scope created at host startup in the API presentation layer, ensuring it runs before the web application starts accepting HTTP traffic.
- **Database Concurrency Control**: When executing role, user, or claim creations, the seeder wraps operations in try-catch blocks to catch database unique index violations and checks for Identity validation results. If a resource already exists (due to a concurrent execution thread), the seeder safely continues rather than throwing an exception.
- **Configuration Security**: Injected configuration controls default admin credentials. If settings are missing or incomplete, a warning is logged, and the admin user seeding is skipped while roles are still initialized.
- **Crash-on-Failure Policy**: Unhandled exceptions during seeding are logged as critical, and the host process is allowed to crash immediately to prevent starting up in an insecure state.

## Testing Decisions

- **Good Test Criteria**: Test the external behavior of the seeder against a real test database, checking that roles, users, and claims exist after seeding and that subsequent seeding passes do not result in duplicates. Do not mock `UserManager` or `RoleManager` dependencies.
- **Modules Tested**: Integration tests for the permission seeder.
- **Prior Art**: Infrastructure database persistence integration tests utilizing localdb connections.

## Out of Scope

- Seeding business domain data (e.g. employee records, department details) which will be handled in future feature slices.
- UI administration panels for configuring seeded permissions.
- Production environment credentials fallback inside the local configuration files.
