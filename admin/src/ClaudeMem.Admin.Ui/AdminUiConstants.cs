namespace ClaudeMem.Admin.Ui;

internal static class AdminUiConstants
{
    internal static class Formats
    {
        internal const string DateOnly = "yyyy-MM-dd";
        internal const string DateAndTime = "yyyy-MM-dd HH:mm";
    }

    internal static class JobStatus
    {
        internal const string Completed = "completed";
        internal const string Failed = "failed";
        internal const string Processing = "processing";
        internal const string Queued = "queued";
        internal const string Cancelled = "cancelled";
    }
}
