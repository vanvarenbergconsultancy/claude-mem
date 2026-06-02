using ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.ServerStored;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public static class PaginationServiceCollectionExtensions
{
    public static IServiceCollection AddPlainCursorPagination(this IServiceCollection services)
    {
        return 
            services.AddCorePaginationServices()
            .AddSingleton<ICursorCodec, PlainCursorCodec>();
    }

    public static IServiceCollection AddSignedCursorPagination(this IServiceCollection services, SignedCursorOptions options)
    {
        return services
            .AddCorePaginationServices()
            .AddSingleton<ICursorCodec>(_ => new SignedCursorCodec(options.SigningKey));
    }

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
    /// or a custom implementation of <see cref="ICursorStore"/>.
    /// </summary>
    public static IServiceCollection AddServerStoredCursorPagination(this IServiceCollection services)
    {
        return services
            .AddCorePaginationServices()
            .AddSingleton<ICursorCodec, ServerStoredCursorCodec>();
    }
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

    private static IServiceCollection AddCorePaginationServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<PaginationLinker>()
            .AddExceptionHandler<InvalidCursorExceptionHandler>();
    }
}
