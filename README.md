# dotNetApiProBoilerplate

Professional .NET API boilerplate focused on clean architecture, scalability, and long-term maintainability.

---

## Features

- Clean separation of concerns (Api / Domain / Dto / Infrastructure / Services)
- Installer pattern to keep `Program.cs` clean and modular
- **MediatR (CQRS-style commands/queries + handlers)**
- **MediatR pipeline behaviors (e.g. UnitOfWorkBehavior)**
- JWT Bearer authentication (API-first; no redirects)
- ASP.NET Core Identity integration
- Global exception handling middleware with standardized JSON errors
- Repository + Unit of Work
- AutoMapper profile-based mapping (safe update rules)
- API versioning (URL segment + header)
- Swagger/OpenAPI with JWT support
- In-memory database setup (easy to replace with PostgreSQL/SQL Server)

---

## Solution structure

```
src/
 ├─ Api/            HTTP layer (controllers, middleware, installers, Program.cs)
 ├─ Domain/         Pure business models (entities, enums)
 ├─ Dto/            API contracts (requests, results, queries, paging)
 ├─ Infrastructure/ EF Core, Identity, repositories, JWT generator
 └─ Services/       Business logic, MediatR handlers, mapping, exceptions, behaviors
```

Rule of thumb:
- Controllers: HTTP only (send commands/queries via IMediator)
- Services: use-cases and business rules (handlers + services)
- Infrastructure: persistence + identity + token generation
- DTOs: transport contracts
- Domain: internal truth

---

## Requirements

- .NET 8 (or newer)

---

## Run locally

```bash
dotnet restore
dotnet run --project src/Api/dotNetApiProBoilerplate.Api
```

Development tooling:
- Swagger UI is enabled only in Development environment.

---

## MediatR (CQRS-style)

Controllers dispatch requests through `IMediator`:
- Commands: write operations (Create/Update/Delete)
- Queries: read operations (GetById/GetAll/Search)

Handlers live in the Services project (example layout):

```
Services/
  Features/
    Products/
      Create/
      GetById/
      GetAll/
      Update/
      Delete/
      Search/
    Auth/
      Register/
      Login/
      Refresh/
      ChangePassword/
  Behaviors/
    UnitOfWorkBehavior.cs
```

Pipeline behaviors allow cross-cutting concerns (transactions, logging, validation) without polluting handlers/controllers.

---

## Authentication (JWT)

- Tokens are validated for issuer, audience, lifetime, and signing key
- Custom JWT challenge handler returns JSON `401` (no HTML redirects)

Send token via header:

```http
Authorization: Bearer <token>
```

---

## Error handling

All errors are returned in a single standardized JSON shape from global middleware (`ExceptionHandlingMiddleware`), mapped from domain/service exceptions:

- 400: `ValidationException`
- 401: `UnauthorizedAccessException`
- 403: `ForbiddenException`
- 404: `NotFoundException`
- 409: `ConflictException`
- 500: fallback for unhandled exceptions

---

## API versioning

- URL segment: `/api/v{version}/...`
- Header: `X-Api-Version: 1.0`
- Default: `v1.0` when unspecified

---

## Extending the boilerplate

Add features using the same layer boundaries:

1. Domain: entity + enums
2. DTO: request/result/query models
3. Infrastructure: persistence support (if needed)
4. Services: business logic (handlers/services) + mapping + exceptions
5. API: controller endpoints (IMediator only)

Avoid putting business logic in controllers.

---

## License

Personal and commercial use allowed.

Redistribution of the boilerplate source as a competing template/boilerplate product is not allowed.
