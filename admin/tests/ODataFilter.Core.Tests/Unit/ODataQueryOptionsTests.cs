
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Xunit;

namespace ODataFilter.Core.Tests.Unit;
public sealed class ODataQueryOptionsTests
{
    [Fact]
    public void Empty_HasNoFilterOrSort()
    {
        var sut = ODataQueryOptions.Empty;

        using var scope = new AssertionScope();
        sut.Should().NotBeNull();
        sut.Filter.Should().BeNull();
        sut.OrderBy.Should().BeNull();
        sut.Top.Should().BeNull();
    }

    [Fact]
    public void AndFilter_WhenCurrentFilterIsNull_ReturnsAdditionalFilter()
    {
        const string tenantFilter = "tenantId eq 'abc'";
        var sut = ODataQueryOptions.Empty;

        var result = sut.AndFilter(tenantFilter);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be(tenantFilter);
    }

    [Fact]
    public void AndFilter_WhenCurrentFilterExists_CombinesWithAnd()
    {
        const string nameFilter = "name eq 'Acme'";
        const string tenantFilter = "tenantId eq 'abc'";
        var sut = new ODataQueryOptions { Filter = nameFilter };
        
        var result = sut.AndFilter(tenantFilter);

        using var scope = new AssertionScope();
        result.Should().NotBeNull();
        result.Filter.Should().Be($"({nameFilter}) and ({tenantFilter})");
    }

    [Fact]
    public void AndFilter_WithNullOrWhitespace_ReturnsOriginal()
    {
        const string nameFilter = "name eq 'Acme'";
        var sut = new ODataQueryOptions { Filter = nameFilter };

        var resultNull = sut.AndFilter(null!);
        var resultEmpty = sut.AndFilter("  ");

        using var scope = new AssertionScope();
        resultNull.Should().NotBeNull();
        resultNull.Filter.Should().Be(nameFilter);

        resultEmpty.Should().NotBeNull();
        resultEmpty.Filter.Should().Be(nameFilter);
    }

    [Fact]
    public void AndFilter_DoesNotMutateOriginalInstance()
    {
        const string nameFilter = "name eq 'Acme'";
        const string tenantFilter = "tenantId eq 'abc'";
        var sut = new ODataQueryOptions { Filter = nameFilter };

        _ = sut.AndFilter(tenantFilter);

        using var scope = new AssertionScope();
        sut.Filter.Should().Be(nameFilter);
        sut.Filter.Should().NotContain(tenantFilter);
    }
}
