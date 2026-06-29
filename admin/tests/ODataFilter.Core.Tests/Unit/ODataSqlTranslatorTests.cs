namespace ODataFilter.Core.Tests.Unit;

using System;

using AwesomeAssertions;

using ODataFilter.Core;

using SqlKata.Compilers;

using Xunit;

public sealed class ODataSqlTranslatorTests
{
    private static readonly ODataSqlTranslator Sut = new(new PostgresCompiler());

    [Fact]
    public void Translate_NoOptions_EmitsSelectStar()
    {
        var result = Sut.Translate("teams", ODataQueryOptions.Empty);

        result.Sql.Should().Be("SELECT * FROM \"teams\"");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Translate_EqFilter_EmitsWhereClause()
    {
        var options = new ODataQueryOptions { Filter = "name eq 'Acme'" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("\"name\"");
        result.Parameters.Should().ContainValue("Acme");
    }

    [Fact]
    public void Translate_NeFilter_EmitsNotEqualClause()
    {
        var options = new ODataQueryOptions { Filter = "active ne false" };

        var result = Sut.Translate("teams", options);

        // SqlKata PostgresCompiler emits != (not <>)
        result.Sql.Should().Contain("!=");
    }

    [Fact]
    public void Translate_ContainsFilter_EmitsLike()
    {
        var options = new ODataQueryOptions { Filter = "contains(name,'acme')" };

        var result = Sut.Translate("teams", options);

        // caseSensitive=true (default) generates LIKE; use tolower() wrapper for ILIKE
        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_StartsWithFilter_EmitsPrefixLike()
    {
        var options = new ODataQueryOptions { Filter = "startswith(name,'ac')" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("like");
        result.Parameters.Values.Should().Contain(v => ((string)v).StartsWith("ac", StringComparison.Ordinal));
    }

    [Fact]
    public void Translate_EndsWithFilter_EmitsSuffixLike()
    {
        var options = new ODataQueryOptions { Filter = "endswith(name,'me')" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_GtFilter_EmitsGreaterThan()
    {
        var options = new ODataQueryOptions { Filter = "createdAt gt 2024-01-01T00:00:00Z" };

        var result = Sut.Translate("events", options);

        result.Sql.Should().Contain(">");
    }

    [Fact]
    public void Translate_AndFilter_EmitsAndClause()
    {
        var options = new ODataQueryOptions { Filter = "active eq true and name eq 'Acme'" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("AND");
        result.Parameters.Should().ContainSingle();
    }

    [Fact]
    public void Translate_OrFilter_EmitsOrClause()
    {
        var options = new ODataQueryOptions { Filter = "name eq 'Acme' or name eq 'Beta'" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("OR");
    }

    [Fact]
    public void Translate_OrderByAsc_EmitsOrderByAsc()
    {
        var options = new ODataQueryOptions { OrderBy = "name asc" };

        var result = Sut.Translate("teams", options);

        // PostgresCompiler omits ASC keyword (it is the default direction in SQL)
        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("\"name\"");
    }

    [Fact]
    public void Translate_OrderByDesc_EmitsOrderByDesc()
    {
        var options = new ODataQueryOptions { OrderBy = "createdAt desc" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("DESC");
    }

    [Fact]
    public void Translate_MultipleOrderBy_EmitsCommaSeparated()
    {
        var options = new ODataQueryOptions { OrderBy = "name asc, createdAt desc" };

        var result = Sut.Translate("teams", options);

        result.Sql.Should().Contain("\"name\"");
        result.Sql.Should().Contain("\"createdAt\"");
    }

    [Fact]
    public void Translate_TopOption_EmitsLimit()
    {
        var options = new ODataQueryOptions { Top = 10 };

        var result = Sut.Translate("teams", options);

        // SqlKata parameterizes LIMIT values (LIMIT @p0 with @p0=10)
        result.Sql.Should().Contain("LIMIT");
        result.Parameters.Values.Should().Contain(10);
    }

    [Fact]
    public void Translate_TopWithFilter_DoesNotThrow()
    {
        // int? Top goes through ToString() before reaching the OData parser so a
        // non-integer $top string is not reachable via the public API
        var action = () => Sut.Translate("teams", new ODataQueryOptions { Filter = "name eq 'x'", Top = 5 });
        action.Should().NotThrow();
    }

    [Fact]
    public void Translate_NullTableName_ThrowsArgumentException()
    {
        var action = () => Sut.Translate(null!, ODataQueryOptions.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Translate_NullOptions_ThrowsArgumentNullException()
    {
        var action = () => Sut.Translate("teams", null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
