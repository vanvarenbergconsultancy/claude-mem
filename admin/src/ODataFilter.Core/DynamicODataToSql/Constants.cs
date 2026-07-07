namespace ODataFilter.Core.DynamicODataToSql;

internal static class Constants
{
    internal static class Operations
    {
        public const string ToUpper = "TOUPPER";
        public const string ToLower = "TOLOWER";
        public const string IndexOf = "INDEXOF";
    }

    internal static class BinaryOperators
    {
        public const string Equal = "=";
        public const string NotEqual = "<>";
        public const string GreaterThan = ">";
        public const string GreaterThanOrEqual = ">=";
        public const string LessThan = "<";
        public const string LessThanOrEqual = "<=";
        public const string And = "and";
        public const string Or = "or";
    }

    internal static class Dates 
    {
        public const string DateFormat = "yyyy-MM-dd";
        public const string HourFormat = "HH:mm";
        public const string Year = "YEAR";
        public const string Month = "MONTH";
        public const string Day = "DAY";
        public const string Hour = "HOUR";
        public const string Minute = "MINUTE";
        public const string Date = "DATE";
        public const string Time = "TIME";
    }
}