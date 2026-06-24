# Entity Framework Core Cursor Store

How to implement `ICursorStore` using Entity Framework Core when this project is extracted into a reusable library.

This project does not take a dependency on `Microsoft.EntityFrameworkCore`. Add it to the target library before following these steps.

## Step 1 — CursorToken entity

```csharp
public sealed class CursorToken
{
    public required string Id { get; set; }        // UUID — the opaque token given to clients
    public required string Payload { get; set; }   // JSON-serialised CursorPayload
    public required DateTimeOffset ExpiresAt { get; set; }
}
```

## Step 2 — Configure in OnModelCreating

```csharp
modelBuilder.Entity<CursorToken>(b =>
{
    b.HasKey(t => t.Id);
    b.Property(t => t.Id).HasMaxLength(32);
    b.Property(t => t.Payload).IsRequired();
    b.HasIndex(t => t.ExpiresAt); // enables efficient cleanup queries
});
```

## Step 3 — Add a migration

```bash
dotnet ef migrations add AddCursorTokens
dotnet ef database update
```

## Step 4 — Implement ICursorStore

```csharp
internal sealed class EntityFrameworkCursorStore<TContext> : ICursorStore
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _factory;
    private readonly ServerStoredCursorOptions _options;

    public EntityFrameworkCursorStore(IDbContextFactory<TContext> factory, ServerStoredCursorOptions options)
    {
        _factory = factory;
        _options = options;
    }

    public async Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
    {
        var opaqueToken = Guid.NewGuid().ToString("N");
        var entry = new CursorToken
        {
            Id = opaqueToken,
            Payload = JsonSerializer.Serialize(payload),
            ExpiresAt = DateTimeOffset.UtcNow.Add(_options.TokenExpiry),
        };

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Set<CursorToken>().Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return opaqueToken;
    }

    public async Task<CursorPayload?> Retrieve(string opaqueToken, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var entry = await db.Set<CursorToken>()
            .Where(t => t.Id == opaqueToken && t.ExpiresAt > DateTimeOffset.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        return entry is null ? null : JsonSerializer.Deserialize<CursorPayload>(entry.Payload);
    }
}
```

## Step 5 — Cleanup expired tokens (run periodically)

```csharp
await using var db = await factory.CreateDbContextAsync(cancellationToken);
await db.Set<CursorToken>()
    .Where(t => t.ExpiresAt <= DateTimeOffset.UtcNow)
    .ExecuteDeleteAsync(cancellationToken);
```

## Step 6 — Register

```csharp
services.AddServerStoredCursorPagination();
services.AddSingleton(new ServerStoredCursorOptions { TokenExpiry = TimeSpan.FromDays(3) });
services.AddSingleton<ICursorStore, EntityFrameworkCursorStore<YourDbContext>>();
```
