NutriTrack is a REST API built with .NET / C# using a Vertical Slice Architecture pattern, backed by a SQL Server database.

What it does
A nutrition and calorie tracking application that lets users log meals, manage recipes, and query nutritional breakdowns. It revolves around four domains:

Identity — user registration, JWT-based authentication, and refresh token rotation
FoodCatalog — a read-only food database with nutrients, brands, and serving definitions
Recipes — users can create, view, and delete personal recipes built from catalog foods, with per-serving nutrition breakdowns computed on the fly
MealLogging — users log meals by referencing foods or recipes directly; recipes are automatically expanded into their constituent foods at log time so history stays stable even if the recipe changes later. Daily nutrition summaries are computed via raw SQL through Dapper and cached for past days


Tech stack

ASP.NET Core Web API with Controllers
MediatR for request handling — every feature is a self-contained command or query
FluentValidation wired into the MediatR pipeline for automatic validation
Entity Framework Core for standard CRUD operations
Dapper for the complex nutrition aggregation query
IMemoryCache for daily nutrition summary caching
JWT Bearer tokens with refresh token rotation for authentication
BCrypt for password hashing
Swagger UI for API documentation and testing
