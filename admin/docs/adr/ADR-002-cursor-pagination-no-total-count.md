# ADR-002: Cursor Pagination with No Total Count

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

List endpoints need pagination. The two common models are:

1. **Offset/limit with total count** — returns `{ items, total, page, pageSize }`. Enables page-number UX ("Page 3 of 47").
2. **Cursor-based** — returns `{ items, nextCursor, hasMore }`. Enables "Load more" / infinite scroll UX.

The admin project already uses cursor-based pagination (encrypted cursor encoding the last-seen primary key).

## Decision

Cursor-based pagination only. The response never includes `totalCount` or equivalent. Clients only receive `nextCursor` and `hasMore`. UX is "Load more" / infinite scroll — no page numbers, no "Page X of Y".

## Consequences

**Positive:**
- No `COUNT(*)` full table scan on every paginated request (the dominant cost at scale).
- Result is not stale between pages (new rows don't shift page boundaries).
- Stable under concurrent inserts/deletes — items don't disappear from or repeat between pages.

**Negative:**
- Cannot jump to an arbitrary page ("go to page 5").
- Cannot display "showing X–Y of Z results".
- Deep links to a specific page are not possible without re-paginating from the beginning.

These are acceptable trade-offs for an internal admin UI built on "Load more" scrolling.

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Offset/limit with `COUNT(*)` | Full table scan on every request; count is stale by the time the response arrives |
| Offset/limit with approximate count (`reltuples`) | Misleading contract; approximate counts confuse users and downstream consumers |
| Keyset pagination with optional total count | Still requires COUNT(*); the cost exists whether the count is "optional" or not |
