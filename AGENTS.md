1. Project Overview & Architecture
ASP.NET Core 10 solution using Clean Architecture. Four layers, one strict direction of dependency:

HrDemo.Domain          <-- no dependencies
HrDemo.Application      <-- depends on Domain only
HrDemo.Infrastructure    <-- depends on Application (+ Domain transitively)
HrDemo.API                <-- depends on Application + Infrastructure

Architecture & Layer Rules:
- HrDemo.API must never reference HrDemo.Domain directly.
- HrDemo.API and HrDemo.Application must never reference EF Core or ASP.NET Core Identity types directly. They must go through Application interfaces.
- EF Core, Identity, and database implementations live only in HrDemo.Infrastructure.
- Every endpoint response must go through ResponseResult<T>. Do not use ProblemDetails.

2. Tech Stack (Fixed)
Do not substitute or "improve" these without being explicitly asked. If a task requires deviating from this table, stop and ask.

| Concern | Choice | Explicitly NOT this |
|---|---|---|
| Primary keys | int (identity column) on every entity | Guid |
| Mediator/CQRS | Mediator.SourceGenerator + Mediator.Abstractions | MediatR |
| Object mapping | Manual mapping via ToDto() / ToEntity() extension methods | AutoMapper, Mapster |
| Database | SQL Server (Microsoft.EntityFrameworkCore.SqlServer) | PostgreSQL, SQLite |
| Auth | ASP.NET Core Identity with int keys + JWT bearer | Cookie auth, Guid-keyed Identity |
| Authorization | Permission claims (e.g. permission:products.create). Every .RequireAuthorization(...) references a permission policy. | Role-based [Authorize(Roles="Admin")] checks |
| API style | Minimal APIs only, one static class per feature under Endpoints/, registered via Map<Feature>Endpoints | Controllers, [ApiController], MVC |
| Response shape | ResponseResult<T> (success/data/message/errors/statusCode) | ProblemDetails |
| Solution file | .slnx | .sln |
| Validation | FluentValidation, run via a Mediator pipeline behavior | Data annotations |
| Test framework | xUnit + FluentAssertions | NUnit, MSTest |
| Mocking | Check tests/ for the installed package before adding a new one | — |
| NuGet | Central Package Management (Directory.Packages.props) | Inline <PackageVersion> in .csproj |

3. Folder Structure
HrDemo.slnx
Directory.Build.props
Directory.Packages.props
src/
  HrDemo.Domain/
    Entities/ Enums/ ValueObjects/ Exceptions/ Events/ Interfaces/
    Common/            <- BaseEntity, BaseAuditableEntity
  HrDemo.Application/
    Common/
      Interfaces/       <- IApplicationDbContext, ICurrentUserService, etc.
      Behaviors/        <- Mediator pipeline behaviors
      Models/           <- ResponseResult<T>, PaginatedList<T>
      Mapping/
    Features/
      <FeatureName>/
        Commands/ Queries/ Dtos/
  HrDemo.Infrastructure/
    Persistence/
      Configurations/   <- IEntityTypeConfiguration<T> per entity
      Migrations/ Interceptors/
    Identity/           <- ApplicationUser, ApplicationRole, auth handlers
    Services/           <- DateTimeProvider, JwtTokenGenerator, etc.
  HrDemo.API/
    Endpoints/          <- one static class per feature (e.g. ProductEndpoints.cs)
    Program.cs
tests/
  HrDemo.Domain.UnitTests/
  HrDemo.Application.UnitTests/
  HrDemo.Infrastructure.IntegrationTests/
  HrDemo.API.FunctionalTests/

New features go in Application/Features/<FeatureName>/ and a matching src/HrDemo.API/Endpoints/<FeatureName>Endpoints.cs. Don't invent a different layout per feature.

