using System;
using System.Diagnostics.CodeAnalysis;

namespace ClaudeMem.Admin.Api.Contracts;

public interface IResponsePage
{
    Uri Self { get; init; }
    Uri First { get; init; }
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Matches generated ResponsePage.Next property from OpenAPI spec.")]
    Uri? Next { get; init; }
}
