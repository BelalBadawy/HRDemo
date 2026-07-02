# Tasks: Swagger Integration with Basic Auth and JWT

This document breaks down the Swagger integration and security implementation into independent tasks.

---

## Issue 1: Reference Swashbuckle Package & Configuration

### What to build
Add the package reference to `Swashbuckle.AspNetCore` in the solution, enable XML documentation generation in the API project, and configure static credentials.

### Acceptance criteria
- [x] Add `Swashbuckle.AspNetCore` (version `10.0.0`) to `Directory.Packages.props`.
- [x] Reference `Swashbuckle.AspNetCore` in `src/HrDemo.API/HrDemo.API.csproj`.
- [x] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `<NoWarn>$(NoWarn);1591</NoWarn>` to the property group of `HrDemo.API.csproj`.
- [x] Add `SwaggerAuth` section with static credentials (`HrAdmin` / `HR@20226$`) to `appsettings.Development.json`.

### Blocked by
None - can start immediately

---

## Issue 2: Create Swagger Basic Auth Middleware

### What to build
Create the `SwaggerBasicAuthMiddleware` to parse and authenticate basic auth credentials on all routes matching `/swagger/*`.

### Acceptance criteria
- [x] Create `SwaggerBasicAuthMiddleware.cs` in `src/HrDemo.API/Middleware/`.
- [x] Intercept all requests starting with `/swagger` (both the UI pages and the OpenAPI JSON schema document).
- [x] Extract the `Authorization` header, verify the `Basic` scheme, base64-decode the credentials, and match them against the configured `SwaggerAuth` settings.
- [x] Return `401 Unauthorized` with the `WWW-Authenticate: Basic realm="HrDemo Swagger"` header if authentication fails or is missing.

### Blocked by
- Issue 1: Reference Swashbuckle Package & Configuration

---

## Issue 3: Implement Swagger Extension and Wire in Program.cs

### What to build
Implement extension methods to register Swagger services with JWT Bearer support and invoke the middleware cleanly in `Program.cs`.

### Acceptance criteria
- [x] Create `SwaggerExtensions.cs` in `src/HrDemo.API/Extensions/`.
- [x] Implement `AddSwaggerConfiguration(this IServiceCollection services)`:
  - Register endpoints API explorer.
  - Call `AddSwaggerGen` to configure Swagger document metadata.
  - Include XML comments files.
  - Define `Bearer` JWT security scheme and set it as a global security requirement.
- [x] Implement `UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)`:
  - Only configure Swagger UI and Basic Auth if `env.IsDevelopment()`.
  - Use `SwaggerBasicAuthMiddleware` right before serving Swagger UI.
  - Map Swagger endpoints.
- [x] Wire up `builder.Services.AddSwaggerConfiguration()` and `app.UseSwaggerConfiguration(builder.Environment)` in `src/HrDemo.API/Program.cs`.

### Blocked by
- Issue 2: Create Swagger Basic Auth Middleware

---

## Issue 4: Add Functional Tests for Swagger Auth

### What to build
Create a functional test class using xUnit, FluentAssertions, and `WebApplicationFactory<Program>` to verify access gating.

### Acceptance criteria
- [x] Create `SwaggerAuthTests.cs` in `tests/HrDemo.API.FunctionalTests/`.
- [x] Configure `WebApplicationFactory` to force the environment to `"Development"`.
- [x] Assert that accessing `/swagger/index.html` without credentials returns `401 Unauthorized`.
- [x] Assert that accessing `/swagger/v1/swagger.json` without credentials returns `401 Unauthorized`.
- [x] Assert that accessing `/swagger/index.html` with correct Basic credentials returns `200 OK`.
- [x] Assert that accessing `/swagger/v1/swagger.json` with correct Basic credentials returns `200 OK`.

### Blocked by
- Issue 3: Implement Swagger Extension and Wire in Program.cs

---

## Issue 5: Sync System Documentation

### What to build
Update `.ai/` documentation (codebase map, decisions registry, conventions, roadmap next steps, and changelog) to reflect the completed Swagger integration.

### Acceptance criteria
- [x] Document files in `CODEBASE_MAP.md` and Key Types Registry.
- [x] Update `CONVENTIONS.md` to note that Swagger UI is protected via Basic Auth and gated to `Development`.
- [x] Create a decision record for "Swagger Basic Auth Protection" in `DECISIONS.md`.
- [x] Set Swagger Integration to completed in `NEXT_STEPS.md`.
- [x] Append changelog entry in `CHANGELOG.md`.
- [x] Overwrite `SESSION_SUMMARY.md` tracking completed work and test results.

### Blocked by
- Issue 4: Add Functional Tests for Swagger Auth
