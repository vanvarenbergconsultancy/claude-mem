using System.ComponentModel.DataAnnotations;

namespace ClaudeMem.Admin.Ui.Services;

/// <summary>Configuration options for connecting the UI to the Admin API backend.</summary>
internal sealed class AdminApiOptions
{
    /// <summary>Base URL of the Admin API (e.g. <c>https://api.example.com</c>). Required.</summary>
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>The API key used to authenticate requests to the Admin API. Required.</summary>
    [Required]
    public string ApiKey { get; init; } = string.Empty;
}
