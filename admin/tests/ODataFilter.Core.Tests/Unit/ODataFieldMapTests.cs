
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Xunit;

namespace ODataFilter.Core.Tests.Unit;
public sealed class ODataFieldMapTests
{
    [Fact]
    public void Apply_RenamesFieldInFilter()
    {
        var map = ODataFieldMap.Create()
            .Map("customerId", "client_identifier")
            .Build();

        var options = new ODataQueryOptions { Filter = "customerId eq 'abc'" };
        var result = map.Apply(options);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be("client_identifier eq 'abc'");
    }

    [Fact]
    public void Apply_RenamesFieldInOrderBy()
    {
        var map = ODataFieldMap.Create()
            .Map("displayName", "full_name")
            .Build();

        var options = new ODataQueryOptions { OrderBy = "displayName asc" };
        var result = map.Apply(options);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.OrderBy.Should().Be("full_name asc");
    }

    [Fact]
    public void Apply_AppliesMultipleRenamesInOneFilter()
    {
        var map = ODataFieldMap.Create()
            .Map("customerId", "client_identifier")
            .Map("teamName", "team_name")
            .Build();

        var options = new ODataQueryOptions { Filter = "customerId eq 'abc' and teamName eq 'x'" };
        var result = map.Apply(options);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Contain("client_identifier");
        result.Filter.Should().Contain("team_name");
        result.Filter.Should().NotContain("customerId");
        result.Filter.Should().NotContain("teamName");
    }

    [Fact]
    public void Apply_UnknownFieldsAreLeftUnchanged()
    {
        var map = ODataFieldMap.Create()
            .Map("foo", "bar")
            .Build();

        var options = new ODataQueryOptions { Filter = "name eq 'Acme'" };
        var result = map.Apply(options);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be("name eq 'Acme'");
    }

    [Fact]
    public void Apply_DoesNotReplacePartialTokens()
    {
        var map = ODataFieldMap.Create()
            .Map("id", "identifier")
            .Build();

        var options = new ODataQueryOptions { Filter = "clientId eq 'x'" };
        var result = map.Apply(options);

        // "id" inside "clientId" must NOT be replaced
        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be("clientId eq 'x'");
    }

    [Fact]
    public void Apply_FieldNameMatchingStringConstant_IsNotReplacedInValue()
    {
        var map = ODataFieldMap.Create()
            .Map("name", "full_name")
            .Build();

        var options = new ODataQueryOptions { Filter = "type eq 'name'" };
        var result = map.Apply(options);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be("type eq 'name'");
    }

    [Fact]
    public void Apply_NullFilterAndOrderBy_ReturnsUnchanged()
    {
        var map = ODataFieldMap.Create()
            .Map("foo", "bar")
            .Build();

        var result = map.Apply(ODataQueryOptions.Empty);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().BeNull();
        result.OrderBy.Should().BeNull();
    }
}
