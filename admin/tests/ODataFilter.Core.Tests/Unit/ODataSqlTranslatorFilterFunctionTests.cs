namespace ODataFilter.Core.Tests.Unit;

using System;

using AwesomeAssertions;
using AwesomeAssertions.Execution;

using ODataFilter.Core;

using SqlKata.Compilers;

using Xunit;

/// <summary>
/// Covers filter operators and functions not exercised by <see cref="ODataSqlTranslatorTests"/>:
/// NOT functions, case-insensitive matching, null equality, in-operator, numeric operators,
/// date parsing, matchesPattern, and TranslateToQuery composition.
/// </summary>
public sealed class ODataSqlTranslatorFilterFunctionTests
{
    private static readonly ODataSqlTranslator Sut = new(new PostgresCompiler());

    // ── NOT function wrappers ───────────────────────────────────────────────

    [Fact]
    public void Translate_NotContains_EmitsNotLike()
    {
        var options = new ODataQueryOptions { Filter = "not contains(name,'xyz')" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT");
        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_NotStartsWith_EmitsNotLike()
    {
        var options = new ODataQueryOptions { Filter = "not startswith(name,'abc')" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT");
        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_NotEndsWith_EmitsNotLike()
    {
        var options = new ODataQueryOptions { Filter = "not endswith(name,'xyz')" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT");
        result.Sql.Should().Contain("like");
    }

    // ── Case-insensitive via tolower() / toupper() ──────────────────────────

    [Fact]
    public void Translate_ContainsWithTolower_EmitsCaseInsensitiveLike()
    {
        var options = new ODataQueryOptions { Filter = "contains(tolower(name),'xyz')" };

        var result = Sut.Translate("items", options);

        // caseSensitive=false → PostgresCompiler emits ilike
        result.Sql.Should().Contain("ilike");
    }

    [Fact]
    public void Translate_StartsWithTolower_EmitsCaseInsensitiveLike()
    {
        var options = new ODataQueryOptions { Filter = "startswith(tolower(name),'abc')" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("ilike");
    }

    // ── Null equality ───────────────────────────────────────────────────────

    [Fact]
    public void Translate_EqNull_EmitsIsNull()
    {
        var options = new ODataQueryOptions { Filter = "deletedAt eq null" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("IS NULL");
    }

    [Fact]
    public void Translate_NeNull_EmitsIsNotNull()
    {
        var options = new ODataQueryOptions { Filter = "deletedAt ne null" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("IS NOT NULL");
    }

    // ── in operator ─────────────────────────────────────────────────────────

    [Fact]
    public void Translate_InOperator_EmitsInClause()
    {
        var options = new ODataQueryOptions { Filter = "status in ('active','pending','draft')" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("IN");
        result.Parameters.Should().HaveCount(3);
        result.Parameters.Values.Should().Contain("active");
        result.Parameters.Values.Should().Contain("pending");
        result.Parameters.Values.Should().Contain("draft");
    }

    [Fact]
    public void Translate_NotInOperator_EmitsNotIn()
    {
        var options = new ODataQueryOptions { Filter = "not (status in ('archived','deleted'))" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT IN");
    }

    // ── Numeric comparison operators ────────────────────────────────────────

    [Fact]
    public void Translate_LtFilter_EmitsLessThan()
    {
        var options = new ODataQueryOptions { Filter = "amount lt 100" };

        var result = Sut.Translate("orders", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("<");
        result.Parameters.Values.Should().Contain(100);
    }

    [Fact]
    public void Translate_LeFilter_EmitsLessThanOrEqual()
    {
        var options = new ODataQueryOptions { Filter = "amount le 100" };

        var result = Sut.Translate("orders", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("<=");
        result.Parameters.Values.Should().Contain(100);
    }

    [Fact]
    public void Translate_GeFilter_EmitsGreaterThanOrEqual()
    {
        var options = new ODataQueryOptions { Filter = "amount ge 50" };

        var result = Sut.Translate("orders", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain(">=");
        result.Parameters.Values.Should().Contain(50);
    }

    [Fact]
    public void Translate_NumericEq_EmitsEqualWithParameter()
    {
        var options = new ODataQueryOptions { Filter = "count eq 0" };

        var result = Sut.Translate("stats", options);

        result.Parameters.Values.Should().Contain(0);
    }

    // ── Date parsing ─────────────────────────────────────────────────────────

    [Fact]
    public void Translate_DateRangeFilter_ParsesDatesToParameters()
    {
        var options = new ODataQueryOptions
        {
            Filter = "createdAt ge 2024-01-01T00:00:00Z and createdAt lt 2025-01-01T00:00:00Z"
        };

        var result = Sut.Translate("events", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("AND");
        // OData parser yields DateTimeOffset for typed date-time literals (ISO 8601 with timezone)
        result.Parameters.Values.Should().AllSatisfy(v => v.Should().BeOfType<DateTimeOffset>());
    }

    // ── matchesPattern ───────────────────────────────────────────────────────

    [Fact]
    public void Translate_MatchesPattern_EmitsLikeWithPattern()
    {
        var options = new ODataQueryOptions { Filter = "matchesPattern(name,'acme.*')" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("like");
        // acme.* → acme% after pattern translation
        result.Parameters.Values.Should().ContainSingle(v => v is string && ((string)v).StartsWith("acme", StringComparison.Ordinal));
    }

    // ── Nested / parenthesised logic ─────────────────────────────────────────

    [Fact]
    public void Translate_NestedOrInsideAnd_EmitsCorrectGroups()
    {
        var options = new ODataQueryOptions
        {
            Filter = "(name eq 'Acme' or name eq 'Beta') and active eq true"
        };

        var result = Sut.Translate("teams", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("OR");
        result.Sql.Should().Contain("AND");
        // 'Acme' and 'Beta' are parameterized; true is inlined
        result.Parameters.Should().HaveCount(2);
    }

    // ── TranslateToQuery ─────────────────────────────────────────────────────

    [Fact]
    public void TranslateToQuery_ReturnsComposableQuery()
    {
        var options = new ODataQueryOptions { Filter = "active eq true" };

        var query = Sut.TranslateToQuery("teams", options);

        query.Should().NotBeNull();
    }

    [Fact]
    public void TranslateToQuery_AllowsJoinComposition()
    {
        var options = new ODataQueryOptions { Filter = "active eq true" };

        var query = Sut.TranslateToQuery("teams", options)
            .Join("customers", "customers.id", "teams.customerId")
            .Select("teams.*", "customers.name AS customerName");

        var compiled = new PostgresCompiler().Compile(query);
        using var scope = new AssertionScope();
        compiled.Sql.Should().Contain("JOIN");
        compiled.Sql.Should().Contain("customers");
    }

    [Fact]
    public void TranslateToQuery_WithEmptyOptions_ReturnsSelectStarQuery()
    {
        var query = Sut.TranslateToQuery("teams", ODataQueryOptions.Empty);

        var compiled = new PostgresCompiler().Compile(query);
        compiled.Sql.Should().Be("SELECT * FROM \"teams\"");
    }

    // ── indexof() function ────────────────────────────────────────────────────

    [Fact]
    public void Translate_IndexOfEqNegativeOne_EmitsNotLike()
    {
        // indexof(name,'tea') eq -1 → does not contain 'tea'
        var options = new ODataQueryOptions { Filter = "indexof(name,'tea') eq -1" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT");
        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_IndexOfEqPositive_EmitsLike()
    {
        // indexof(name,'tea') ge 0 → contains 'tea'
        var options = new ODataQueryOptions { Filter = "indexof(name,'tea') ge 0" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("like");
    }

    [Fact]
    public void Translate_IndexOfWithTolower_EmitsCaseInsensitiveNotLike()
    {
        // indexof(tolower(name),'tea') eq -1 → does not contain 'tea' case-insensitively
        var options = new ODataQueryOptions { Filter = "indexof(tolower(name),'tea') eq -1" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("NOT");
        result.Sql.Should().Contain("ilike");
    }

    // ── toupper/tolower direct equality ───────────────────────────────────────

    [Fact]
    public void Translate_TolowerEq_EmitsCaseInsensitiveLike()
    {
        // toupper(name) eq 'Tea' → case-insensitive match (WhereLike with false)
        var options = new ODataQueryOptions { Filter = "toupper(name) eq 'Tea'" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("ilike");
    }

    // ── Integer in-operator ───────────────────────────────────────────────────

    [Fact]
    public void Translate_InOperatorWithIntegers_EmitsInClause()
    {
        var options = new ODataQueryOptions { Filter = "orderId in (2, 4, 8, 16)" };

        var result = Sut.Translate("orders", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("IN");
        result.Parameters.Should().HaveCount(4);
    }

    // ── Advanced compound filter ──────────────────────────────────────────────

    [Fact]
    public void Translate_ComplexAndOrNotFilter_EmitsCorrectClauses()
    {
        // Mirrors upstream AdvancedFilters test case: nested OR/AND with NOT
        var options = new ODataQueryOptions
        {
            Filter = "(contains(name,'Tea') or (inventory ge 100 and inventory le 200)) and not (origin eq 'US' or origin eq 'UK')"
        };

        var result = Sut.Translate("products", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("like");
        result.Sql.Should().Contain("AND");
        result.Sql.Should().Contain("OR");
        result.Sql.Should().Contain("NOT");
    }

    // ── AndFilter SQL integration ─────────────────────────────────────────────

    [Fact]
    public void Translate_AndFilterCombined_InjectsScopeBeforeUserFilter()
    {
        var userOptions = new ODataQueryOptions { Filter = "name eq 'Acme'" };
        var scoped = userOptions.AndFilter("tenantId eq 'abc'");

        var result = Sut.Translate("teams", scoped);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("AND");
        result.Parameters.Values.Should().Contain("Acme");
        result.Parameters.Values.Should().Contain("abc");
    }
}
