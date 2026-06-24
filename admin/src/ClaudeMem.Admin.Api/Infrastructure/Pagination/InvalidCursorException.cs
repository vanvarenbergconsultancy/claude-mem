using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

public sealed class InvalidCursorException : Exception
{
    public InvalidCursorException() : base("The cursor is invalid or has been tampered with.")
    {
    }
}
