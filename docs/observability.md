# Observability

## Overview

Structured logging for the hotel booking API with **Serilog**, a **correlation id** on every request, and **centralized exception handling** via `IExceptionHandler` + RFC 7807 `ProblemDetails`. Logs are written only to `stdout` — the container platform (Docker log driver, later Kubernetes / Fluent Bit / Loki / Elasticsearch) forwards from there. No File sink inside the app. No aggregator wiring in this ticket.

The design goal is **one consistent log line per event, no duplicates, no PII, easy to search later**.

## Design decisions

### 1. Serilog with a fixed enrichment set, JSON in prod, human-readable in dev

**What.** `UseSerilog` replaces the default logger. Four project-owned `ILogEventEnricher`s register with DI and are picked up automatically via `ReadFrom.Services`. Console is the only sink; formatter is `CompactJsonFormatter` in production and a fixed `outputTemplate` in development.

**Why.** Same enrichers, same properties, one output shape per environment. Devs read the same fields in the terminal that show up in the aggregator later — no cognitive switch.

**Benefit.** When you see a bug in prod logs, you already know the shape from dev. Aggregator queries are stable because field names never change.

### 2. Consistent schema — required fields, conditional fields

**Required on every log line:** `@t`, `@l`, `@m`, `@mt`, `@i`, `Application`, `Environment`, `MachineName`, `ThreadId`.

**Conditionally present (only when meaningful):** `CorrelationId`, `RequestPath`, `RequestMethod` (only during an HTTP request), `UserId` (only when authenticated), `RequestName` (only inside a MediatR handler), `@x` (only on exceptions).

**Why.** Log aggregators handle absent fields cleanly. Forcing every line to carry every field creates fake data (empty user id on startup logs) and confuses queries.

### 3. Correlation id in `HttpContext.Items`, not `LogContext`

**What.** `CorrelationIdMiddleware` reads `X-Correlation-Id` from the incoming request (accepts non-empty, ≤64 chars) or generates a 16-char lowercase hex id. Stores it in `HttpContext.Items["CorrelationId"]` and echoes it on the response header. A dedicated `CorrelationIdEnricher` reads that key at log-emission time via `IHttpContextAccessor` and adds it to every log line.

**Why.** `LogContext` scopes are unreliable across exception paths — a `using` disposal pops the scope before the exception reaches the exception handler. `HttpContext.Items` survives, is per-request, and is exactly the pattern ASP.NET Core expects for request-scoped data.

**Benefit.** `GlobalExceptionHandler` reads the correlation id reliably. Clients get the id back on the response header — support tickets quote it, engineers find the entry with one aggregator query.

### 4. `IExceptionHandler` is the sole logger of unhandled exceptions

**What.** `GlobalExceptionHandler` implements `IExceptionHandler`. Registered via `AddExceptionHandler<GlobalExceptionHandler>()`. Runs when any exception reaches the top of the middleware pipeline. Reads correlation id, path, method from `HttpContext`; reads request name from `exception.Data`; logs once at `Error` level with those as structured fields.

**Why.** Every unhandled exception is logged exactly once, in one place, with one shape. Aggregator counters and alerts stay accurate. No duplicate lines.

**Benefit.** One place to change how server errors are logged. One place to change how they render to callers (ProblemDetails).

### 5. `LoggingBehavior` uses `using var _ = LogContext.PushProperty(...)`, tags exceptions via Option B

**What.** The behavior pushes `RequestName` to `LogContext` inside a `using` block. The property is scoped correctly to the async execution of the handler — present for every log line the handler emits, popped when the handler returns (success or exception). If an exception escapes, the behavior tags it with `exception.Data["MediatR.RequestName"] = requestName` and rethrows — no logging.

**Why.** `LogContext.PushProperty` returns `IDisposable`. Skipping `using` leaks the property to whatever async work captures the ExecutionContext (background tasks, timers). Using `using` scopes it correctly. But since `using` pops the property before the exception propagates past the behavior, we need a different channel to carry `RequestName` into the exception log — hence `exception.Data`.

**Benefit.** No cross-request leaks. No duplicate exception logs. `GlobalExceptionHandler` still gets `RequestName` as a first-class structured field.

### 6. `UseSerilogRequestLogging` handles the HTTP-level line; `LoggingBehavior` drops to Debug

**What.** `UseSerilogRequestLogging()` emits one Information-level line per HTTP request with method, path, status, and duration — automatically enriched with the properties added by our enrichers. `LoggingBehavior` emits its "Handling"/"Handled" lines at Debug (visible in dev, off in prod).

**Why.** No duplication in production — one HTTP line per request, no matching "Handled X" line from MediatR. Devs still get MediatR-level detail for tracing by enabling Debug.

### 7. Logging is not alerting

**What.** Log levels reflect what happened — Information for normal flow, Warning for abnormal-but-recovered, Error for unexpected failures, Fatal for cannot-continue. Business failures (400/404/409) are visible via the HTTP status on the request log line; they are not warnings by themselves.

