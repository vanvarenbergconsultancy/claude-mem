using System;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>Thrown when a pagination cursor token is malformed, expired, or has been tampered with.</summary>
public sealed class InvalidCursorException : Exception
{
    public InvalidCursorException() : base("The cursor is invalid or has been tampered with.")
    {
    }
}
