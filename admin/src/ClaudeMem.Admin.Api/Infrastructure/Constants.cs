using Microsoft.AspNetCore.Http;

namespace ClaudeMem.Admin.Api.Infrastructure
{
    internal static class Constants
    {
        public static class StatusCodeConventions
        {
            public const int ValidationFailedStatusCode = StatusCodes.Status422UnprocessableEntity;
        }
    }
}
