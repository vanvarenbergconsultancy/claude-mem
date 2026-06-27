# Audit Log Analysis

## Overview

The `audit_log` table is a structured event log that records every significant mutation in the claude-mem system. It is written to by both the claude-mem Node.js server (the "upstream") and the Admin API (this codebase). The upstream writes entries as part of ordinary user-facing operations (API key usage, session handling, observation generation). The Admin API writes entries when an operator performs management actions such as creating a team or revoking an API key.

The log is append-only; no upstream code deletes or updates rows. Tenant scoping is soft: `team_id` and `project_id` may be `NULL` when the event occurs before a project context is established, or after a project/team has been deleted (`ON DELETE SET NULL`).

---

## Schema

```sql
CREATE TABLE IF NOT EXISTS audit_log (
  id           TEXT PRIMARY KEY,
  team_id      TEXT REFERENCES teams(id) ON DELETE SET NULL,
  project_id   TEXT REFERENCES projects(id) ON DELETE SET NULL,
  actor_id     TEXT,
  api_key_id   TEXT REFERENCES api_keys(id) ON DELETE SET NULL,
  action       TEXT NOT NULL,
  resource_type TEXT NOT NULL,
  resource_id  TEXT,
  details      JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  CHECK (project_id IS NULL OR team_id IS NOT NULL),
  FOREIGN KEY (project_id, team_id) REFERENCES projects(id, team_id) ON DELETE SET NULL
);
```

| Column | Type | Notes |
|---|---|---|
| `id` | TEXT PK | UUID string assigned by the writer |
| `team_id` | TEXT nullable FK | Tenant scope; SET NULL on team deletion |
| `project_id` | TEXT nullable FK | Project scope; SET NULL on project deletion |
| `actor_id` | TEXT nullable | Human or system identifier who triggered the event |
| `api_key_id` | TEXT nullable FK | API key used to authenticate the request, SET NULL on key deletion |
| `action` | TEXT NOT NULL | Dot-notation event name, e.g. `api_key.create` |
| `resource_type` | TEXT NOT NULL | Category of the affected entity, e.g. `api_key`, `team` |
| `resource_id` | TEXT nullable | ID of the specific affected entity |
| `details` | JSONB NOT NULL | Structured payload; schema is action-specific |
| `created_at` | TIMESTAMPTZ NOT NULL | Wall-clock time, defaulting to `NOW()` |

---

## Event Catalogue

