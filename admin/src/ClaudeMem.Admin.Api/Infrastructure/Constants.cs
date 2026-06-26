using Microsoft.AspNetCore.Http;

namespace ClaudeMem.Admin.Api.Infrastructure
{
    /// <summary>Shared constants used across the API infrastructure.</summary>
    internal static class Constants
    {
        public static class AppSettings
        {
            public const string AdminApiKey = "AdminApiKey";
            public const string UseLocalContainerKey = "UseLocalContainer";
        }

        public static class Authentication
        {
            public const string ApiKeySchemeName = "ApiKey";
        }

        public static class ConnectionStrings
        {
            public const string ConnectionStringName = "Default";
        }

        public static class Docker
        {
            public const string SecretsPath = "/run/secrets";
            public const string AdminApiKeySecretPath = $"{SecretsPath}/admin_api_key";
        }

        public static class Environments
        {
            public const string Local = "Local";
        }

        /// <summary>HTTP status code conventions agreed across all endpoints.</summary>
        public static class StatusCodeConventions
        {
            /// <summary>Status code returned when FluentValidation rejects the request payload.</summary>
            public const int ValidationFailedStatusCode = StatusCodes.Status422UnprocessableEntity;
        }
    }
}
