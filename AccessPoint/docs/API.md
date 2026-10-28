# API map

All business endpoints require an authenticated cookie. Obtain a session and antiforgery token with GET /api/account/session. Supply X-CSRF-TOKEN on every unsafe method, including login/logout. Refresh the session token after changing authenticated identity.

Common: POST /api/account/login; POST /api/account/logout; GET /api/operations (Admin); GET /health/live; GET /health/ready.

GET /api/catalog; GET /api/slots; GET /api/bookings; GET /api/bookings/{id}; POST /api/bookings (Idempotency-Key required); POST /api/bookings/{id}/reschedule; POST /api/bookings/{id}/actions; GET /api/bookings/{id}/history; POST /api/slots (Admin).

Input contracts are typed records in Domain/AppModels.cs. A changed GUID version must be refreshed after every successful workflow mutation. Common responses: 400 invalid input or antiforgery failure; 401 no authenticated session; 403 insufficient role; 404 missing or inaccessible record; 409 stale state, occupied slot or illegal transition; 429 rate limit. Unhandled errors return sanitized ProblemDetails with traceId.

See tests/api_regression.py for executable request sequences, including cookie management and antiforgery headers.
