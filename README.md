# NutriTrack

A nutrition and calorie tracking application. Users register, log meals, manage
recipes, and query nutritional breakdowns over time. It is built as a .NET
solution with a REST API backed by SQL Server and a Blazor WebAssembly client.

## What it does

The application revolves around five domains:

- **Identity** — user registration, JWT-based authentication, and refresh token rotation.
- **FoodCatalog** — a read-only food database with nutrients, brands, and serving definitions.
- **Recipes** — users create, view, and delete personal recipes built from catalog foods, with per-serving nutrition breakdowns.
- **MealLogging** — users log meals by referencing foods or recipes directly. Recipes are expanded into their constituent foods at log time (scaled by the logged grams), so meal history stays stable even if the recipe changes later. Daily and ranged nutrition summaries are computed from the logged foods.
- **UserPreferences** — per-user settings.

## Architecture

The solution is split into five projects with a one-directional dependency flow:

```
NutriTrack.Web ─┐                 NutriTrack ─┐   (the API host)
 (Blazor WASM)  │                             │
                └──► NutriTrack.Shared ◄───────┘
                          │
                          ▼
                   NutriTrack.Domain   (entities only, no dependencies)
```

- **NutriTrack.Domain** — entity classes only, grouped by domain (FoodCatalog, Identity, Recipes, MealLogging, UserPreferences). No external dependencies.
- **NutriTrack.Shared** — the bulk of the system: feature services (the business logic), FluentValidation validators, the EF Core `DbContext` and entity configurations, migrations, DTOs, service interfaces, and auth helpers. The nutrition math lives here in `NutritionQueryService` + `NutritionAggregator`.
- **NutriTrack** (API host) — thin ASP.NET Core Web API. Controllers validate auth and delegate to a single feature-service method. Hosts DI wiring, JWT configuration, and exception-handling middleware.
- **NutriTrack.Web** — Blazor WebAssembly client. References Shared to reuse DTOs and service interfaces as the wire contract; all calls go through a central `ApiClient` that attaches the bearer token. The client does not touch EF Core or the database directly.
- **NutriTrack.Tests** — xUnit tests targeting the feature services, with EF Core InMemory.

### Request lifecycle

Each feature is a self-contained service class registered in DI. A controller
action calls one service method; the service validates with FluentValidation and
performs the work via EF Core. Domain exceptions (`NotFoundException`,
`ForbiddenException`) are translated to HTTP status codes by
`ExceptionHandlingMiddleware`.

The same DTO types travel from the Blazor client through the API to the service,
because both ends reference the Shared project.

## Local setup

Registration sends a confirmation email, and the API refuses to start until the
sender is configured. `appsettings.json` carries the non-secret half (SMTP host,
port, STARTTLS); the credentials are per-machine and must come from user secrets,
never from a tracked file — this repo is public.

For Gmail, create an [app password](https://myaccount.google.com/apppasswords)
(requires 2-Step Verification on the account), then:

```bash
dotnet user-secrets set "Email:User" "you@gmail.com" --project NutriTrack/NutriTrack.Api.csproj
dotnet user-secrets set "Email:FromAddress" "you@gmail.com" --project NutriTrack/NutriTrack.Api.csproj
dotnet user-secrets set "Email:Password" "your-16-char-app-password" --project NutriTrack/NutriTrack.Api.csproj
```

`Email:FromAddress` must match `Email:User` — Gmail rejects a mismatched sender.
In deployment the same keys can come from `Email__User` / `Email__Password`
environment variables instead, since user secrets are Development-only.

To develop without sending real mail, run a local catcher
(`dotnet tool install --global rnwood.smtp4dev`, then `smtp4dev`) and point
`Email:Host` at `localhost` with `Port` 25 and `UseStartTls` false; it accepts
anything unauthenticated, so leave `Email:User` and `Email:Password` unset.
`Email:FromAddress` is still required — any address will do, since nothing is
relayed.

`App:ClientBaseUrl` must match the URL you browse the Blazor client on, since
confirmation links are built from it (`http://localhost:5107` for the `http`
launch profile, `https://localhost:7115` for `https`).

## Tech stack

- ASP.NET Core Web API with Controllers
- Blazor WebAssembly client (Blazored.LocalStorage for token storage)
- Plain injected feature-service classes for request handling (one self-contained service per domain)
- Entity Framework Core (SQL Server) for data access, with code-first migrations
- FluentValidation for server-side request validation (the *Request DTOs also carry DataAnnotations for the Blazor client)
- JWT Bearer tokens with refresh token rotation for authentication
- BCrypt for password hashing
- Scalar / OpenAPI for API documentation (Development environment)
