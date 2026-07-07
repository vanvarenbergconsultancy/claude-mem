
using System.Collections.Generic;
using System.Linq;

using AwesomeAssertions;
using AwesomeAssertions.Execution;

using Gridify;

using ODataFilter.Core;
using Xunit;

namespace ODataFilter.EfCore.Tests;
public sealed class ODataFilterQueryableExtensionsTests
{
    private static readonly IQueryable<Product> Products = new List<Product>
    {
        new() { Id = 1, Name = "Acme Widget", Price = 9.99m, Active = true },
        new() { Id = 2, Name = "Beta Gadget", Price = 49.99m, Active = true },
        new() { Id = 3, Name = "Gamma Device", Price = 149.99m, Active = false },
        new() { Id = 4, Name = "Acme Pro", Price = 199.99m, Active = true }
    }.AsQueryable();

    [Fact]
    public void ApplyODataFilter_ContainsFilter_ReturnsMatchingRows()
    {
        var result = Products.ApplyODataFilter<Product>("contains(Name,'Acme')").ToList();

        using var scope = new AssertionScope();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.Name.Should().Contain("Acme"));
    }

    [Fact]
    public void ApplyODataFilter_EqFilter_ReturnsSingleRow()
    {
        var result = Products.ApplyODataFilter<Product>("Id eq 1").ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void ApplyODataFilter_AndFilter_AppliesBothConditions()
    {
        var result = Products.ApplyODataFilter<Product>("Active eq true and Price gt 100").ToList();

        using var scope = new AssertionScope();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Acme Pro");
    }

    [Fact]
    public void ApplyODataFilter_OrFilter_ReturnsEitherCondition()
    {
        var result = Products.ApplyODataFilter<Product>("Id eq 1 or Id eq 3").ToList();

        using var scope = new AssertionScope();
        result.Should().HaveCount(2);
        result.Select(p => p.Id).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public void ApplyODataFilter_NullFilter_ReturnsSameSource()
    {
        var result = Products.ApplyODataFilter<Product>(null).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ApplyODataFilter_EmptyFilter_ReturnsSameSource()
    {
        var result = Products.ApplyODataFilter<Product>("").ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ApplyODataOrderBy_AscOrder_ReturnsSortedRows()
    {
        var result = Products.ApplyODataOrderBy<Product>("Name asc").ToList();

        result[0].Name.Should().Be("Acme Pro");
        result[1].Name.Should().Be("Acme Widget");
    }

    [Fact]
    public void ApplyODataOrderBy_DescOrder_ReturnsSortedRowsDescending()
    {
        var result = Products.ApplyODataOrderBy<Product>("Price desc").ToList();

        result[0].Price.Should().Be(199.99m);
    }

    [Fact]
    public void ApplyODataOptions_FilterAndSort_AppliesBoth()
    {
        var options = new ODataQueryOptions
        {
            Filter = "Active eq true",
            OrderBy = "Price desc"
        };

        var result = Products.ApplyODataOptions(options).ToList();

        using var scope = new AssertionScope();
        result.Should().HaveCount(3);
        result[0].Price.Should().Be(199.99m);
    }

    [Fact]
    public void ApplyODataOptions_WithMapper_UsesFieldAlias()
    {
        var mapper = new GridifyMapper<Product>().AddMap("cost", p => p.Price);
        var options = new ODataQueryOptions { Filter = "cost gt 100" };

        var result = Products.ApplyODataOptions(options, mapper).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyODataOptions_NullOptions_ReturnsSameSource()
    {
        var result = Products.ApplyODataOptions<Product>(null).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ApplyODataOptions_EmptyOptions_ReturnsSameSource()
    {
        var result = Products.ApplyODataOptions(ODataQueryOptions.Empty).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ApplyODataOptions_TopOption_LimitsRows()
    {
        var options = new ODataQueryOptions { Top = 2 };

        var result = Products.ApplyODataOptions(options).ToList();

        result.Should().HaveCount(2);
    }

    public sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Active { get; set; }
    }
}
