# HrDemo Solution

An ASP.NET Core 10 Clean Architecture solution skeleton for the `HrDemo` project, establishing standard architectural patterns, secure identity management, and post-commit domain event handling.

## Architecture Overview & Dependency Graph

This project follows Clean Architecture principles, ensuring clear separation of concerns and unidirectional dependencies pointing inward:

```
[ HrDemo.Domain ]
      ▲
      │ (references Domain only)
[ HrDemo.Application ]
      ▲              ▲
      │              │ (references Application & Domain)
[ HrDemo.Infrastructure ]
      ▲              ▲
      │              │ (references Application & Infrastructure)
[   HrDemo.API   ]
```

- **Domain**: Contains pure domain entities, value objects, domain events, and interfaces. It has zero external package dependencies.
- **Application**: Defines application use cases (CQRS commands/queries), FluentValidation rules, pipeline behaviors (Logging, Validation, Authorization), and interfaces.
- **Infrastructure**: Implements database persistence (EF Core SQL Server, ApplicationDbContext), Identity membership management, JWT token generation, clocks, and post-save domain event dispatching.
- **API (HrDemo.API)**: Exposes endpoints mapped via Minimal APIs, handles global exception middleware, registers health checks, rate limiting, and response compression.

---

## Central Package Management (CPM)

Dependencies across all projects are managed centrally using `Directory.Packages.props` at the root. Per-project `.csproj` files declare `<PackageReference Include="PackageName" />` without specifying versions. 

> [!NOTE]
> Since build metadata like `PrivateAssets` and `IncludeAssets` in `Directory.Packages.props` is ignored by NuGet during central package mapping, these must be declared inline on the `<PackageReference>` elements within the consuming projects.

---

## Database Migrations

Entity Framework Core migrations live in the `HrDemo.Infrastructure` project, with `ApplicationDbContext` serving as the main database context. A design-time factory (`IDesignTimeDbContextFactory<ApplicationDbContext>`) is implemented in the Persistence layer to allow executing CLI migrations without running the API host.

### CLI Tooling Commands

Generate a new migration:
```bash
dotnet ef migrations add <Name> --project src/HrDemo.Infrastructure --startup-project src/HrDemo.API
```

Apply migrations to the database:
```bash
dotnet ef database update --project src/HrDemo.Infrastructure --startup-project src/HrDemo.API
```

---

## Hashed Rotating Refresh Tokens & Single Refresh Token Policy

To maximize security:
- Raw refresh tokens are never persisted or logged. Only a SHA256 cryptographic hash of the refresh token is stored in the database (`TokenHash`).
- **Single Session Policy**: A user owns exactly one refresh token row at any point in time. The `RefreshToken` table uses `UserId` as both the primary key and foreign key.
- **Login Flow**: Upserts the user's refresh token record with a newly rotated token. Logging in from a second device overwrites the existing row, immediately invalidating the previous session.
- **Refresh Flow**: Rotates both the access token and refresh token, atomically updating the database record. Concurrency races (e.g. from simultaneous client retries) throw a `DbUpdateConcurrencyException`, which is caught and mapped to a 409 Conflict/401 Unauthorized result.
- **Logout Flow**: Deletes the user's refresh token record from the database.

---

## Domain Event Life-Cycle & Post-Commit Dispatch

Domain events are defined as pure `IDomainEvent` marker interfaces in the Domain layer. They are collected within aggregates and dispatched sequentially strictly **AFTER** a successful database commit in `SaveChangesAsync`.

* **Trade-off**: Dispatching events post-commit guarantees that database operations succeed before event handlers are executed. However, if an event handler fails, eventual consistency must be resolved out-of-band or via an Outbox Pattern (which is scheduled on the future roadmap).

---

## Permission-Based Authorization

The application uses granular claims-based permission authorization policies (e.g. `permission:users.create`) rather than coarse role-based constraints. 
- API endpoints map permission requirements.
- The `AuthorizationBehavior` pipeline behavior evaluates requests implementing `IAuthorizeRequest` before executing input validators, preventing unauthorized requests from accessing database persistence layers.

---

## Health Checks Semantics

- **Liveness** (`/health/live`): Fast status check to verify the host service is running. It does not perform database queries.
- **Readiness** (`/health/ready`): Validates database health using built-in EF Core DbContext health checks.

---

## Adding a New CQRS Feature

Follow this sequential recipe to introduce a new CQRS vertical slice:
1. **Domain**: Define domain entities, aggregates, or events.
2. **Infrastructure**: Add `IEntityTypeConfiguration<T>` mapping configurations in `Persistence/Configurations/` and register the `DbSet<T>` in `ApplicationDbContext`. Add an EF Core migration.
3. **Application**: Add a feature folder under `Features/` (e.g. `Features/Products/Commands/CreateProduct/`). Define the Command/Query record, its Handler, FluentValidation rules, and mapping extension methods.
4. **API**: Map the endpoint in the respective static feature class (e.g., `Endpoints/ProductEndpoints.cs`) and register version-controlled routes.
5. **Tests**: Implement unit tests for the Handler/Validator, and functional tests verifying the API endpoint responses.

---

## Equivalent CI Pipeline Commands

Verify the codebase in a CI environment using:
```bash
# 1. Restore central package versions
dotnet restore

# 2. Build solution under WarningsAsErrors constraints
dotnet build --configuration Release

# 3. Execute all unit and functional tests
dotnet test --configuration Release --no-build

# 4. Check style formatting
dotnet format --verify-no-changes
```