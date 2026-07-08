using Microsoft.Extensions.DependencyInjection;
using SqlKata.Compilers;

namespace ODataFilter.Core;

/// <summary>DI registration extensions for ODataFilter.Core.</summary>
public static class ODataFilterServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IODataSqlTranslator"/> as a singleton using the provided <paramref name="sqlCompiler"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sqlCompiler">Kata compiler for the target database (e.g. <c>new PostgresCompiler()</c>). </param>
    public static IServiceCollection AddODataSqlTranslator(this IServiceCollection services, Compiler sqlCompiler)
    {
        return services.AddSingleton<IODataSqlTranslator>(new ODataSqlTranslator(sqlCompiler));
    }
}
