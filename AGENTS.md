# AGENTS.md
## Project overview

ASP.NET Core 10 solution using Clean Architecture. Four layers, one direction of dependency:

```
HrDemo.Domain          <-- no dependencies
HrDemo.Application      <-- depends on Domain only
HrDemo.Infrastructure    <-- depends on Application (+ Domain transitively)
HrDemo.API                <-- depends on Application + Infrastructure
```

`HrDemo.API` must never reference `HrDemo.Domain` directly, and must never reference EF Core or Identity types directly in endpoint handlers — go through Application interfaces.

## Tech stack (fixed — do not substitute or "improve" without being asked)

| Concern | Choice | Explicitly NOT this |
|---|---|---|
| Primary keys | `int` (identity column) on every entity | Guid |
| Mediator/CQRS | Mediator source generator (`Mediator.SourceGenerator` + `Mediator.Abstractions`) | MediatR |
| Object mapping | Manual mapping via `ToDto()` / `ToEntity()` extension methods | AutoMapper, Mapster |
| Database | SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) | PostgreSQL, SQLite |
| Auth | ASP.NET Core Identity with `int` keys (`IdentityUser<int>`, `IdentityRole<int>`) + JWT bearer | Cookie auth, Guid-keyed Identity |
| Authorization model | Roles = coarse grouping only. Permissions = claims (e.g. `permission:products.create`). Every `.RequireAuthorization(...)` call references a permission claim policy, never a role name directly. | Role-based `[Authorize(Roles="Admin")]` style checks |
| API style | Minimal APIs only, one static class per feature under `Endpoints/`, registered via `Map<Feature>Endpoints` extension methods | Controllers, `[ApiController]`, MVC |
| Response shape | `ResponseResult<T>` (success/data/message/errors/statusCode) returned from every endpoint | `ProblemDetails` |
| Solution file | `.slnx` | `.sln` |
| Validation | FluentValidation, run via a Mediator pipeline behavior | Data annotations for command/query validation |
| Test framework | xUnit + FluentAssertions | NUnit, MSTest |
| Mocking | Whatever was chosen at scaffold time — check `tests/` for the installed package before adding a new one | — |

If a task seems to require deviating from this table, stop and ask rather than silently switching libraries.

## Folder structure

```
HrDemo.slnx
Directory.Build.props
Directory.Packages.props
src/
  HrDemo.Domain/
    Entities/
    Enums/
    ValueObjects/
    Exceptions/
    Events/
    Interfaces/
    Common/            <- BaseEntity, BaseAuditableEntity
  HrDemo.Application/
    Common/
      Interfaces/       <- IApplicationDbContext, ICurrentUserService, etc.
      Behaviors/        <- Mediator pipeline behaviors
      Models/           <- ResponseResult<T>, PaginatedList<T>
      Mapping/
    Features/
      <FeatureName>/
        Commands/
        Queries/
        Dtos/
  HrDemo.Infrastructure/
    Persistence/
      Configurations/   <- IEntityTypeConfiguration<T> per entity
      Migrations/
      Interceptors/
    Identity/           <- ApplicationUser, ApplicationRole, auth handlers
    Services/           <- DateTimeProvider, JwtTokenGenerator, etc.
  HrDemo.API/
    Endpoints/          <- one static class per feature
    Program.cs
tests/
  HrDemo.Domain.UnitTests/
  HrDemo.Application.UnitTests/
  HrDemo.Infrastructure.IntegrationTests/
  HrDemo.API.FunctionalTests/
```

New features go in `Application/Features/<FeatureName>/` and a matching `Web/Endpoints/<FeatureName>Endpoints.cs`. Don't invent a different layout per feature — match the existing ones.

## Conventions when adding a feature (CQRS slice)

For a typical "create/read/update/delete X" feature:

1. **Domain**: add the entity (int Id, inherits `BaseAuditableEntity` if it needs audit fields), plus any value objects/enums it needs.
2. **Infrastructure**: add an `IEntityTypeConfiguration<X>` in `Persistence/Configurations/`, add a `DbSet<X>` to `IApplicationDbContext` and `ApplicationDbContext`, generate an EF Core migration.
3. **Application**: under `Features/X/`, add Commands/Queries as records implementing the Mediator request interfaces, their handlers, FluentValidation validators, and a manual mapping extension (`X.ToDto()`).
4. **Web**: add or extend `Endpoints/XEndpoints.cs` with a `MapXEndpoints` extension method, mapped under a route group, each handler sending the command/query via the mediator and returning `ResponseResult<T>` via the shared `ToHttpResult()` extension. Apply `.RequireAuthorization("x.permission")` per endpoint as appropriate.
5. **Tests**: add a unit test for the handler/validator in `HrDemo.Application.UnitTests`, and where relevant a functional test in `HrDemo.API.FunctionalTests`.

Always wire new DI registrations through the existing `AddApplicationServices` / `AddInfrastructureServices` extension methods — don't add ad-hoc `services.AddX()` calls directly in `Program.cs`.

## Authorization specifics

- New permission claims are added to the fixed permission list (wherever that's seeded — check `Infrastructure/Identity` for the seeding mechanism before assuming one doesn't exist) rather than invented ad hoc per endpoint.
- Don't gate an endpoint on a role name. Define/use a permission claim and a matching authorization policy.

## Commands the agent should know

```bash
dotnet build                          # must succeed with zero errors/warnings before any change is considered done
dotnet test                           # run full test suite
dotnet ef migrations add <Name> -p src/HrDemo.Infrastructure -s src/HrDemo.API
dotnet ef database update -p src/HrDemo.Infrastructure -s src/HrDemo.API
```

## Hard rules

- No MediatR, no AutoMapper/Mapster, no Controllers, no ProblemDetails, no Guid keys, no `.sln` — see the table above.
- Don't put EF Core or Identity types in `HrDemo.API` or `HrDemo.Application` — only in `HrDemo.Infrastructure`, exposed through interfaces.
- Don't bypass `ResponseResult<T>` — every endpoint response goes through it.
- Don't add a NuGet package without checking `Directory.Packages.props` first; add the version there, not inline in the `.csproj`.
- Before finishing any task: run `dotnet build` and `dotnet test`, and report the results.
- If a request conflicts with anything in this file, point out the conflict and ask before proceeding rather than silently picking one side.