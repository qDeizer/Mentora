namespace PsikologProje_Void.Options
{
    public sealed class DetailedLoggingOptions
    {
        public const string SectionName = "DetailedLogging";

        public bool Enabled { get; set; } = true;
        public bool IncludeRequestBody { get; set; } = true;
        public bool IncludeResponseBody { get; set; } = false;
        public int MaxBodyLength { get; set; } = 4096;
        public bool EnableEfDetailedErrors { get; set; } = true;
        public bool EnableEfSensitiveDataLogging { get; set; } = false;

        public string[] ExcludedPaths { get; set; } =
        {
            "/health/live",
            "/health/ready",
            "/css",
            "/js",
            "/lib",
            "/images",
            "/favicon.ico"
        };

        public string[] SensitiveKeys { get; set; } =
        {
            "password",
            "confirmPassword",
            "oldPassword",
            "newPassword",
            "token",
            "access_token",
            "refresh_token",
            "authorization",
            "cookie",
            "set-cookie",
            "secret",
            "client_secret",
            "apiKey",
            "apikey"
        };
    }
}
