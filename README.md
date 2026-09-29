# Hotel Booking System

A backend for a hotel booking platform, built with .NET 10 and Clean Architecture. It powers the full guest journey — browse and search hotels, open a hotel's details, hold rooms, pay with Stripe, and get an invoice by email — plus an admin area for managing cities, hotels, rooms, amenities, discounts, and images.

This started as a training project, so the code leans on patterns you would use in a real production service: a clear layer separation, a CQRS pipeline, a transactional Outbox for side effects, token-based auth with revocation, and background workers. The sections below explain not just what is there, but why each piece exists.

---

## Table of contents

- [Features](#features)
- [Architecture](#architecture)
- [How a request flows](#how-a-request-flows)
- [Key techniques, in plain terms](#key-techniques-in-plain-terms)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [API endpoints](#api-endpoints)
- [Admin endpoints](#admin-endpoints)
- [Testing](#testing)
- [Get Involved](#get-involved)

---

## Features

---

Grouped by domain:

- **Authentication & accounts** — register, confirm email, log in, refresh tokens, log out, and log out of every device. Two roles: `User` and `Admin`.
- **Hotels** — search with filters and paging, hotel details, featured deals, and recently visited hotels.
- **Cities** — trending destinations for the home page.
- **Bookings** — create a booking that holds the rooms, pay for it with Stripe, and download the invoice as JSON or PDF.
- **Reviews** — guests who stayed can review a hotel; anyone can read a hotel's reviews.
- **Admin** — full management of cities, hotels, rooms, room types, amenities, and discounts (including bulk creation), plus image uploads for cities, hotels, and rooms, and an outbox dashboard to inspect and requeue failed events.

---

## Architecture

---

The solution follows Clean Architecture with four projects. Dependencies only ever point inward, so the core business code never depends on frameworks or infrastructure:

```
Presentation  ->  Infrastructure  ->  Application  ->  Domain
```

- **HotelBooking.Domain** — Entities and enums in plain C#. No framework references.
- **HotelBooking.Application** — The use cases. Holds the CQRS contracts and the dispatcher, the pipeline behaviors, the Result type, and the interfaces (abstractions) that the outer layers implement. This is where the business rules live.
- **HotelBooking.Infrastructure** — The concrete implementations: EF Core and the database, Identity, Redis, Stripe, Cloudinary, SMTP, the Outbox machinery, and the background workers.
- **HotelBooking.Presentation** — The ASP.NET Core Web API: controllers, middleware, logging, Swagger, rate limiting, and dependency-injection wiring.

Inside the Application layer, code is organized by **feature (vertical slices)** rather than by technical type. Each feature owns its command or query, its handler, its validator, its response, and its errors — so everything about one use case sits in one folder:

```
Application/Features/
  Auth/        Register, Login, Refresh, Logout, LogoutAll, Email confirmation
  Bookings/    Create, Pay, Invoice
  Hotels/      Search, GetDetails, FeaturedDeals, RecentlyVisited
  Reviews/     Create, GetForHotel
  Cities/      GetTrending
  Admin/       Cities, Hotels, Rooms, RoomTypes, Amenities, Discounts, Images, Users, Outbox
```

---

## How a request flows

---

Every request takes the same path:

```
Controller  ->  IDispatcher  ->  [ Logging -> Validation -> UnitOfWork ]  ->  Handler  ->  Result  ->  HTTP response
```

The controller stays thin: it builds a command or query and hands it to the dispatcher. The dispatcher runs it through a pipeline of behaviors and into the matching handler. The handler returns a `Result`, which the controller maps to an HTTP response — or to an RFC 7807 `ProblemDetails` on failure.

---

## Key techniques, in plain terms

---

### CQRS with a hand-written dispatcher

Reads and writes are separated at the type level: writes are `ICommand` / `ICommand<T>`, reads are `IQuery<T>`. Instead of pulling in MediatR, the project uses its own small `Dispatcher`. It looks up the right handler in the container and wraps it with the pipeline behaviors. Because the concrete request type is only known at runtime, it builds a tiny generic wrapper per request type using reflection and **caches it**, so that cost is paid once per type, not per request. Handlers and validators are wired up automatically by an assembly scan (Scrutor), so adding a new feature needs no manual registration.

### The pipeline (cross-cutting behaviors)

Shared concerns are handled once, in order, around every handler:

- **Logging** — logs each request in and out.
- **Validation** — runs the request's FluentValidation rules and short-circuits with a failure before the handler if anything is invalid.
- **Unit of Work** — for commands only: opens a database transaction, runs the handler, then commits on success or rolls back on failure. A command can opt out (when it manages its own transaction), and the transaction runs inside EF Core's retry strategy so it survives transient database errors.

### The Result pattern

Handlers don't throw for expected problems (not found, forbidden, conflict, validation). They return `Result` or `Result<T>` carrying either a value or an `Error` with a category. Controllers translate that into the correct status code and a `ProblemDetails` body. Unexpected exceptions are caught by a global handler and returned the same way, so error responses are consistent everywhere.

### EF Core, with reads and writes kept apart

Persistence is EF Core on SQL Server. Each entity has its own configuration class, and migrations are committed to the repo and applied automatically on startup. The read/write split continues into the data layer: writes go through **repositories**, while reads go through dedicated **readers** that project straight into DTOs, so queries stay lean and untracked.

### Authentication and token revocation

Auth is JWT-only, built on ASP.NET Core Identity (no cookies), with `Guid` keys and the two roles. Access tokens are short-lived; refresh tokens are stored and **rotated** on each use, so a used refresh token can't be replayed. The tricky part with JWTs is logout — a signed token stays valid until it expires. To solve that, every token carries a unique id (`jti`), and on logout that id is written to a **Redis blacklist** with a TTL equal to the token's remaining life, so it cleans itself up. Every request checks the blacklist and rejects revoked tokens. "Log out everywhere" revokes all of a user's tokens. Sensitive auth endpoints (register, login, confirm, resend) are rate-limited per IP.

### The Outbox pattern

Some things must happen only *after* a transaction commits — sending a confirmation email, emailing an invoice. If you just fire them inside the handler, a later rollback leaves you having sent an email for something that never happened; a crash leaves it never sent at all. The Outbox fixes both:

1. The handler saves the event as a row in the same transaction as the business data, so they commit together — atomically.
2. A `SaveChanges` interceptor signals a background worker that new work exists.
3. That worker (`OutboxProcessor`) reads pending rows in batches and dispatches each to its handler. Under load it reacts to the signal instantly; when idle it falls back to polling.
4. Failures are **retried with exponential backoff**, and after too many attempts a row is moved to a **dead-letter** state that an admin can inspect and requeue.

Today it drives the account-confirmation email and the booking-invoice email.

### Payments with Stripe

Payments use Stripe Payment Intents behind an `IPaymentGateway` interface, so the business layer never touches Stripe directly. The pay flow creates an intent for the booking total, then confirms it. Every call uses a **stable idempotency key** derived from the booking, so a retry never charges twice. On success the bookings are confirmed and an invoice event goes to the Outbox; on failure the bookings are cancelled and their room holds are released.

### Booking holds and idempotency

Booking is a two-step "hold then pay" flow that prevents double-booking. Creating a booking validates room availability and capacity, snapshots the price (with any active discount), and writes a **hold** on each requested night. The whole create is guarded by an **idempotency record**: if the same request is retried, it replays the original result instead of creating a duplicate — even under a race. Holds live for a fixed time; a background **sweeper** runs on a timer, finds holds that expired without payment, and releases those nights back to inventory.

### Images, email, and invoices

Images (cities, hotels, rooms) are stored in **Cloudinary** behind an `IImageStorage` interface. Email is sent over SMTP with **MailKit**; in development, docker-compose runs **Mailpit** to catch outgoing mail in a local web UI. Invoices are rendered to PDF with **QuestPDF** and attached to the confirmation email.

### Observability

Logging is structured with **Serilog**. A middleware assigns a **correlation id** to each request and adds it to the log context, so all logs for one request share an id. There's a global exception handler, request logging, and a `/health` endpoint.

---

## Tech stack

---

- .NET 10, ASP.NET Core Web API
- Entity Framework Core 10, SQL Server 2022
- ASP.NET Core Identity (JWT-only) with two roles
- A hand-written CQRS dispatcher and pipeline (no MediatR)
- FluentValidation for validation, Scrutor for DI scanning
- Redis (StackExchange.Redis) for token revocation
- Stripe.net for payments
- Cloudinary for image storage
- MailKit for email, QuestPDF for invoice PDFs
- Serilog for logging
- Docker Compose for local infrastructure (SQL Server, Redis, Mailpit)
- xUnit, NSubstitute, FluentAssertions, and Testcontainers for tests

---

## Getting started

---

### Prerequisites

- .NET SDK 10.0
- Docker Desktop (for local infrastructure, and for the integration tests, which start a real database)
- The EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Configure the environment

```bash
cp .env.example .env
```

Open `.env` and fill in real values (see the [Environment variables](#environment-variables) table below). The SQL Server password must meet complexity rules, and the JWT signing key must be at least 32 bytes.

### 2. Start the infrastructure

```bash
docker compose up -d
```

This starts SQL Server (`localhost:1433`), Redis (`localhost:6379`), and Mailpit (SMTP on `1025`, web UI on `http://localhost:8025`). Check they're healthy with `docker compose ps`.

### 3. Run the API

```bash
dotnet run --project src/HotelBooking.Presentation
```

Migrations are applied automatically on startup. Swagger UI opens where you can try and explore the endpoints (in Development it's served at `/swagger`; when running in Docker it's at `http://localhost:8080/swagger`).

To try the admin functionality, sign in with these credentials:

- **Email:** `admin@hotelbooking.com`
- **Password:** `Admin1234`

### Run everything in Docker

The compose file also builds and runs the API itself:

```bash
docker compose up -d --build
```

The API is then available on `http://localhost:8080`.

### Environment variables

Configuration comes from environment variables (loaded from `.env` in development). See `.env.example` for the complete list.

| Group | Variables |
|-------|-----------|
| **SQL Server** | `MSSQL_SA_PASSWORD`, `DB_CONNECTION` |
| **Redis** | `REDIS_CONNECTION` |
| **JWT** | `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SIGNING_KEY`, `JWT_ACCESS_MINUTES`, `JWT_REFRESH_MINUTES` |
| **Cloudinary** | `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`, `CLOUDINARY_FOLDER` |
| **SMTP** | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_USE_SSL`, `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME` |
| **Email confirmation** | `EMAIL_CONFIRM_TOKEN_LIFETIME_HOURS`, `EMAIL_CONFIRM_TOKEN_BYTE_LENGTH`, `EMAIL_CONFIRM_URL_TEMPLATE`, `EMAIL_CONFIRM_RESEND_COOLDOWN_SECONDS` |
| **Stripe** | `STRIPE_SECRET_KEY`, `STRIPE_CURRENCY` |

### Database migrations

Add a migration when the schema changes:

```bash
dotnet ef migrations add <Name> --project src/HotelBooking.Infrastructure --startup-project src/HotelBooking.Infrastructure --output-dir Persistence/Migrations
```

Apply migrations explicitly:

```bash
dotnet ef database update --project src/HotelBooking.Infrastructure --startup-project src/HotelBooking.Infrastructure
```

Never regenerate a migration that has already been pushed.

---

## API endpoints

---

All routes are prefixed with `api/v1`. Path parameters are shown in `{braces}`. Open `/swagger` while the API is running for the full, grouped, interactive reference.

### Auth

**Register, verify email, sign in, and manage sessions.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `POST` | `/auth/register` | Register a new user and send a confirmation email |
| `POST` | `/auth/confirm-email` | Confirm an account with the emailed token |
| `POST` | `/auth/resend-confirmation` | Resend the account-confirmation email |
| `POST` | `/auth/login` | Log in and receive an access and refresh token |
| `POST` | `/auth/refresh` | Exchange a refresh token for a new token pair |
| `POST` | `/auth/logout` | Revoke the current access token |
| `POST` | `/auth/logout-all` | Revoke all of the current user's tokens |

---

### Hotels

**Public browsing, search, and hotel details.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/hotels/search` | Search hotels with filters and paging |
| `GET` | `/hotels/{id}` | Get a hotel's full details |
| `GET` | `/hotels/featured-deals` | Get hotels with the best active discounts |
| `GET` | `/hotels/recently-visited` | Get the current user's recently visited hotels |

---

### Cities

**Destination discovery for the home page.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/cities/trending` | Get the most popular destination cities |

---

### Amenities

**Reference data for hotel and room features.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/amenities` | List the available amenities |

---

### Reviews

**Read a hotel's reviews, and post one after a stay.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/hotels/{hotelId}/reviews` | Get a page of reviews for a hotel |
| `POST` | `/reviews` | Create a review for a hotel the user booked |

---

### Bookings

**Create, pay for, and invoice a booking.** _Requires authentication._

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `POST` | `/bookings` | Create a booking that holds the selected rooms |
| `POST` | `/bookings/{bookingGroupId}/payment` | Pay for a booking with Stripe |
| `GET` | `/bookings/{bookingGroupId}/invoice` | Get the booking invoice as JSON |
| `GET` | `/bookings/{bookingGroupId}/invoice/pdf` | Download the booking invoice as PDF |

---

## Admin endpoints

---

> **All endpoints in this section require the `Admin` role.**

### Cities

**Manage cities and their images.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/cities` | Get a paged list of cities |
| `GET` | `/admin/cities/{id}` | Get a city by id |
| `POST` | `/admin/cities` | Create a city |
| `PATCH` | `/admin/cities/{id}` | Update a city |
| `DELETE` | `/admin/cities/{id}` | Delete a city |
| `POST` | `/admin/cities/{cityId}/images` | Upload images for a city |
| `DELETE` | `/admin/cities/{cityId}/images/{imageId}` | Delete a city image |

---

### Hotels

**Manage hotels and their images.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/hotels` | Get a paged list of hotels |
| `GET` | `/admin/hotels/{id}` | Get a hotel by id |
| `POST` | `/admin/hotels` | Create a hotel |
| `PATCH` | `/admin/hotels/{id}` | Update a hotel |
| `DELETE` | `/admin/hotels/{id}` | Delete a hotel |
| `POST` | `/admin/hotels/{hotelId}/images` | Upload images for a hotel |
| `PUT` | `/admin/hotels/{hotelId}/images/{imageId}/primary` | Set a hotel's primary image |
| `DELETE` | `/admin/hotels/{hotelId}/images/{imageId}` | Delete a hotel image |

---

### Rooms

**Manage rooms and their images.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/hotels/{hotelId}/rooms` | Get a paged list of rooms in a hotel |
| `GET` | `/admin/hotels/{hotelId}/rooms/{id}` | Get a room by id |
| `POST` | `/admin/hotels/{hotelId}/rooms` | Create a room |
| `PATCH` | `/admin/hotels/{hotelId}/rooms/{id}` | Update a room |
| `DELETE` | `/admin/hotels/{hotelId}/rooms/{id}` | Delete a room |
| `POST` | `/admin/hotels/{hotelId}/rooms/{roomId}/images` | Upload images for a room |
| `DELETE` | `/admin/hotels/{hotelId}/rooms/{roomId}/images/{imageId}` | Delete a room image |

---

### Room types

**Manage the room-type catalog.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/room-types` | List room types |
| `POST` | `/admin/room-types` | Create a room type |
| `PATCH` | `/admin/room-types/{id}` | Update a room type |
| `DELETE` | `/admin/room-types/{id}` | Delete a room type |

---

### Amenities

**Manage the amenity catalog.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/amenities` | Get a paged list of amenities |
| `POST` | `/admin/amenities` | Create an amenity |
| `PATCH` | `/admin/amenities/{id}` | Update an amenity |
| `DELETE` | `/admin/amenities/{id}` | Delete an amenity |

---

### Discounts

**Manage room discounts, one at a time or in bulk.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/discounts` | Get a paged list of discounts |
| `GET` | `/admin/discounts/{id}` | Get a discount by id |
| `POST` | `/admin/discounts` | Create a discount |
| `POST` | `/admin/discounts/bulk` | Create multiple discounts in one request |
| `DELETE` | `/admin/discounts/{id}` | Delete a discount |

---

### Users

**Provision admin accounts.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `POST` | `/admin/users` | Create a new admin user |

---

### Outbox

**Inspect and recover failed integration events.**

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/admin/outbox/dead-letters` | List failed (dead-lettered) integration events |
| `POST` | `/admin/outbox/{id}/requeue` | Requeue a dead-lettered event for reprocessing |

---

## Testing

---

```bash
dotnet test
```

- **Unit tests** cover handlers, the validation behavior, and the Outbox components in-process (xUnit, NSubstitute, FluentAssertions).
- **Integration tests** run real end-to-end flows — auth, the Outbox, payments, and the hold sweeper — against a real SQL Server started with Testcontainers. Docker Desktop must be running.

---

## Get Involved

---

Your feedback and contributions are welcome!

**Ways to contribute:**

- **Feedback** — share your thoughts and ideas.
- **Issue reporting** — help by reporting any bugs or issues on GitHub.
- **Code contributions** — contribute to the codebase.

**Contact and support:**

- **Email:** [abdulhamidhajhamad@gmail.com](mailto:abdulhamidhajhamad@gmail.com)
- **GitHub:** [abdulhamidhajhamad](https://github.com/abdulhamidhajhamad)

Thank you for your interest. I look forward to hearing from you!