4. Conventions for Adding a Feature (CQRS Slice)
- Domain: Add the entity (int Id, inherits BaseAuditableEntity if audit fields needed), plus value objects/enums.
- Infrastructure: Add IEntityTypeConfiguration<X> in Persistence/Configurations/, add DbSet<X> to IApplicationDbContext and ApplicationDbContext, generate an EF migration.
- Application: Under Features/X/, add Commands/Queries as records implementing Mediator request interfaces, handlers, FluentValidation validators, and manual mapping extensions (X.ToDto()).
- API: Add/extend Endpoints/XEndpoints.cs with a MapXEndpoints extension method mapped under a route group. Handlers send commands/queries via the mediator and return ResponseResult<T> via a shared ToHttpResult() extension. Apply .RequireAuthorization("x.permission") per endpoint.
- Tests: Add unit tests for handler/validator in HrDemo.Application.UnitTests, and functional tests in HrDemo.API.FunctionalTests where relevant.
- DI Wiring: Always wire new DI registrations through the existing AddApplicationServices / AddInfrastructureServices extension methods. Do not add ad-hoc services.AddX() calls in Program.cs.

5. Authorization Rules
- Roles are for coarse grouping only.
- Permissions are claims (e.g., permission:products.create).
- Every .RequireAuthorization(...) call references a permission claim policy, never a role name directly.
- New permission claims must be added to the fixed permission seeding list (check Infrastructure/Identity for the seeding mechanism) rather than invented ad hoc per endpoint.

6. Agent Commands
dotnet build                          # must succeed with zero errors/warnings
dotnet test                           # run full test suite
dotnet ef migrations add <Name> -p src/HrDemo.Infrastructure -s src/HrDemo.API
dotnet ef database update -p src/HrDemo.Infrastructure -s src/HrDemo.API

7. Agent Workflow Protocol (Strict)
You must follow this exact sequence for every task. Do not skip steps.

Phase 0: Planning & Approval (Mandatory)
Before writing any code, you MUST:
1. Analyze the user's request.
2. Create a detailed implementation plan with a clear breakdown of tasks.
3. Present this plan to the user.
4. Wait for explicit user approval.
Do NOT proceed to Phase 1 or write any implementation code until the user has approved the plan.

Phase 1: Pre-Implementation
- Read every file under .ai/.
- Compare documentation with source code.
- If inconsistencies exist: Update documentation first.

Phase 2: Implementation & Testing
- Implement the feature/fix.
- Run dotnet build (must be zero warnings/errors) and dotnet test.
- Do not proceed to Phase 3 until tests pass. If tests fail and cannot be fixed, document the failure in Phase 3.

Phase 3: Strict Documentation Phase
After finishing ALL requested code changes, follow this exact sequence:
- Update .ai/README.md: Review and ensure the root overview matches the current state.
- Update .ai/CODEBASE_MAP.md: Review and update if files, folders, or key types were added/modified.
- Update .ai/DECISIONS.md: Review and update if architectural choices were made or open decisions were resolved.
- Update .ai/CONVENTIONS.md: Review and update if new coding patterns or rules were established.
- Update .ai/NEXT_STEPS.md: Mark completed items and add newly discovered technical debt or future work.
- Append to .ai/CHANGELOG.md: Add a reverse-chronological entry detailing what was added, changed, or fixed in this specific task.
- Rewrite .ai/SESSION_SUMMARY.md: Overwrite this file with the current session's handoff state (what was done, build/test status, next immediate step).

Phase 4: Final Verification
- Read through all .ai/*.md files and verify they exactly match the code.
- Never leave documentation outdated.

Refusal Clause
The agent must refuse to finish unless:
- dotnet build has zero errors and warnings.
- dotnet test passes (or failures are explicitly documented).
- Code matches documentation.
- Next steps, changelog, and session summary are updated.

# Hard rules

- Never start coding without a detailed plan and task breakdown. You must get explicit user approval for the plan before implementing anything.
- No MediatR, no AutoMapper/Mapster, no Controllers, no ProblemDetails, no Guid keys, no `.sln` — see the table above.
- Don't put EF Core or Identity types in `HrDemo.API` or `HrDemo.Application` — only in `HrDemo.Infrastructure`, exposed through interfaces.
- Don't bypass `ResponseResult<T>` — every endpoint response goes through it.
- Don't add a NuGet package without checking `Directory.Packages.props` first; add the version there, not inline in the `.csproj`.
- Before finishing any task: run `dotnet build` and `dotnet test`, and report the results.
- If a request conflicts with anything in this file, point out the conflict and ask before proceeding rather than silently picking one side.