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

    private static IServiceCollection AddCorePaginationServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<PaginationLinker>()
            .AddExceptionHandler<InvalidCursorExceptionHandler>();
    }
}
