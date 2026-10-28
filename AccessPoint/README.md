# AccessPoint

A standalone, database-backed ASP.NET Core application with an HTML/CSS/JavaScript front end. This is a substantial engineering sample, not a portfolio website or a claim of past client work. Created with AI assistance; review and understand the implementation before presenting it as your own work sample.

## Implemented capabilities

- Identity-based login with Citizen, Staff and Admin roles.
- Provider schedules and database-backed availability.
- Reservation with a required idempotency key and payload fingerprint.
- Database unique constraint preventing duplicate active reservations.
- Rescheduling and cancellation with optimistic concurrency checks.
- Staff check-in and completion workflow; admin schedule publication.
- Owner-scoped queries, server pagination and booking history.
- Transactional audit/outbox writes and durable local delivery processing.

## Start locally

Requirements: .NET 10 SDK. SQLite is the default and requires no database installation. Python 3 and Node.js are required only for the supplied verification script.

From this project folder:

```bash
dotnet user-secrets set "Seed:Password" "YOUR-OWN-STRONG-PASSWORD"
dotnet restore
dotnet run
```

Replace the placeholder with your own password of at least 12 characters including uppercase, lowercase, a number and a symbol. Do not commit it. Open **http://127.0.0.1:5080**.

The launch profile enables Development mode. On the first run it creates `application.db`, development accounts and fictional records. Subsequent runs preserve data. The seeder does not reset existing account passwords.

| Account | Role |
|---|---|
| admin@example.test | Admin |
| staff@example.test | Staff |
| citizen@example.test | Citizen |
| other@example.test | Second citizen for ownership checks |

Each account uses the password you configured during initial creation. These are fictional local accounts, not production identities.

## What to demonstrate

1. Sign in as a citizen and complete the main workflow.
2. Sign out, then use the other citizen to verify record isolation.
3. Sign in as staff and demonstrate the operational workflow.
4. Sign in as admin and inspect the operations monitor.
5. Open the service and data-model code to explain concurrency and transaction boundaries.

## Repository layout

| Folder/file | Responsibility |
|---|---|
| Controllers/ | ASP.NET Core MVC API controllers, authentication and role gates |
| Services/ | Application workflows and business validation |
| Domain/ | Entities, input contracts and workflow rules |
| Data/ | EF Core mapping, indexes, constraints and development seeding |
| Infrastructure/ | Actor context, exception responses, audit/outbox support and background worker |
| wwwroot/ | Working front end, session handling and API calls |
| tests/ | HTTP regression checks against a real SQLite database |
| scripts/verify.sh | Isolated end-to-end verification runner |
| docs/ | Architecture, API map, security/accessibility scope and test results |

## Verify

On Linux/macOS or Windows with WSL/Git Bash:

```bash
bash scripts/verify.sh
```

The script builds the project, syntax-checks JavaScript, creates an isolated temporary database, generates a temporary password, starts the server and runs HTTP checks. It stops the server and removes test data afterward. Set `TEST_PORT=5081` if the default port is busy. No npm packages are needed.

**Verified in the creation environment:** .NET build succeeded with zero warnings/errors, JavaScript syntax checks passed, and **36 HTTP assertions passed** against SQLite. See `docs/TEST_RESULTS.md`.

## SQL Server option

Configure these environment variables before running:

```text
Database__Provider=SqlServer
ConnectionStrings__App=<your SQL Server connection string>
```

Both EF providers are included. SQLite was tested; SQL Server integration was not executed. Development uses `EnsureCreated` only for a new database. Production schema migration scripts are not included: generate and review provider-specific EF migrations, configure authenticated accounts and apply them before production startup. Do not mix `EnsureCreated` with migrations on an existing database.

## Docker

```bash
docker build -t accesspoint .
```

Run the container with a locally supplied `Seed__Password`, `ASPNETCORE_ENVIRONMENT=Development`, a persistent volume at `/app/data`, and port binding `127.0.0.1:5080:8080`. Do not put passwords in source or committed Compose files. Docker execution was not tested here.

## Scope

This application demonstrates production-relevant patterns but is not production-certified. It supports one application instance/worker; distributed worker coordination is not implemented. The outbox adapter records local deliveries and does not send email or SMS. Full WCAG/Section 508 evaluation, SQL Server integration, deployment hardening and real identity provisioning remain follow-up work. No government affiliation or completed client engagement is implied.
