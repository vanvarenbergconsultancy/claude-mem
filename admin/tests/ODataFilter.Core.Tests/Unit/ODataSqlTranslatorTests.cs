
using System;

using AwesomeAssertions;
using AwesomeAssertions.Execution;
using SqlKata.Compilers;

using Xunit;

namespace ODataFilter.Core.Tests.Unit;
public sealed class ODataSqlTranslatorTests
{
    private static readonly ODataSqlTranslator Sut = new(new PostgresCompiler());

    // ── Basic filter → single string param ──────────────────────────────────

    [Theory]
    [InlineData("name eq 'Acme'",       "teams", @"SELECT * FROM ""teams"" WHERE ""name"" = @p0",      "Acme")]
    [InlineData("contains(name,'acme')", "teams", @"SELECT * FROM ""teams"" WHERE ""name"" like @p0",  "%acme%")]
    [InlineData("startswith(name,'ac')", "teams", @"SELECT * FROM ""teams"" WHERE ""name"" like @p0",  "ac%")]
    [InlineData("endswith(name,'me')",   "teams", @"SELECT * FROM ""teams"" WHERE ""name"" like @p0",  "%me")]
    public void Translate_Filter_EmitsSqlWithStringParam(string filter, string table, string expectedSql, string p0)
    {
        var result = Sut.Translate(table, new ODataQueryOptions { Filter = filter });

        result.Sql.Should().Be(expectedSql);
        result.Parameters["@p0"].Should().Be(p0);
    }

    // ── OrderBy ──────────────────────────────────────────────────────────────

    [Theory]
    // PostgresCompiler omits ASC keyword (it is the default direction in SQL)
    [InlineData("name asc",                  @"SELECT * FROM ""teams"" ORDER BY ""name""")]
    [InlineData("createdAt desc",            @"SELECT * FROM ""teams"" ORDER BY ""createdAt"" DESC")]
    [InlineData("name asc, createdAt desc",  @"SELECT * FROM ""teams"" ORDER BY ""name"", ""createdAt"" DESC")]
    public void Translate_OrderBy_EmitsSql(string orderBy, string expectedSql)
    {
        var result = Sut.Translate("teams", new ODataQueryOptions { OrderBy = orderBy });

        result.Sql.Should().Be(expectedSql);
    }

    // ── Cases that don't fit the theory shapes ────────────────────────────────

    [Fact]
    public void Translate_NoOptions_EmitsSelectStar()
    {
        var result = Sut.Translate("teams", ODataQueryOptions.Empty);

        using var scope = new AssertionScope();
        result.Sql.Should().Be(@"SELECT * FROM ""teams""");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Translate_NeFilter_EmitsNotEqualClause()
    {
        // SqlKata PostgresCompiler emits != (not <>) and inlines boolean literals (no binding)
        var result = Sut.Translate("teams", new ODataQueryOptions { Filter = "active ne false" });

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE ""active"" != false");
    }

    [Fact]
    public void Translate_GtFilter_EmitsGreaterThan()
    {
        var result = Sut.Translate("events", new ODataQueryOptions { Filter = "createdAt gt 2024-01-01T00:00:00Z" });

        result.Sql.Should().Be(@"SELECT * FROM ""events"" WHERE ""createdAt"" > @p0");
        result.Parameters["@p0"].Should().BeOfType<DateTimeOffset>();
    }

    [Fact]
    public void Translate_AndFilter_EmitsAndClause()
    {
        // boolean literal true is inlined; only the string param is bound
        var result = Sut.Translate("teams", new ODataQueryOptions { Filter = "active eq true and name eq 'Acme'" });

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE (""active"" = true AND ""name"" = @p0)");
        result.Parameters["@p0"].Should().Be("Acme");
    }

    [Fact]
    public void Translate_OrFilter_EmitsOrClause()
    {
        var result = Sut.Translate("teams", new ODataQueryOptions { Filter = "name eq 'Acme' or name eq 'Beta'" });

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE (""name"" = @p0 OR ""name"" = @p1)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("Acme");
        result.Parameters["@p1"].Should().Be("Beta");
    }

    [Fact]
    public void Translate_TopOption_EmitsLimit()
    {
        // SqlKata parameterizes LIMIT values
        var result = Sut.Translate("teams", new ODataQueryOptions { Top = 10 });

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" LIMIT @p0");
        result.Parameters["@p0"].Should().Be(10);
    }

    [Fact]
    public void Translate_TopWithFilter_EmitsBothWhereAndLimit()
    {
        var result = Sut.Translate("teams", new ODataQueryOptions { Filter = "name eq 'x'", Top = 5 });

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE ""name"" = @p0 LIMIT @p1");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("x");
        result.Parameters["@p1"].Should().Be(5);
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