**Why.** Alerting policies (page the on-call when error rate spikes; notify security when failed-login rate exceeds threshold) belong in the log aggregator. Overloading log levels to signal alerts couples code to on-call rotations — bad separation.

**Benefit.** Alerting rules change without a code deploy. Log levels stay descriptive and predictable.

### 8. No request body, no sensitive data

**What.** Nowhere in this design is the request payload logged. `GlobalExceptionHandler` logs only exception + structured properties. `UseSerilogRequestLogging` in default configuration does not log bodies. `LoggingBehavior` emits only the request type name. `UserContextEnricher` exposes only `UserId` — never email, name, or roles.

**Why.** Register, Login, and Checkout carry passwords, PII, and payment data. `{@Request}`-style logging near auth is a well-known incident source. The design forbids it structurally, not by convention.

## File layout

- `src/HotelBooking.Presentation/Common/Logging/ApplicationEnricher.cs` — adds `Application`.
- `src/HotelBooking.Presentation/Common/Logging/CorrelationIdEnricher.cs` — reads `HttpContext.Items`, adds `CorrelationId`.
- `src/HotelBooking.Presentation/Common/Logging/RequestContextEnricher.cs` — reads request path/method, adds them.
- `src/HotelBooking.Presentation/Common/Logging/UserContextEnricher.cs` — reads `ICurrentUser.Id`, adds `UserId`.
- `src/HotelBooking.Presentation/Common/Middleware/CorrelationIdMiddleware.cs` — reads/generates the id.
- `src/HotelBooking.Presentation/Common/Middleware/GlobalExceptionHandler.cs` — sole unhandled-exception logger + ProblemDetails 500.
- `src/HotelBooking.Application/Behaviors/LoggingBehavior.cs` — Debug MediatR lifecycle + Option B exception tagging.
- `src/HotelBooking.Presentation/Program.cs` — Serilog bootstrap, `UseSerilog`, `IExceptionHandler` registration, middleware order.
- `src/HotelBooking.Presentation/appsettings.json` — production defaults (JSON output, Information level).
- `src/HotelBooking.Presentation/appsettings.Development.json` — dev overrides (template output, Debug level).

## Middleware order

```
UseSerilogRequestLogging          // outermost — timer wraps everything, one HTTP log line per request
UseExceptionHandler               // catches everything below; invokes GlobalExceptionHandler
UseMiddleware<CorrelationIdMiddleware>  // sets HttpContext.Items + response header
UseHttpsRedirection
UseAuthentication
UseAuthorization
MapControllers
```

Rationale:
- **`UseSerilogRequestLogging` first** so its per-request line includes everything downstream did, including any exception handler processing.
- **`UseExceptionHandler` before `CorrelationIdMiddleware`** so that middleware itself is covered. `GlobalExceptionHandler` still sees the correlation id because `CorrelationIdMiddleware` runs before any user code and stores the id in `HttpContext.Items`, which survives the exception path.
- **Correlation id before authentication** so unauthenticated errors also carry an id.

## Layer responsibility

| Concern | Owner |
|---|---|
| HTTP-level request log line (method / path / status / duration) | `UseSerilogRequestLogging` |
| MediatR-level lifecycle (Debug in dev, off in prod) | `LoggingBehavior` |
| Unhandled exception log | `GlobalExceptionHandler` (only one) |
| Adding `Application` / `CorrelationId` / `RequestPath` / `RequestMethod` / `UserId` fields | Enrichers |
| Reading/generating the correlation id | `CorrelationIdMiddleware` |
| Domain-specific structured events (`Registered {Email}`, `LoginFailed {IpAddress}`) | Individual handlers, explicit calls |

## Development vs Production behavior

| Aspect | Development | Production |
|---|---|---|
| Minimum log level | `Debug` | `Information` |
| Console sink formatter | `outputTemplate` (human-readable) | `CompactJsonFormatter` (JSON) |
| `LoggingBehavior` visibility | On (Debug) | Off (below threshold) |
| `ProblemDetails.Detail` on 500 | Exception type + message | `"An unexpected error occurred."` + correlation id |
| File sink | None | None |
| Elasticsearch / Loki sink | None | None (added in observability infra ticket, outside this app) |

## Known deferred work

- **Aggregator wiring** (Elasticsearch, Loki, CloudWatch) — configured on the container platform when we deploy, not in the app. The JSON output is already aggregator-friendly.
- **Alerting rules** — configured in the aggregator's alerting layer (Elastalert, Grafana, PagerDuty), not in the app.
- **Distributed tracing** (OpenTelemetry, W3C traceparent) — deferred per the project's scope trim.
- **PII scrubbing pipeline** — the design forbids logging bodies, so no scrubber is needed today. If the policy ever loosens, a Serilog `IDestructuringPolicy` can strip sensitive fields at emission time.