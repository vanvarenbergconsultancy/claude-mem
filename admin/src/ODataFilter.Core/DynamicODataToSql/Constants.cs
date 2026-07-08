namespace ODataFilter.Core.DynamicODataToSql;

internal static class Constants
{
    public static class Sql
    {
        public static class Dates
        {
            public const string Year = "YEAR";
            public const string Month = "MONTH";
            public const string Day = "DAY";
            public const string Hour = "HOUR";
            public const string Minute = "MINUTE";
            public const string Date = "DATE";
            public const string Time = "TIME";
        }

        public static class Functions
        {
            public const string ToUpper = "TOUPPER";
            public const string ToLower = "TOLOWER";
            public const string IndexOf = "INDEXOF";
        }
    }

    public static class Formatting
    {
        public const string DateFormat = "yyyy-MM-dd";
        public const string HourFormat = "HH:mm";
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
        public const string DateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ssZ";
    }

    public static class ODataOperations
    {
        public const string Top = "$top";
        public const string Filter = "$filter";
        public const string OrderBy = "$orderby";

        public static class BinaryOperator
        {
            public const string Equal = "=";
            public const string NotEqual = "<>";
            public const string GreaterThan = ">";
            public const string GreaterThanOrEqual = ">=";
            public const string LessThan = "<";
            public const string LessThanOrEqual = "<=";
        }

        public static class ComparisonOperator
        {
            public const string Contains = "contains";
            public const string NotContains = "not contains";
            public const string StartsWith = "startswith";
            public const string EndsWith = "endswith";
            public const string MatchesPattern = "matchespattern";
            public const string Equal = "eq";
            public const string NotEqual = "ne";
            public const string GreaterThan = "gt";
            public const string GreaterThanOrEqual = "ge";
            public const string LessThan = "lt";
            public const string LessThanOrEqual = "le";
        }

        public static class LogicalOperator
        {
            public const string And = "and";
            public const string Or = "or";
        }
    }
}