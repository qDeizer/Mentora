using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using PsikologProje_Void.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace PsikologProje_Void.Middleware
{
    public sealed class DetailedRequestLoggingMiddleware
    {
        private static readonly string[] TextualContentTypes =
        {
            "application/json",
            "application/problem+json",
            "application/xml",
            "application/x-www-form-urlencoded",
            "text/plain",
            "text/html",
            "text/xml"
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<DetailedRequestLoggingMiddleware> _logger;
        private readonly IOptionsMonitor<DetailedLoggingOptions> _loggingOptionsMonitor;

        public DetailedRequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<DetailedRequestLoggingMiddleware> logger,
            IOptionsMonitor<DetailedLoggingOptions> loggingOptionsMonitor)
        {
            _next = next;
            _logger = logger;
            _loggingOptionsMonitor = loggingOptionsMonitor;
        }

        public async Task Invoke(HttpContext context)
        {
            var options = _loggingOptionsMonitor.CurrentValue;
            if (!options.Enabled || ShouldSkip(context.Request.Path, options.ExcludedPaths))
            {
                await _next(context);
                return;
            }

            var request = context.Request;
            var stopwatch = Stopwatch.StartNew();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var userName = context.User.Identity?.Name ?? "anonymous";
            var requestPath = request.Path.Value ?? "/";
            var requestQuery = SanitizeQueryString(request.QueryString.Value, options.SensitiveKeys);
            var requestBody = await ReadRequestBodyIfEnabledAsync(request, options);
            var requestScheme = request.Scheme;
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = request.Headers.UserAgent.ToString();

            _logger.LogInformation(
                "HTTP request started {Method} {Path}{Query} CorrelationId={CorrelationId} UserId={UserId} UserName={UserName} RemoteIp={RemoteIp} Scheme={Scheme} UserAgent={UserAgent} RequestBody={RequestBody}",
                request.Method,
                requestPath,
                requestQuery,
                context.TraceIdentifier,
                userId,
                userName,
                remoteIp,
                requestScheme,
                userAgent,
                requestBody ?? "<omitted>");

            Exception? requestException = null;
            string? responseBody = null;
            Stream? originalBodyStream = null;
            MemoryStream? responseBodyCapture = null;

            if (options.IncludeResponseBody)
            {
                originalBodyStream = context.Response.Body;
                responseBodyCapture = new MemoryStream();
                context.Response.Body = responseBodyCapture;
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                requestException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                if (responseBodyCapture != null && originalBodyStream != null)
                {
                    responseBodyCapture.Seek(0, SeekOrigin.Begin);
                    responseBody = await ReadResponseBodySnippetAsync(responseBodyCapture, context.Response.ContentType, options);
                    responseBodyCapture.Seek(0, SeekOrigin.Begin);
                    await responseBodyCapture.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                }

                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                if (requestException != null)
                {
                    _logger.LogError(
                        requestException,
                        "HTTP request failed {Method} {Path}{Query} CorrelationId={CorrelationId} StatusCode={StatusCode} DurationMs={DurationMs:F2} UserId={UserId} ResponseBody={ResponseBody}",
                        request.Method,
                        requestPath,
                        requestQuery,
                        context.TraceIdentifier,
                        statusCode,
                        elapsedMs,
                        userId,
                        responseBody ?? "<omitted>");
                }
                else
                {
                    var level = statusCode >= StatusCodes.Status500InternalServerError
                        ? LogLevel.Error
                        : statusCode >= StatusCodes.Status400BadRequest
                            ? LogLevel.Warning
                            : LogLevel.Information;

                    _logger.Log(
                        level,
                        "HTTP request completed {Method} {Path}{Query} CorrelationId={CorrelationId} StatusCode={StatusCode} DurationMs={DurationMs:F2} UserId={UserId} ResponseContentType={ResponseContentType} ResponseBody={ResponseBody}",
                        request.Method,
                        requestPath,
                        requestQuery,
                        context.TraceIdentifier,
                        statusCode,
                        elapsedMs,
                        userId,
                        context.Response.ContentType ?? "<none>",
                        responseBody ?? "<omitted>");
                }
            }
        }

        private static async Task<string?> ReadRequestBodyIfEnabledAsync(HttpRequest request, DetailedLoggingOptions options)
        {
            if (!options.IncludeRequestBody)
            {
                return null;
            }

            if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
            {
                return null;
            }

            if (request.ContentLength == 0)
            {
                return string.Empty;
            }

            var contentType = request.ContentType ?? string.Empty;
            if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
            {
                return "<multipart content omitted>";
            }

            if (!IsTextualContentType(contentType))
            {
                return "<binary request content omitted>";
            }

            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await ReadTrimmedAsync(reader, options.MaxBodyLength);

            request.Body.Seek(0, SeekOrigin.Begin);
            return SanitizeBody(body, options.SensitiveKeys);
        }

        private static async Task<string?> ReadResponseBodySnippetAsync(Stream responseStream, string? responseContentType, DetailedLoggingOptions options)
        {
            if (!IsTextualContentType(responseContentType))
            {
                return "<binary response content omitted>";
            }

            using var reader = new StreamReader(responseStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await ReadTrimmedAsync(reader, options.MaxBodyLength);
            return SanitizeBody(body, options.SensitiveKeys);
        }

        private static bool ShouldSkip(PathString requestPath, IReadOnlyCollection<string>? excludedPaths)
        {
            if (excludedPaths == null || excludedPaths.Count == 0)
            {
                return false;
            }

            foreach (var excluded in excludedPaths)
            {
                if (string.IsNullOrWhiteSpace(excluded))
                {
                    continue;
                }

                if (requestPath.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTextualContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            var normalized = contentType.ToLowerInvariant();
            return TextualContentTypes.Any(x => normalized.StartsWith(x, StringComparison.Ordinal));
        }

        private static string SanitizeQueryString(string? queryString, IReadOnlyCollection<string>? sensitiveKeys)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return string.Empty;
            }

            var parsed = QueryHelpers.ParseQuery(queryString);
            if (parsed.Count == 0)
            {
                return queryString ?? string.Empty;
            }

            var items = new List<string>(parsed.Count);
            foreach (var pair in parsed)
            {
                var value = IsSensitiveKey(pair.Key, sensitiveKeys)
                    ? "***"
                    : string.Join(",", pair.Value.Select(v => v ?? string.Empty));
                items.Add($"{pair.Key}={value}");
            }

            return "?" + string.Join("&", items);
        }

        private static string SanitizeBody(string? body, IReadOnlyCollection<string>? sensitiveKeys)
        {
            if (string.IsNullOrEmpty(body))
            {
                return string.Empty;
            }

            if (sensitiveKeys == null || sensitiveKeys.Count == 0)
            {
                return body;
            }

            var sanitized = body;
            foreach (var key in sensitiveKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var escapedKey = Regex.Escape(key);
                var jsonPattern = $"(?i)(\\\"{escapedKey}\\\"\\s*:\\s*)(\\\".*?\\\"|[^,\\}}\\]]+)";
                sanitized = Regex.Replace(sanitized, jsonPattern, "$1\"***\"", RegexOptions.CultureInvariant | RegexOptions.Singleline);

                var formPattern = $"(?i)({escapedKey}\\s*=\\s*)([^&\\s]+)";
                sanitized = Regex.Replace(sanitized, formPattern, "$1***", RegexOptions.CultureInvariant);
            }

            return sanitized;
        }

        private static bool IsSensitiveKey(string key, IReadOnlyCollection<string>? sensitiveKeys)
        {
            if (sensitiveKeys == null || sensitiveKeys.Count == 0)
            {
                return false;
            }

            return sensitiveKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
        }

        private static async Task<string> ReadTrimmedAsync(TextReader reader, int maxLength)
        {
            var effectiveLength = Math.Clamp(maxLength, 128, 20000);
            var buffer = new char[effectiveLength + 1];
            var readCount = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

            if (readCount <= 0)
            {
                return string.Empty;
            }

            var text = new string(buffer, 0, Math.Min(readCount, effectiveLength));
            if (readCount > effectiveLength)
            {
                text += "... <truncated>";
            }

            return text.Normalize(NormalizationForm.FormKC);
        }
    }
}
