
using System;

using AwesomeAssertions;
using AwesomeAssertions.Execution;
using SqlKata.Compilers;

using Xunit;

namespace ODataFilter.Core.Tests.Unit;
/// <summary>
/// Covers filter operators and functions not exercised by <see cref="ODataSqlTranslatorTests"/>:
/// NOT functions, case-insensitive matching, null equality, in-operator, numeric operators,
/// date parsing, matchesPattern, and TranslateToQuery composition.
/// </summary>
public sealed class ODataSqlTranslatorFilterFunctionTests
{
    private static readonly ODataSqlTranslator Sut = new(new PostgresCompiler());

    // ── IS NULL / IS NOT NULL ─────────────────────────────────────────────────

    [Theory]
    [InlineData("deletedAt eq null", @"SELECT * FROM ""items"" WHERE ""deletedAt"" IS NULL")]
    [InlineData("deletedAt ne null", @"SELECT * FROM ""items"" WHERE ""deletedAt"" IS NOT NULL")]
    public void Translate_NullFilter_EmitsSql(string filter, string expectedSql)
    {
        var result = Sut.Translate("items", new ODataQueryOptions { Filter = filter });

        result.Sql.Should().Be(expectedSql);
    }

    // ── Single string @p0 ─────────────────────────────────────────────────────
    // Covers: NOT like/ilike, tolower ilike, matchesPattern, indexof, toupper eq

    [Theory]
    [InlineData("not contains(name,'xyz')",              @"SELECT * FROM ""items"" WHERE NOT (""name"" like @p0)",  "%xyz%")]
    [InlineData("not startswith(name,'abc')",            @"SELECT * FROM ""items"" WHERE NOT (""name"" like @p0)",  "abc%")]
    [InlineData("not endswith(name,'xyz')",              @"SELECT * FROM ""items"" WHERE NOT (""name"" like @p0)",  "%xyz")]
    [InlineData("contains(tolower(name),'xyz')",         @"SELECT * FROM ""items"" WHERE ""name"" ilike @p0",       "%xyz%")]
    [InlineData("startswith(tolower(name),'abc')",       @"SELECT * FROM ""items"" WHERE ""name"" ilike @p0",       "abc%")]
    [InlineData("matchesPattern(name,'acme.*')",         @"SELECT * FROM ""items"" WHERE ""name"" like @p0",        "acme%")]
    [InlineData("matchesPattern(name,'%5Eacme.*')",      @"SELECT * FROM ""items"" WHERE ""name"" like @p0",        "acme%")]
    [InlineData("matchesPattern(name,'acme$')",          @"SELECT * FROM ""items"" WHERE ""name"" like @p0",        "acme")]
    [InlineData("indexof(name,'tea') eq -1",             @"SELECT * FROM ""items"" WHERE NOT (""name"" like @p0)",  "%tea%")]
    [InlineData("indexof(name,'tea') ge 0",              @"SELECT * FROM ""items"" WHERE ""name"" like @p0",        "%tea%")]
    [InlineData("indexof(tolower(name),'tea') eq -1",    @"SELECT * FROM ""items"" WHERE NOT (""name"" ilike @p0)", "%tea%")]
    [InlineData("toupper(name) eq 'Tea'",                @"SELECT * FROM ""items"" WHERE ""name"" ilike @p0",       "Tea")]
    public void Translate_Filter_EmitsSqlWithStringParam(string filter, string expectedSql, string p0)
    {
        var result = Sut.Translate("items", new ODataQueryOptions { Filter = filter });

        result.Sql.Should().Be(expectedSql);
        result.Parameters["@p0"].Should().Be(p0);
    }

    // ── Single int @p0 ────────────────────────────────────────────────────────
    // Covers: numeric comparisons (lt/le/ge/eq) and date-part functions (year/month/day/hour/minute)

