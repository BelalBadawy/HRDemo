# Next Steps

This document outlines the planned roadmap and outstanding implementation tasks for the **HrDemo** solution.

---

## 1. Domain Features Implementation (Core HR Slices)

The current codebase is a scaffold skeleton containing identity, authentication, and session control. The core HR domain features need to be designed and implemented:

- **Employee Management Slice**:
  - Implement the `Employee` aggregate root (inheriting `BaseAuditableEntity`) with properties like Name, Email, Department, JobTitle, HireDate, and Status.
  - Implement commands to create, update, and terminate employees.
  - Expose API endpoints under `/api/v1/employees`.
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

- **Database Seeding**:
  - Implement a database initialization and seeding service (e.g. `src/HrDemo.Infrastructure/Identity/PermissionSeeder.cs`) to execute automatically on host startup.
  - Define a fixed list of baseline permission claims (such as `roles.assign` and `claims.assign`) and seed them to specific roles/users on startup, satisfying the requirement in `AGENTS.md` Rule 5.

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
