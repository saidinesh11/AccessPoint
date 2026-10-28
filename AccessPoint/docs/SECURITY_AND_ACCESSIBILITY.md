# Security and accessibility scope

Implemented: ASP.NET Core Identity password hashing, lockout on failed sign-in, role gates, citizen ownership filtering, antiforgery validation, HttpOnly/SameSite cookies, secure cookies outside Development, a CSP, no-store API responses, server-side validation, parameterized EF queries, rate limiting and sanitized exception responses. Unhandled failures log a trace identifier. CSV fields in CivicFlow escape formula prefixes.

Development accounts are fictional and use a password supplied locally. Development bootstrap must not be enabled for a public production deployment. Persist and protect data-protection keys in production, configure TLS and trusted proxy handling for the actual host, review credentials and backups, and provision real identities. No multi-tenant isolation is claimed.

Front-end patterns: semantic headings/landmarks, a skip link, visible labels, native controls, focusable error/status feedback, native modal dialogs, descriptive action labels, responsive layout and text-based status. User content is rendered with textContent.

Full accessibility evaluation is NOT complete. Required follow-up: keyboard-only task completion, JAWS/NVDA testing, error announcements, dialog focus restoration, 320px reflow/400% zoom, contrast measurement and axe checks. A professional's DHS Trusted Tester certification does not certify the application itself.