    [Theory]
    [InlineData("amount lt 100",             "orders", @"SELECT * FROM ""orders"" WHERE ""amount"" < @p0",                          100)]
    [InlineData("amount le 100",             "orders", @"SELECT * FROM ""orders"" WHERE ""amount"" <= @p0",                         100)]
    [InlineData("amount ge 50",              "orders", @"SELECT * FROM ""orders"" WHERE ""amount"" >= @p0",                         50)]
    [InlineData("count eq 0",                "stats",  @"SELECT * FROM ""stats"" WHERE ""count"" = @p0",                            0)]
    [InlineData("year(createdAt) eq 2024",   "events", @"SELECT * FROM ""events"" WHERE DATE_PART('YEAR', ""createdAt"") = @p0",    2024)]
    [InlineData("month(createdAt) eq 3",     "events", @"SELECT * FROM ""events"" WHERE DATE_PART('MONTH', ""createdAt"") = @p0",   3)]
    [InlineData("day(createdAt) ge 15",      "events", @"SELECT * FROM ""events"" WHERE DATE_PART('DAY', ""createdAt"") >= @p0",    15)]
    [InlineData("hour(startTime) ge 9",      "events", @"SELECT * FROM ""events"" WHERE DATE_PART('HOUR', ""startTime"") >= @p0",   9)]
    [InlineData("minute(startTime) lt 30",   "events", @"SELECT * FROM ""events"" WHERE DATE_PART('MINUTE', ""startTime"") < @p0",  30)]
    public void Translate_Filter_EmitsSqlWithIntParam(string filter, string table, string expectedSql, int p0)
    {
        var result = Sut.Translate(table, new ODataQueryOptions { Filter = filter });

        result.Sql.Should().Be(expectedSql);
        result.Parameters["@p0"].Should().Be(p0);
    }

    // ── Cases that don't fit the theory shapes ────────────────────────────────

