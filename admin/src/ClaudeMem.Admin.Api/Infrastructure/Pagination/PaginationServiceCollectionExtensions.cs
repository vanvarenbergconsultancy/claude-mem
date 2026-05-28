using ClaudeMem.Admin.Api.Infrastructure.Pagination.Encrypted;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Hmac;
using ClaudeMem.Admin.Api.Infrastructure.Pagination.Plain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public static class PaginationServiceCollectionExtensions
{
    public static IServiceCollection AddPagination(this IServiceCollection services, CursorEncodingStrategy strategy, IConfiguration configuration)
    {
        AddCorePaginationServices(services);

        switch (strategy)
        {
            case CursorEncodingStrategy.Plain:
                AddPlainCursorEncoder(services);
                break;
            case CursorEncodingStrategy.Hmac:
                AddHmacCursorEncoder(services, configuration);
                break;
            case CursorEncodingStrategy.Encrypted:
                AddEncryptedCursorEncoder(services, configuration);
                break;
        }

        return services;
    }

    public static void AddCorePaginationServices(IServiceCollection services)
    {
        services.AddSingleton<PaginationLinker>();
        services.AddExceptionHandler<InvalidCursorExceptionHandler>();
    }

    public static void AddPlainCursorEncoder(IServiceCollection services)
    {
        services.AddSingleton<ICursorEncoder, PlainCursorEncoder>();
    }

    public static void AddHmacCursorEncoder(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HmacCursorEncoderOptions>()
            .Bind(configuration.GetSection("Pagination:Hmac"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICursorEncoder, HmacCursorEncoder>();
    }

    public static void AddEncryptedCursorEncoder(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EncryptedCursorEncoderOptions>()
            .Bind(configuration.GetSection("Pagination:Encrypted"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICursorEncoder, EncryptedCursorEncoder>();
    }
}
