using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Infrastructure;

[CollectionDefinition(Name)]
#pragma warning disable CA1711
public sealed class AdminApiCollection : ICollectionFixture<AdminApiFixture>
#pragma warning restore CA1711
{
    public const string Name = "AdminApi";
}
