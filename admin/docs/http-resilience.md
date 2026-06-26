# HTTP Resilience

Engineering decisions and open questions for the HTTP resilience strategy in the Blazor admin UI.

---

## Current implementation

`Microsoft.Extensions.Http.Resilience` (the official Polly v8 wrapper for .NET) is registered globally in `src/ClaudeMem.Admin.Ui/Program.cs` via `ConfigureHttpClientDefaults`:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.Retry.DisableForUnsafeHttpMethods();
    });
});
```

This applies to all five typed clients (`ITeamsClient`, `IProjectsClient`, `IApiKeysClient`, `IObservationsClient`, `IJobsClient`) through `AddAdminApiClients(...)` without requiring each registration to be touched.

### What `AddStandardResilienceHandler` provides by default

| Strategy | Default |
|---|---|
| Rate limiter | 1 000 concurrent requests |
| Total request timeout | 30 s (across all retries) |
| Retry | 3 attempts, exponential backoff, jitter |
| Circuit breaker | 10 % failure ratio, min 100 requests, 5 s break |
| Per-attempt timeout | 10 s |

Transient errors that trigger retry: HTTP 5xx, 408 Request Timeout, 429 Too Many Requests, `HttpRequestException`, `TimeoutRejectedException`.

### Why `DisableForUnsafeHttpMethods()` is applied

The default retry retries **all** HTTP methods. This is dangerous for non-idempotent methods.

**The core race condition for POST:**

```
T0:  Client sends POST /teams (create team)
T5:  Server begins processing, starts INSERT
T10: Client per-attempt timeout fires → connection cancelled client-side
T10: Server continues executing (RequestAborted is set, but Abort() is NOT called automatically)
T11: Server commits the INSERT — team now exists in the database
T12: Client's retry POST arrives — server creates a SECOND team
T14: Client gets a response for the retry, user has two teams
```

Per [ASP.NET Core Request Timeouts documentation](https://learn.microsoft.com/en-us/aspnet/core/performance/timeouts):

> "When a timeout limit is hit, a `CancellationToken` in `HttpContext.RequestAborted` has `IsCancellationRequested` set to true. **Abort() isn't automatically called on the request, so the application may still produce a success or failure response.**"

Per [Microsoft HTTP resilience documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience):

> "For example, if a POST request inserts a new record to a database, then making retries for such a request could lead to data duplication."

`DisableForUnsafeHttpMethods()` disables retry for POST, PATCH, PUT, DELETE, and CONNECT — defined as "unsafe" in RFC 9110 (methods with side effects).

### Note on DELETE and PUT

RFC 9110 defines DELETE and PUT as *idempotent* (repeating them has the same effect as calling them once). `DisableForUnsafeHttpMethods()` disables them anyway because the method uses the RFC "safe methods" taxonomy rather than the "idempotent" taxonomy.

For this application:
- No PUT operations exist
- The only DELETE is API key revocation — retrying a revocation that already succeeded returns 404, which is acceptable

The conservative choice (disable all unsafe methods) avoids the overhead of a custom `ShouldHandle` predicate and matches the standard recommended pattern.

---

## Open: idempotency for POST (future work)

The right long-term solution for POST retry safety is **idempotency keys** — a client-supplied UUID that the server uses to detect and deduplicate repeated requests.

### How it works

1. Client generates a UUID before making a POST request
2. Client includes it as a request header: `Idempotency-Key: <uuid>`
3. Server checks whether this key was already processed:
   - If yes: return the cached response (HTTP 200 with the original result)
   - If no: process the request, store the key + result, return HTTP 201
4. Client can safely retry using the same UUID — duplicates are impossible

This is how Stripe, PayPal, and Adyen handle POST retries. It is described in [draft-ietf-httpapi-idempotency-key-header](https://datatracker.ietf.org/doc/html/draft-ietf-httpapi-idempotency-key-header-07).

### What needs to happen

**API side:**
- Accept an `Idempotency-Key` header on all POST endpoints
- Store processed keys + serialised responses (short TTL, e.g. 24 h)
- Return cached response on duplicate key

**Client side:**
- Generate a UUID per logical user action (not per HTTP attempt)
- Pass the same UUID on retries via `DelegatingHandler` or Polly's `OnRetry` callback

**Polly side:**
- Remove `DisableForUnsafeHttpMethods()` once idempotency keys are implemented
- POST retries are then safe

### Current status

API guidelines covering idempotency have been written but not yet implemented. This is a separate plan/design discussion.

Until idempotency keys are in place, `DisableForUnsafeHttpMethods()` remains the correct default.

---

## Known issues

**`DisableForUnsafeHttpMethods()` reported bug ([dotnet/extensions #6708](https://github.com/dotnet/extensions/issues/6708)):** POST requests have been reported as still being retried despite calling this method in some versions of the library. Verify by observing network traffic or server logs when a POST endpoint returns 500 — it should not retry. If the bug is present in the installed version, a custom `ShouldHandle` predicate is the fallback:

```csharp
options.Retry.ShouldHandle = static args =>
{
    if (args.Outcome.Result?.RequestMessage?.Method == HttpMethod.Post ||
        args.Outcome.Result?.RequestMessage?.Method == HttpMethod.Patch)
    {
        return ValueTask.FromResult(false);
    }
    return HttpClientResiliencePredicates.IsTransient(args.Outcome);
};
```

---

## References

- [Build resilient HTTP apps: Key development patterns — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [HttpRetryStrategyOptionsExtensions.DisableForUnsafeHttpMethods — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.http.resilience.httpretrystrategyoptionsextensions.disableforunsafehttpmethods)
- [Request timeouts middleware in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/timeouts)
- [RFC 9110 — HTTP Semantics](https://datatracker.ietf.org/doc/html/rfc9110)
- [The Idempotency-Key HTTP Header Field (IETF draft)](https://datatracker.ietf.org/doc/html/draft-ietf-httpapi-idempotency-key-header-07)
- [dotnet/extensions #5248 — Add DisableForUnsafeHttpMethods](https://github.com/dotnet/extensions/issues/5248)
- [dotnet/extensions #6708 — DisableForUnsafeHttpMethods may not work](https://github.com/dotnet/extensions/issues/6708)
