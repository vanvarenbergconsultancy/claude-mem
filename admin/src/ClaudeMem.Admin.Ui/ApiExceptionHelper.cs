using System;
using ClaudeMem.Admin.Api.Contracts;

namespace ClaudeMem.Admin.Ui;

internal static class ApiExceptionHelper
{
    internal static string GetUserMessage(Exception ex)
    {
        return ex switch
        {
            ApiException { StatusCode: 401 }
                => "Authentication failed — check the API key configuration.",
            ApiException<ProblemDetails> { Result.Detail: { Length: > 0 } detail }
                => detail,
            ApiException<ProblemDetails> { Result.Title: { Length: > 0 } title }
                => title,
            ApiException apiEx
                => $"Server error ({apiEx.StatusCode}). Please try again.",
            _
                => "Could not reach the server. Check your connection and try again."
        };
    }
}
