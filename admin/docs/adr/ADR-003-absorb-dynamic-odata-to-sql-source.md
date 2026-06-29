# ADR-003: Absorb DynamicODataToSQL Source Instead of Taking a NuGet Reference

**Status:** Accepted
**Date:** 2026-06-29
**Deciders:** Xavier

## Context

`ODataFilter.Core` needs to translate OData `$filter` strings to parameterised SQL. The `DynamicODataToSQL` library (GitHub: `DynamicODataToSQL/DynamicODataToSQL`) does exactly this using Microsoft.OData.Core and SqlKata.

State of the upstream library at decision time:
- Last NuGet release: 2 years stale (v2.x, targets .NET Standard 2.1 + SqlKata 2.x)
- SqlKata 4.x (our dependency) has a breaking API change not reflected in the NuGet release
- 7 open GitHub issues that are fixable and affect our use cases (#14, #33, #41, #42, #46, #52, #58)
- Only 6 source files (≈800 lines total)

## Decision

Copy the 6 source files into `src/ODataFilter.Core/Internal/DynamicODataToSql/`, mark every type `internal`, add attribution headers (MIT license preserved), and own the code going forward. Apply all 7 fixes immediately.

The absorbed files live under `Internal/` and are not part of the public API. Consumers only interact with `IODataSqlTranslator` and `ODataQueryOptions`.

Fixes applied at absorption time:
- **#59** — SqlKata 4.x API compatibility
- **#58** — Full `_x[hex]_` decoding (not just space `_x0020_`)
- **#52** — Column-to-column comparisons (`id ne payorId`)
- **#46** — Preserve leading/trailing spaces in string filter values
- **#14** — Strip defensive single-quote wrapping from extracted column names
- **#42** — Catch `ODataException` from parse calls, rethrow as `ODataFilterParseException`
- **#41** — Document thread-safety; verify stateless per-call construction
- **#33** — Improved error message for invalid `$top` values

## Consequences

**Positive:**
- Targets `net10.0` only — `netstandard2.1` support was explicitly dropped. The `.csproj` inherits `<TargetFramework>net10.0</TargetFramework>` from `Directory.Build.props` and adds no multi-targeting.
- All 7 bugs fixed in our copy; no waiting for upstream.
- SqlKata 4.x works.
- Internal visibility means we can refactor freely without breaking public API.

**Negative:**
- We own the maintenance from here on (no upstream bug-fix inheritance).
- Upstream MIT attribution must be preserved in file headers.

## Alternatives Considered

| Alternative | Why Rejected |
|---|---|
| Take NuGet reference and wait for upstream | Stale; SqlKata 4.x breaks it; upstream is inactive |
| Build from scratch | Unnecessary — the hard part (OData AST walking) is already done in the absorbed source |
| Fork on GitHub and take a git submodule | Submodule adds complexity; we only need 6 files; full fork creates a separate repo to maintain |