| Action | Resource Type | Details JSONB Shape | Source File | Status |
|---|---|---|---|---|
| `api_key.create` | `api_key` | `{ source?: string }` | `api-key-service.ts` | Implemented (upstream) |
| `api_key.revoke` | `api_key` | `{}` | `api-key-service.ts` | Implemented (upstream) |
| `project.create` | `project` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `project.read` | `project` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `projects.list` | `project` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `session.start` | `session` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `session.end` | `session` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `session.read` | `session` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `event.write` | `event` | `{ eventId, eventType }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `event.batch_write` | `event` | `{ count }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `event.received` | `event` | `{ eventId, eventType }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `event.batch_received` | `event` | `{ count }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `event.read` | `event` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `memory.write` | `memory` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `memory.read` | `memory` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `memory.update` | `memory` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `memory.search` | `memory` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `memory.context` | `memory` | `{}` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `observation.created` | `observation` | `{ generationJobId, sourceType, sourceId, provider, model, sourceAdapter, parsedObservationIndex }` | `processGeneratedResponse.ts` | Implemented (upstream) |
| `generation_job.processing` | `generation_job` | `{ sourceType, sourceId, sourceAdapter, attempt, correlationId, requestId }` | `ProviderObservationGenerator.ts` | Implemented (upstream) |
| `generation_job.completed` | `generation_job` | `{ generationJobId, provider, model, observationCount, observationIds, sourceAdapter }` | `processGeneratedResponse.ts` | Implemented (upstream) |
| `generation_job.scope_violation` | `generation_job` | `{ reason, message, payloadTeamId, payloadProjectId, canonicalTeamId, canonicalProjectId, sourceAdapter, correlationId }` | `ProviderObservationGenerator.ts` | Implemented (upstream) |
| `generation_job.revoked_key` | `generation_job` | `{ reason, message, sourceAdapter, correlationId }` | `ProviderObservationGenerator.ts` | Implemented (upstream) |
| `generation_job.stalled` | `generation_job` | `{ lane, bullmqJobId }` | `ActiveServerBetaGenerationWorkerManager.ts` | Implemented (upstream) |
| `generation_job.retried_by_operator` | `generation_job` | `{ outcome?, currentAttempts?, previousStatus?, currentStatus?, retriedCount?, requestId? }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `generation_job.cancelled_by_operator` | `generation_job` | `{ outcome?, previousStatus?, currentStatus?, requestId? }` | `ServerV1PostgresRoutes.ts` | Implemented (upstream) |
| `team.create` | `team` | `{}` | Admin API (`CreateTeamHandler.cs`) | Implemented (admin) |
| `project.create` | `project` | `{}` | Admin API (`CreateProjectHandler.cs`) | Implemented (admin) |
| `api_key.create` | `api_key` | `{}` | Admin API (`CreateApiKeyHandler.cs`) | Implemented (admin) |
| `api_key.revoke` | `api_key` | `{}` | Admin API (`DeleteApiKeyHandler.cs`) | Implemented (admin) |

---

## Actor Semantics

**`actor_id`** identifies who initiated the event:
- For upstream requests authenticated with an API key, `actor_id` is the `actor_id` column of the `api_keys` row (set at key creation time — typically a user or service identifier).
- For Admin API management operations, `actor_id` defaults to `"admin-api"` unless a more specific identity is available. For `api_key.create`, the `actor_id` of the newly created key is used instead so the key owner is recorded.
- For system-generated events (e.g. `generation_job.*`), `actor_id` may be `NULL`.

**`api_key_id`** identifies the API key row that was used to authenticate the request:
- For upstream events, this is the `id` of the `api_keys` row whose `key_hash` matched the bearer token.
- For Admin API events, this is `NULL` (the admin API uses its own separate authentication scheme, not an `api_keys` row).
- For `api_key.create` and `api_key.revoke` written by the Admin API, `api_key_id` is set to the ID of the affected key (the resource, not the caller's key).

---

## Indexes

Existing index (from original schema):
- `idx_audit_log_scope_created` on `(project_id, team_id, created_at)` — upstream queries by tenant scope

Indexes added by this admin feature:
- `idx_audit_log_created` on `(created_at DESC, id DESC)` — default listing order with efficient pagination
- `idx_audit_log_actor` on `(actor_id, created_at DESC)` — filter by actor
- `idx_audit_log_api_key` on `(api_key_id, created_at DESC)` — filter by API key
- `idx_audit_log_action` on `(action, created_at DESC)` — filter by action
- `idx_audit_log_resource_type` on `(resource_type, created_at DESC)` — filter by resource type

---

## Admin API Additions

This feature adds a read-only `GET /audit-log` endpoint to the Admin API that allows operators to:

- Browse all audit log entries across all tenants with cursor-based pagination
- Filter by `team_id`, `project_id`, `api_key_id`, `actor_id`, `action`, `resource_type`
- Filter by time range (`from` / `to`)

The endpoint joins `teams` and `projects` to resolve human-readable `team_name` and `project_name` alongside the raw IDs.

Additionally, four management handlers (`CreateTeamHandler`, `CreateProjectHandler`, `CreateApiKeyHandler`, `DeleteApiKeyHandler`) are extended to write audit entries after each successful operation via `IAuditLogAccess.WriteEntry`. Write failures are swallowed so an audit subsystem issue never fails a management operation.
