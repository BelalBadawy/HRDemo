# 4. Swagger Integration with Basic Auth and JWT PRD

## Problem Statement

The application has various Minimal API endpoints, but lacks a visual tool or interactive document for developers and testers to inspect and execute them. Exposing the raw OpenAPI specification or Swagger UI publicly is a significant security risk as it exposes the API blueprint. Therefore, we need to integrate Swagger UI for visual debugging and testing, but ensure it is strictly protected by Basic Authentication using static credentials, and completely disabled outside the `Development` environment.

## Solution

Integrate `Swashbuckle.AspNetCore` into the API project, but constrain its registration and middleware to execution only in the `Development` environment. To protect the API schema and UI:
1. Implement a custom middleware `SwaggerBasicAuthMiddleware` that intercepts all requests matching `/swagger/*` (both UI files and JSON schema).
2. Validate incoming credentials from the `Authorization: Basic` header against a `SwaggerAuth` section in `appsettings.Development.json`.
3. If credentials are empty, missing, or invalid, return `401 Unauthorized` along with `WWW-Authenticate: Basic` header to trigger browser-native login popups.
4. Configure Swagger to include a JWT Bearer security scheme definition, allowing developers to authenticate and execute endpoints directly from the UI.
5. Generate XML documentation files for the API project to enrich the Swagger UI with comments, suppressing warning CS1591.

## User Stories

1. As a developer, I want to view a visual Swagger UI page at `/swagger` when running in the `Development` environment, so that I can explore and understand the available API routes.
2. As a security architect, I want the Swagger UI and OpenAPI schema endpoints to be disabled in non-development environments, so that we prevent public API blueprint exposure.
3. As a developer, I want all `/swagger` endpoints (including the UI and the underlying `/swagger/v1/swagger.json` document) to require Basic Authentication, so that only authorized users can access the API layout.
4. As a tester, I want unauthorized requests to any `/swagger` endpoint to return a `401 Unauthorized` status with a `WWW-Authenticate: Basic` header, so that my web browser prompts me to log in.
5. As a QA engineer, I want to supply a JWT Bearer token via a security definition in Swagger UI, so that I can easily test claims-authorized endpoints.
6. As a developer, I want the Swagger documentation to include API XML comments, so that route parameters and summary descriptions are clearly documented in the UI.

## Implementation Decisions

- **Environment Gating**: Swagger services and middleware are registered and used only when the application is running in the `Development` environment.
- **Middleware Protection**: A custom `SwaggerBasicAuthMiddleware` intercepts all requests starting with `/swagger`. This protects the HTML UI pages, assets, and the underlying Swagger JSON schema (`/swagger/v1/swagger.json`).
- **Configuration-Backed Credentials**: The middleware validates the decoded credentials from the `Authorization: Basic` header against `SwaggerAuth` settings defined in `appsettings.Development.json`.
- **JWT Bearer Security Scheme**: Swagger is configured with a security definition for `Bearer` tokens (`SecuritySchemeType.Http`, format `"JWT"`) and a security requirement that applies it globally.
- **XML Documentation**: The API project generates XML documentation files during compile time, and Swagger includes these comments to document endpoints. CS1591 warnings are suppressed to prevent warnings on undocumented elements.

## Testing Decisions

- **Good Test Criteria**: Test the external HTTP interface behavior. Unauthenticated requests to `/swagger/index.html` or `/swagger/v1/swagger.json` must return `401 Unauthorized` with the correct `WWW-Authenticate` header. Authenticated requests with correct credentials must return `200 OK`.
- **Modules Tested**: Functional API tests using a custom environment setup.
- **Prior Art**: API functional tests (`AuthEndpointsTests.cs`, `HealthCheckTests.cs`) utilizing `WebApplicationFactory<Program>`.

## Out of Scope

- Supporting multiple users, permissions, or database-backed authentication for Swagger UI.
- Enabling Swagger UI in non-development (e.g. Staging, Production) environments.
- Configuring security schemes other than JWT Bearer in Swagger (e.g., OAuth2, API Keys).
