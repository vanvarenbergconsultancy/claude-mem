using Microsoft.AspNetCore.Http;

namespace ClaudeMem.Admin.Api.Infrastructure
{
    /// <summary>Shared constants used across the API infrastructure.</summary>
    internal static class Constants
    {
        /// <summary>HTTP status code conventions agreed across all endpoints.</summary>
        public static class StatusCodeConventions
        {
            /// <summary>Status code returned when FluentValidation rejects the request payload.</summary>
            public const int ValidationFailedStatusCode = StatusCodes.Status422UnprocessableEntity;
        }
    }
}
