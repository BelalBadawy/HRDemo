# Next Steps

This document outlines the active backlog and planned roadmap for the **HrDemo** solution.

---

## 0. Immediate Backlog (Active Priority)

- [ ] **Employee Management Slice**:
  - Implement the `Employee` aggregate root in `HrDemo.Domain` (inheriting `BaseAuditableEntity`) with properties like Name, Email, Department, JobTitle, HireDate, and Status.
  - Configure EF mappings in Infrastructure persistence layer and generate an EF migration.
  - Implement CQRS commands/queries under `src/HrDemo.Application/Features/Employees/` (Create, Update, Terminate, GetById, List).
  - Expose API endpoints under `/api/v1/employees` in `HrDemo.API/Endpoints/EmployeeEndpoints.cs` and apply `.RequireAuthorization(...)` permissions.
  - Write unit and functional tests for the slice.

---

## 1. Domain Features Implementation (Core HR Slices)

- **Department Management Slice**:
  - Implement the `Department` entity.
  - Define associations between Employees and Departments.
  - Create endpoints to map and manage departments.
- **Leave Request Management Slice**:
  - Implement a `LeaveRequest` state machine (Submitted, Approved, Rejected, Cancelled).
  - Raise domain events when requests are submitted or updated (e.g. `LeaveRequestSubmittedEvent` to notify managers).

---

## 2. Infrastructure & Reliability Slices

- **Outbox Pattern for Domain Events**:
  - Replace the immediate post-commit mediator dispatching in `AuditableAndDomainEventsInterceptor` with an Outbox Pattern to ensure reliable eventual consistency.
  - Create an `OutboxMessage` table.
  - Write events to the outbox table in the same transaction as state updates in `SavingChangesAsync`.
  - Add a background worker (e.g., using ASP.NET Core `BackgroundService` or Quartz.NET) to poll, deserialize, and publish messages, ensuring at-least-once delivery.
- **Dapper Integration for CQRS Read Path (Queries)**:
  - Add Dapper support to the Infrastructure layer to implement high-performance, read-only queries.
  - Implement Dapper query handlers that bypass EF Core tracking to optimize search and grid list performance.
- **Configure Rate Limiting over Auth Routes**:
  - Apply the configured `"AuthPolicy"` rate limiter to the authentication minimal API group in `AuthEndpoints.cs` by calling `.RequireRateLimiting("AuthPolicy")` on the mapped group.

---

## 3. Operations & Environment Configs

- **Database Seeding** (Completed):
  - Created a concurrency-safe database initializer `PermissionSeeder` in `src/HrDemo.Infrastructure/Identity/PermissionSeeder.cs`.
  - Configured startup wiring in `Program.cs` to seed roles, admin user, and permission claims on startup.
  - Added comprehensive integration tests to prevent regression and verify idempotency.

- **Dockerization**:
  - Add a multi-stage `Dockerfile` to compile and package the API project.
  - Create a `docker-compose.yml` file configuring the SQL Server database and API container for local execution.
- **Structured Logging**:
  - Configure structured logging (e.g. Serilog) in the API project to log JSON-formatted payloads, allowing trace IDs and correlating requests across boundaries.

---

## 4. Test Coverage Expansion

- **Domain Unit Testing**:
  - Implement tests in `HrDemo.Domain.UnitTests` verifying business rules, entity validations, and state machine transitions (e.g., validating leave balances).
- **Outbox Worker Integration Tests**:
  - Add tests under `HrDemo.Infrastructure.IntegrationTests` verifying outbox message generation, execution, and locking.
- **Edge-case Functional Tests**:
  - Add functional tests verifying early request rejection when validation fails or authentication tokens are missing.
