// This file documents how to implement ICursorStore using Entity Framework Core.
// It is intentionally NOT compiled (no using directives for EF) because this project
// does not take a dependency on Microsoft.EntityFrameworkCore.
//
// When extracting cursor pagination into a reusable library, add
// Microsoft.EntityFrameworkCore to that library and implement this class there.
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 1: Add a CursorToken entity to your DbContext
// ─────────────────────────────────────────────────────────────────────────────
//
// public sealed class CursorToken
// {
//     public required string Id { get; set; }        // UUID — the opaque token given to clients
//     public required string Payload { get; set; }   // JSON-serialised CursorPayload
//     public required DateTimeOffset ExpiresAt { get; set; }
// }
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 2: Configure in OnModelCreating
// ─────────────────────────────────────────────────────────────────────────────
//
// modelBuilder.Entity<CursorToken>(b =>
// {
//     b.HasKey(t => t.Id);
//     b.Property(t => t.Id).HasMaxLength(32);
//     b.Property(t => t.Payload).IsRequired();
//     // Index on ExpiresAt enables efficient cleanup queries.
//     b.HasIndex(t => t.ExpiresAt);
// });
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 3: Add a migration
// ─────────────────────────────────────────────────────────────────────────────
//
// dotnet ef migrations add AddCursorTokens
// dotnet ef database update
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 4: Implement ICursorStore
// ─────────────────────────────────────────────────────────────────────────────
//
// internal sealed class EntityFrameworkCursorStore<TContext> : ICursorStore
//     where TContext : DbContext
// {
//     private readonly IDbContextFactory<TContext> _factory;
//     private readonly ServerStoredCursorOptions _options;
//
//     public EntityFrameworkCursorStore(IDbContextFactory<TContext> factory, ServerStoredCursorOptions options)
//     {
//         _factory = factory;
//         _options = options;
//     }
//
//     public async Task<string> Store(CursorPayload payload, CancellationToken cancellationToken = default)
//     {
//         var token = Guid.NewGuid().ToString("N");
//         var entry = new CursorToken
//         {
//             Id = token,
//             Payload = JsonSerializer.Serialize(payload),
//             ExpiresAt = DateTimeOffset.UtcNow.Add(_options.TokenExpiry),
//         };
//
//         await using var db = await _factory.CreateDbContextAsync(cancellationToken);
//         db.Set<CursorToken>().Add(entry);
//         await db.SaveChangesAsync(cancellationToken);
//         return token;
//     }
//
//     public async Task<CursorPayload?> Retrieve(string token, CancellationToken cancellationToken = default)
//     {
//         await using var db = await _factory.CreateDbContextAsync(cancellationToken);
//         var entry = await db.Set<CursorToken>()
//             .Where(t => t.Id == token && t.ExpiresAt > DateTimeOffset.UtcNow)
//             .FirstOrDefaultAsync(cancellationToken);
//
//         return entry is null ? null : JsonSerializer.Deserialize<CursorPayload>(entry.Payload);
//     }
// }
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 5: Cleanup expired tokens (run periodically, e.g. via a background service)
// ─────────────────────────────────────────────────────────────────────────────
//
// await using var db = await factory.CreateDbContextAsync(cancellationToken);
// await db.Set<CursorToken>()
//     .Where(t => t.ExpiresAt <= DateTimeOffset.UtcNow)
//     .ExecuteDeleteAsync(cancellationToken);
//
// ─────────────────────────────────────────────────────────────────────────────
// STEP 6: Register (example)
// ─────────────────────────────────────────────────────────────────────────────
//
// services.AddServerStoredCursorPagination();
// services.AddSingleton(new ServerStoredCursorOptions { TokenExpiry = TimeSpan.FromDays(3) });
// services.AddSingleton<ICursorStore, EntityFrameworkCursorStore<YourDbContext>>();

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
