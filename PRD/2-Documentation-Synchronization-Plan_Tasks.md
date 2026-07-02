# Tasks: Documentation Synchronization

This document breaks down the documentation synchronization pass into independent verification and writing tasks.

---

## Issue 1: Discover and Document Core DI & Registrations

### What to build
Discover the registration points for the application and infrastructure service layers in the C# codebase and index them in the codebase map.

### Acceptance criteria
- [x] Identify `AddApplicationServices` inside [DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Application/DependencyInjection.cs).
- [x] Identify `AddInfrastructureServices` inside [DependencyInjection.cs](file:///d:/_MyFolder/MyWorkSpace/HRDemo/src/HrDemo.Infrastructure/DependencyInjection.cs).
- [x] Add mappings and exact references for these methods inside the Key Types Registry of [CODEBASE_MAP.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CODEBASE_MAP.md).

### Blocked by
None - can start immediately

---

## Issue 2: Discover and Document Domain Common Entities & Fields

### What to build
Discover base entity classes and audit tracking fields in the domain layer, and update the architectural specifications.

### Acceptance criteria
- [x] Verify `BaseEntity` utilizes `int` primary keys rather than Guids.
- [x] Verify fields of `BaseAuditableEntity` (`CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`).
- [x] Map these base entities and audit fields in [CODEBASE_MAP.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CODEBASE_MAP.md) and [README.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/README.md).

### Blocked by
None - can start immediately

---

## Issue 3: Discover and Document Authorization & Mapping Flow

### What to build
Verify the permission claims authorization policy configurations and Mediator CQRS request mapping conventions.

### Acceptance criteria
- [x] Confirm database permission seeding is currently unimplemented, and document this state as an open task in [NEXT_STEPS.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/NEXT_STEPS.md).
- [x] Document the adopted mocking library (`NSubstitute` v5.3.0) and compile-time Mediator interfaces (`IRequest<T>`) as decisions in [DECISIONS.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/DECISIONS.md).
- [x] Define static manual mapping extension methods (`ToDto()` / `ToEntity()`) as mandatory conventions in [CONVENTIONS.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CONVENTIONS.md), noting the current Auth slice as a temporary exception.

### Blocked by
None - can start immediately

---

## Issue 4: Create Changelog & Session Summary

### What to build
Initialize project documentation changelog and session state files tracking the completed documentation pass.

### Acceptance criteria
- [x] Create [CHANGELOG.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/CHANGELOG.md) outlining changes made to project documentation.
- [x] Create [SESSION_SUMMARY.md](file:///d:/_MyFolder/MyWorkSpace/HRDemo/.ai/SESSION_SUMMARY.md) summarizing the handoff state.
- [x] Ensure all documentation contains zero unresolved placeholders.

### Blocked by
- Issue 1: Discover and Document Core DI & Registrations
- Issue 2: Discover and Document Domain Common Entities & Fields
- Issue 3: Discover and Document Authorization & Mapping Flow
