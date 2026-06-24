using ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>Extension methods for registering cursor-based pagination services.</summary>
public static class PaginationServiceCollectionExtensions
{
    /// <summary>Registers unprotected (plain base-64) cursor pagination — suitable for non-sensitive, trusted environments only.</summary>
    public static IServiceCollection AddPlainCursorPagination(this IServiceCollection services)
    {
        return
            services.AddCorePaginationServices()
            .AddSingleton<ICursorCodec, PlainCursorCodec>();
    }

    /// <summary>Registers HMAC-SHA-256 signed cursor pagination, preventing clients from forging or tampering with cursors.</summary>
    public static IServiceCollection AddSignedCursorPagination(this IServiceCollection services, SignedCursorOptions options)
    {
        return services
            .AddCorePaginationServices()
            .AddSingleton<ICursorCodec>(_ => new SignedCursorCodec(options.SigningKey));
    }

    /// <summary>Registers ASP.NET Core Data Protection encrypted cursor pagination, fully opaque to clients.</summary>
    public static IServiceCollection AddEncryptedCursorPagination(this IServiceCollection services)
    {
        services
            .AddCorePaginationServices()
            .AddSingleton<ICursorCodec, EncryptedCursorCodec>();

        services.AddDataProtection();

        return services;
    }

    /// <summary>
    /// Registers the server-stored cursor codec.
    /// The caller must also register an <see cref="ICursorStore"/> implementation using one of:
    /// <see cref="AddInMemoryCursorStore"/>
    /// <see cref="AddMemoryCacheCursorStore"/>,
    /// <see cref="AddDistributedCacheCursorStore"/>
    /// or a custom implementation of <see cref="ICursorStore"/>.
    /// </summary>
    public static IServiceCollection AddServerStoredCursorPagination(this IServiceCollection services)
    {
        return services
            .AddCorePaginationServices()
            .AddSingleton<ICursorCodec, ServerStoredCursorCodec>();
    }

    /// <summary>Registers the in-process memory cursor store. Cursors are lost on restart; suitable for development or single-instance deployments.</summary>
    public static IServiceCollection AddInMemoryCursorStore(this IServiceCollection services, ServerStoredCursorOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<InMemoryCursorStore>();
        services.AddSingleton<ICursorStore>(sp => sp.GetRequiredService<InMemoryCursorStore>());
        services.AddSingleton<ISupportsCursorPurge>(sp => sp.GetRequiredService<InMemoryCursorStore>());

        if (options.EnableAutoCleanup)
        {
            services.AddHostedService<CursorStoreCleanupService>();
        }

        return services;
    }

    /// <summary>Registers the in-process memory-cache cursor store. Suitable for single-instance deployments; eviction is handled by the cache.</summary>
    public static IServiceCollection AddMemoryCacheCursorStore(this IServiceCollection services, ServerStoredCursorOptions options)
    {
        return services
            .AddMemoryCache()
            .AddSingleton(options)
            .AddSingleton<ICursorStore, MemoryCacheCursorStore>();
    }

    /// <summary>
    /// Registers the distributed cache cursor store. The caller is responsible for
    /// registering an <see cref="IDistributedCache"/> implementation (e.g.
    /// <c>AddStackExchangeRedisCache</c> or <c>AddDistributedMemoryCache</c>).
    /// </summary>
    public static IServiceCollection AddDistributedCacheCursorStore(this IServiceCollection services, ServerStoredCursorOptions options)
    {
        return services
            .AddSingleton(options)
            .AddSingleton<ICursorStore, DistributedCacheCursorStore>();
    }

    private static IServiceCollection AddCorePaginationServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<PaginationLinker>()
            .AddExceptionHandler<InvalidCursorExceptionHandler>();
    }
}
