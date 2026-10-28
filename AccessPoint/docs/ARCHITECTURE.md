# Architecture and decisions

## Request path
Authenticated browser → same-origin MVC API controller → application workflow → EF Core transaction → SQLite (default) or SQL Server (configured).

Native browser controls provide keyboard behavior. Mutating requests carry antiforgery tokens. Server authorization is authoritative; hiding a UI control is not treated as an access boundary.

## Persistence and consistency
Identity and business data live in the same database. Each successful mutation commits its state change, audit event and outbox record through the same DbContext SaveChanges transaction. GUID version tokens implement optimistic concurrency. Read models are projected or scoped before materialization.

AccessPoint adds a filtered unique index on active booking SlotId, a unique provider/start schedule index, and a unique actor/idempotency-key receipt. These database constraints remain effective under competing writers. Concurrent retries with the same new key may first receive a conflict; retrying after the first transaction commits returns the recorded result.

CivicFlow validates transitions before mutation. Only staff can update operational fields. Internal comments are filtered on the server before they reach citizen responses. SLA changes are recorded by a background system actor and also change the version token.

## Outbox
A periodic worker processes a bounded batch of pending events into a durable local delivery table. The delivery ID equals the message ID, giving a unique deduplication key. Delivery and processed markers commit together. This is a local sink, not an external email transport. Failures are logged and retried on later cycles. Multi-worker leases, dead-letter administration and external transport acknowledgements are not implemented.

## Tradeoffs
SQLite provides a frictionless persistent local run. SQL Server support is configured through EF Core but requires provider integration testing. Controllers and services use the DbContext directly rather than introducing a generic repository that obscures transaction boundaries. The front end uses small JavaScript modules instead of requiring a build system. This keeps setup simple while preserving server-backed behavior.

## Extension points
Add tenant boundaries if needed; adopt a production identity provider; generate reviewed migrations; use worker leases before scaling horizontally; add telemetry export and alerting; add an external notification adapter with recipient consent and delivery idempotency. These are future work, not claimed capabilities.
