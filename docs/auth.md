# Authentication

## Overview

Authentication for the hotel booking platform uses **ASP.NET Core Identity** (`AddIdentityCore`, JWT-only) with **role-based authorization** and a small role catalog: `User` (default for every registered account) and `Admin` (protects admin endpoints).

This document grows one endpoint at a time. It currently covers **registration**. Login, refresh, logout, and admin seed will be appended as their tickets land.

- Access tokens will be JWT (HS256, 15-min TTL) — introduced in the login ticket.
- Refresh tokens will be stored in SQL as hashes with 30-min sliding TTL — introduced in the refresh ticket.
- Access-token revocation will use a Redis JTI blacklist — introduced in the logout ticket.
- Authorization strategy: role-based, attribute-based (`[Authorize(Roles = "Admin")]`).

## Endpoint: `POST /api/v1/auth/register`

Creates a new user account and assigns the default `User` role.

### Request

```json
{
  "email": "ahmad@example.com",
  "fullName": "Ahmad Hamad",
  "password": "SecurePass1"
}
```

### Response — 200 OK

```json
{
  "userId": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
  "email": "ahmad@example.com"
}
```

### Response — 400 Bad Request (validation failed)

```json
{
  "type": "Validation.Failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "Email: 'Email' is not a valid email address.; Password: Password must contain at least one uppercase letter."
}
```

### Response — 409 Conflict (email already registered)

```json
{
  "type": "Auth.EmailAlreadyRegistered",
  "title": "Conflict",
  "status": 409,
  "detail": "An account with email 'ahmad@example.com' already exists."
}
```

## What this ticket delivers

**Application layer**
- `Common/Errors/AuthErrors` — central catalog of `Error` factories with stable machine-readable codes.
- `Abstractions/IUserRegistrar` — narrow one-method abstraction so the handler never touches ASP.NET Core Identity types.
- `Features/Auth/Register/`:
    - `RegisterCommand` — sealed record implementing `ICommand<RegisterResponse>` (triggers `TransactionBehavior`).
    - `RegisterResponse` — id + email only; never returns password data.
    - `RegisterCommandValidator` — FluentValidation rules for email, full name, password (matches Identity's `PasswordOptions`).
    - `RegisterCommandHandler` — orchestrates via `IUserRegistrar`, shapes the response. No transaction, logging, or validation code — handled by pipeline behaviors.

**Infrastructure layer**
- `Identity/UserRegistrar` — concrete implementation using `UserManager<ApplicationUser>`. Duplicate-email check → `CreateAsync` → `AddToRoleAsync("User")`. Every Identity error is translated into a `Result.Failure`.
- `Persistence/Configurations/ApplicationRoleConfiguration` — declares the two Identity roles (`User`, `Admin`) via EF `HasData` with fixed Guids and fixed `ConcurrencyStamp`s. Roles ship with the schema; a `SeedIdentityRoles` migration inserts the rows.
- `DependencyInjection` — registers `IUserRegistrar`.

**Presentation layer**
- `Common/Extensions/ResultExtensions` — single place that maps `Result` / `Result<T>` to `IActionResult`. Every error becomes an RFC 7807 `ProblemDetails` payload.
- `Controllers/AuthController` — thin `POST /api/v1/auth/register`.

## Design decisions

### 1. `IUserRegistrar` — abstraction over `UserManager`

**What.** A one-method interface in the Application layer. Infrastructure implements it using `UserManager<ApplicationUser>`.

**Why.** Handlers must not depend on `Microsoft.AspNetCore.Identity` concrete types (Dependency Inversion). This keeps Application clean, unit-testable with a fake `IUserRegistrar`, and swappable in the future (e.g. external identity provider) without touching the handler.

**Benefit.** The `RegisterCommandHandler` is 20 lines and testable with one mocked dependency. Zero setup for a unit test — no in-memory Identity, no `UserManager` mock.

### 2. Result pattern, never exceptions for business failures

**What.** Every domain-level failure — duplicate email, invalid input, weak password — is a `Result.Failure(Error)`. Exceptions are reserved for infrastructure faults (DB down).

**Why.** Business failures are values, not exceptions. Explicit in the type system (`Task<Result<Guid>>` says "this can fail — here's how"). Cheap (exceptions cost ~100× a normal return in .NET). Consistent HTTP mapping via `ResultExtensions`.

**Benefit.** Consistent 4xx responses across every future endpoint. One place to change the mapping.

### 3. `AuthErrors` — central error catalog

**What.** Static class holding factory methods for every auth-domain error. Machine-readable codes (`Auth.EmailAlreadyRegistered`) live here.

**Why.** Callers get IntelliSense (`AuthErrors.EmailAlreadyRegistered("...")`), codes stay stable across refactors, translation/i18n has one place to hook.

**Benefit.** Adding a new auth error = one method in this file. `AuthController` never constructs `Error` directly.

### 4. Default role assigned atomically with user creation

**What.** `UserRegistrar` calls `CreateAsync` then immediately `AddToRoleAsync("User")` inside the same `TransactionBehavior`-managed transaction. If role assignment fails, the user creation is rolled back.

**Why.** A user without a role is a broken state — no permissions, cannot log in properly. Atomicity avoids leaving orphan users in the DB.

**Benefit.** Guaranteed state consistency without hand-written transaction code in the handler.

### 5. Role bootstrap in a migration, not in a runtime seeder

**What.** The two Identity roles (`User`, `Admin`) are inserted by an EF migration (`SeedIdentityRoles`) via `HasData` on `ApplicationRole`. Guids and `ConcurrencyStamp`s are fixed constants so the migration is deterministic.

**Why.** Static reference data the system requires to function is a schema concern, not a runtime concern. A migration ships the rows once; every environment that applies migrations has them. No `IHostedService` running on every startup, no idempotency logic to maintain, no first-run seeding step in the setup guide.

**Benefit.** Zero startup cost. Mentor sees the roles declared in a migration file — auditable, versioned, reversible. Adding a role later = a new migration (which is what migrations are for). Environment-specific bootstrap that legitimately needs runtime code (e.g. a default admin user from `.env`) can be added later without touching this decision.

### 6. Password rules in two places (validator + Identity options), by design

**What.** Password policy is enforced by both `RegisterCommandValidator` and Identity's `PasswordOptions` (in `AddInfrastructure`).

**Why.** Two different jobs. FluentValidation returns a **friendly 400 with clear messages** before the DB is touched. Identity's `PasswordOptions` is the **backstop** — even if the validator is bypassed (direct `UserManager` call from an admin tool later), the policy still holds.

**Benefit.** UX is nice (validator gives specific messages) and the security invariant survives even if the validation layer is missed.

### 7. Response DTO excludes any credential material

**What.** `RegisterResponse` = `{ userId, email }`. No password, no token, no security stamp.

**Why.** Register is not login. The client should call `/auth/login` next to get a token. Never leak credential material in any response body.

**Benefit.** Register cannot accidentally hand out an authenticated session. Login is the only place tokens are minted.

## Known deferred work

- **Login endpoint + JWT issuance** — next ticket.
- **Refresh + rotation + reuse detection** — after login.
- **Logout + logout-all with Redis JTI blacklist** — after refresh (needs Redis in the stack).
- **Default admin user** — a bootstrap admin created from `.env` (`SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`). This one *is* runtime, environment-dependent — shipped as an `IHostedService` in the admin-seed ticket.
- **Rate limiting** on `/auth/register` and `/auth/login` — added when login lands.