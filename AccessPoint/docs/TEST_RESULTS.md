# Verification results

Executed using .NET SDK 10.0.403 and EF Core 10.0.14 against isolated SQLite databases.

- Build: passed, zero warnings and errors.
- JavaScript syntax: passed for all source modules.
- HTTP regression checks: 36 assertions passed.
- SQL Server, Docker runtime, load testing, full browser interaction testing and assistive technologies: not tested.

```text
PASS session endpoint
PASS login requires antiforgery token
PASS session endpoint
PASS login citizen@example.test
PASS session endpoint
PASS session endpoint
PASS login other@example.test
PASS session endpoint
PASS session endpoint
PASS login staff@example.test
PASS session endpoint
PASS session endpoint
PASS login admin@example.test
PASS session endpoint
PASS bookings require authentication
PASS seeded schedule availability
PASS idempotency key required
PASS reservation persisted
PASS idempotent replay returns same booking
PASS key reuse with different payload rejected
PASS other citizen cannot read booking
PASS double booking rejected
PASS citizen cannot check in
PASS reschedule saved
PASS stale version rejected
PASS cancellation saved
PASS cancelled slot released
PASS released original slot reservable
PASS staff check-in
PASS staff completion
PASS booking audit entries persisted
PASS schedule publication admin-only
PASS admin publishes schedule
PASS outbox processed into durable local sink
PASS operations monitor admin-only
PASS logout
36 HTTP assertions passed.
```

The tests exercise running ASP.NET Core applications, not mocked API responses. This is regression evidence, not a production certification or a full penetration/accessibility test.
