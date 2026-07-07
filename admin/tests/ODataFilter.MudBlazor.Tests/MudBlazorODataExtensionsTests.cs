using System.Collections.Generic;
using AwesomeAssertions;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace ODataFilter.MudBlazor.Tests;

public sealed class MudBlazorODataExtensionsTests
{
    [Fact]
    public void ToODataFilter_EmptyList_ReturnsNull()
    {
        var result = new List<IFilterDefinition<TestEntity>>().ToODataFilter<TestEntity>();

        result.Should().BeNull();
    }

    [Fact]
    public void ToODataFilter_ContainsOperator_EmitsContainsFunction()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.Contains, "acme")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("contains(name,'acme')");
    }

    [Fact]
    public void ToODataFilter_NotContainsOperator_EmitsNotContains()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.NotContains, "spam")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("not contains(name,'spam')");
    }

    [Fact]
    public void ToODataFilter_EqualsOperator_EmitsEqClause()
    {
        var filters = new[]
        {
            MakeFilter("status", FilterOperator.String.Equal, "active")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("status eq 'active'");
    }

    [Fact]
    public void ToODataFilter_NotEqualsOperator_EmitsNeClause()
    {
        var filters = new[]
        {
            MakeFilter("status", FilterOperator.String.NotEqual, "deleted")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("status ne 'deleted'");
    }

    [Fact]
    public void ToODataFilter_StartsWithOperator_EmitsStartswith()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.StartsWith, "Ac")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("startswith(name,'Ac')");
    }

    [Fact]
    public void ToODataFilter_EndsWithOperator_EmitsEndswith()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.EndsWith, "me")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("endswith(name,'me')");
    }

    [Fact]
    public void ToODataFilter_IsEmptyOperator_EmitsEqNull()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.Empty, null)
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("name eq null");
    }

    [Fact]
    public void ToODataFilter_IsNotEmptyOperator_EmitsNeNull()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.NotEmpty, null)
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("name ne null");
    }

    [Fact]
    public void ToODataFilter_NumericGreaterThan_EmitsGtClause()
    {
        var filters = new[]
        {
            MakeFilter("count", FilterOperator.Number.GreaterThan, 5)
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("count gt 5");
    }

    [Fact]
    public void ToODataFilter_NumericLessThanOrEqual_EmitsLeClause()
    {
        var filters = new[]
        {
            MakeFilter("count", FilterOperator.Number.LessThanOrEqual, 100)
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("count le 100");
    }

    [Fact]
    public void ToODataFilter_MultipleFilters_CombinesWithAnd()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.Contains, "acme"),
            MakeFilter("active", FilterOperator.Boolean.Is, true)
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("contains(name,'acme') and active eq true");
    }

    [Fact]
    public void ToODataFilter_FilterWithNullValue_IsExcluded()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.Equal, null),
            MakeFilter("status", FilterOperator.String.Equal, "active")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("status eq 'active'");
    }

    [Fact]
    public void ToODataFilter_StringWithSingleQuote_IsEscaped()
    {
        var filters = new[]
        {
            MakeFilter("name", FilterOperator.String.Equal, "O'Brien")
        };

        var result = filters.ToODataFilter<TestEntity>();

        result.Should().Be("name eq 'O''Brien'");
    }

    [Fact]
    public void ToODataOrderBy_EmptyList_ReturnsNull()
    {
        var result = new List<SortDefinition<TestEntity>>().ToODataOrderBy();

        result.Should().BeNull();
    }

    [Fact]
    public void ToODataOrderBy_SingleAscSort_EmitsAsc()
    {
        var sorts = new[] { new SortDefinition<TestEntity>("name", false, 0, null!, null) };

        var result = sorts.ToODataOrderBy();

        result.Should().Be("name asc");
    }

    [Fact]
    public void ToODataOrderBy_SingleDescSort_EmitsDesc()
    {
        var sorts = new[] { new SortDefinition<TestEntity>("createdAt", true, 0, null!, null) };

        var result = sorts.ToODataOrderBy();

        result.Should().Be("createdAt desc");
    }

    [Fact]
    public void ToODataOrderBy_MultipleSorts_EmitsCommaSeparated()
    {
        var sorts = new[]
        {
            new SortDefinition<TestEntity>("name", false, 0, null!, null),
            new SortDefinition<TestEntity>("createdAt", true, 1, null!, null)
        };

        var result = sorts.ToODataOrderBy();

        result.Should().Be("name asc, createdAt desc");
    }

    private static IFilterDefinition<TestEntity> MakeFilter(string field, string op, object? value)
    {
        var fd = Substitute.For<IFilterDefinition<TestEntity>>();
        fd.Column.Returns(default(Column<TestEntity>));
        fd.Title.Returns(field);
        fd.Operator.Returns(op);
        fd.Value.Returns(value);
        return fd;
    }

    public sealed class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int Count { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
