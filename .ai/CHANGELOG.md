# Changelog

All notable changes to the **HrDemo** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-02

### Added
- Documentation synchronization pass. Initialized/updated all `.ai` documentation files to match existing codebase state perfectly:
  - Updated `README.md` with verified tech stack details (specifically centering on .NET 10 / ASP.NET Core 10).
  - Updated `CODEBASE_MAP.md` mapping file directories and adding the **Key Types Registry** section (`AddApplicationServices`, `AddInfrastructureServices`, `BaseAuditableEntity`, etc.).
  - Updated `DECISIONS.md` documenting decisions for `NSubstitute` mocking, compile-time `Mediator` interfaces, and planned permission seeding.
  - Updated `CONVENTIONS.md` mapping standard `IRequest<TResponse>` CQRS slices and setting mandatory manual mapping standards (using static `ToDto()` and `ToEntity()` methods) for future Domain aggregate entities.
  - Updated `NEXT_STEPS.md` referencing database seeding implementation tasks.