    [Fact]
    public void Translate_InOperator_EmitsInClause()
    {
        var result = Sut.Translate("items", new ODataQueryOptions { Filter = "status in ('active','pending','draft')" });

        result.Sql.Should().Be(@"SELECT * FROM ""items"" WHERE ""status"" IN (@p0, @p1, @p2)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("active");
        result.Parameters["@p1"].Should().Be("pending");
        result.Parameters["@p2"].Should().Be("draft");
    }

    [Fact]
    public void Translate_NotInOperator_EmitsNotIn()
    {
        var result = Sut.Translate("items", new ODataQueryOptions { Filter = "not (status in ('archived','deleted'))" });

        result.Sql.Should().Be(@"SELECT * FROM ""items"" WHERE ""status"" NOT IN (@p0, @p1)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("archived");
        result.Parameters["@p1"].Should().Be("deleted");
    }

    [Fact]
    public void Translate_InOperatorWithIntegers_EmitsInClause()
    {
        var result = Sut.Translate("orders", new ODataQueryOptions { Filter = "orderId in (2, 4, 8, 16)" });

        result.Sql.Should().Be(@"SELECT * FROM ""orders"" WHERE ""orderId"" IN (@p0, @p1, @p2, @p3)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be(2);
        result.Parameters["@p1"].Should().Be(4);
        result.Parameters["@p2"].Should().Be(8);
        result.Parameters["@p3"].Should().Be(16);
    }

    [Fact]
    public void Translate_DateRangeFilter_ParsesDatesToParameters()
    {
        var options = new ODataQueryOptions
        {
            Filter = "createdAt ge 2024-01-01T00:00:00Z and createdAt lt 2025-01-01T00:00:00Z"
        };

        var result = Sut.Translate("events", options);

        // OData parser yields DateTimeOffset for typed date-time literals (ISO 8601 with timezone)
        result.Sql.Should().Be(@"SELECT * FROM ""events"" WHERE (""createdAt"" >= @p0 AND ""createdAt"" < @p1)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().BeOfType<DateTimeOffset>();
        result.Parameters["@p1"].Should().BeOfType<DateTimeOffset>();
    }

    [Fact]
    public void Translate_NestedOrInsideAnd_EmitsCorrectGroups()
    {
        var result = Sut.Translate("teams", new ODataQueryOptions
        {
            Filter = "(name eq 'Acme' or name eq 'Beta') and active eq true"
        });

        // boolean true is inlined; only the two string params are bound
        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE ((""name"" = @p0 OR ""name"" = @p1) AND ""active"" = true)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("Acme");
        result.Parameters["@p1"].Should().Be("Beta");
    }

    [Fact]
    public void Translate_ComplexAndOrNotFilter_EmitsCorrectClauses()
    {
        // Mirrors upstream AdvancedFilters test case: nested OR/AND with NOT
        var options = new ODataQueryOptions
        {
            Filter = "(contains(name,'Tea') or (inventory ge 100 and inventory le 200)) and not (origin eq 'US' or origin eq 'UK')"
        };

        var result = Sut.Translate("products", options);

        result.Sql.Should().Be(
            @"SELECT * FROM ""products"" WHERE " +
            @"((""name"" like @p0 OR (""inventory"" >= @p1 AND ""inventory"" <= @p2)) " +
            @"AND NOT (""origin"" = @p3 OR ""origin"" = @p4))");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("%Tea%");
        result.Parameters["@p1"].Should().Be(100);
        result.Parameters["@p2"].Should().Be(200);
        result.Parameters["@p3"].Should().Be("US");
        result.Parameters["@p4"].Should().Be("UK");
    }

    [Fact]
    public void Translate_AndFilterCombined_InjectsScopeBeforeUserFilter()
    {
        var scoped = new ODataQueryOptions { Filter = "name eq 'Acme'" }.AndFilter("tenantId eq 'abc'");

        var result = Sut.Translate("teams", scoped);

        result.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE (""name"" = @p0 AND ""tenantId"" = @p1)");
        using var scope = new AssertionScope();
        result.Parameters["@p0"].Should().Be("Acme");
        result.Parameters["@p1"].Should().Be("abc");
    }

    [Fact]
    public void Translate_RightSideFunctionCall_ThrowsNotSupported()
    {
        // OData allows function calls on the right side of comparisons (e.g. name eq toupper('Acme')).
        // ApplyComparisonOperator only handles Constant and PropertyAccess on the right side.
        // This test verifies that unhandled right-side kinds throw rather than silently
        // returning a WHERE-less query (which would return all rows with no error).
        var action = () => Sut.Translate("items", new ODataQueryOptions { Filter = "name eq toupper('Acme')" });

        action.Should().Throw<NotSupportedException>();
    }

    // ── TranslateToQuery ─────────────────────────────────────────────────────

    [Fact]
    public void TranslateToQuery_ReturnsComposableQuery()
    {
        var query = Sut.TranslateToQuery("teams", new ODataQueryOptions { Filter = "active eq true" });

        var compiled = new PostgresCompiler().Compile(query);
        compiled.Sql.Should().Be(@"SELECT * FROM ""teams"" WHERE ""active"" = true");
    }

    [Fact]
    public void TranslateToQuery_AllowsJoinComposition()
    {
        var query = Sut.TranslateToQuery("teams", new ODataQueryOptions { Filter = "active eq true" })
            .Join("customers", "customers.id", "teams.customerId")
            .Select("teams.*", "customers.name AS customerName");

        var compiled = new PostgresCompiler().Compile(query);
        // SqlKata emits a newline before INNER JOIN
        compiled.Sql.Should().Be(
            @"SELECT ""teams"".*, ""customers"".""name"" AS ""customerName"" FROM ""teams"" " + "\n" +
            @"INNER JOIN ""customers"" ON ""customers"".""id"" = ""teams"".""customerId"" WHERE ""active"" = true");
    }

    [Fact]
    public void TranslateToQuery_WithEmptyOptions_ReturnsSelectStarQuery()
    {
        var compiled = new PostgresCompiler().Compile(Sut.TranslateToQuery("teams", ODataQueryOptions.Empty));

        compiled.Sql.Should().Be(@"SELECT * FROM ""teams""");
    }
}
