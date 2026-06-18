using Serilog.Context;
using System.Diagnostics;

namespace PsikologProje_Void.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
            context.TraceIdentifier = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(string? incoming)
        {
            if (!string.IsNullOrWhiteSpace(incoming))
            {
                var normalized = incoming.Trim();
                if (normalized.Length <= 128)
                {
                    return normalized;
                }
            }

            return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }
    }
}
