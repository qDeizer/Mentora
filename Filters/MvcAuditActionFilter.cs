using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PsikologProje_Void.Options;
using System.Collections;
using System.Diagnostics;
using System.Security.Claims;

namespace PsikologProje_Void.Filters
{
    public sealed class MvcAuditActionFilter : IAsyncActionFilter
    {
        private readonly ILogger<MvcAuditActionFilter> _logger;
        private readonly IOptionsMonitor<DetailedLoggingOptions> _loggingOptionsMonitor;

        public MvcAuditActionFilter(
            ILogger<MvcAuditActionFilter> logger,
            IOptionsMonitor<DetailedLoggingOptions> loggingOptionsMonitor)
        {
            _logger = logger;
            _loggingOptionsMonitor = loggingOptionsMonitor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var options = _loggingOptionsMonitor.CurrentValue;
            if (!options.Enabled)
            {
                await next();
                return;
            }

            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var controller = actionDescriptor?.ControllerName ?? "UnknownController";
            var action = actionDescriptor?.ActionName ?? "UnknownAction";
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var route = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            var actionArguments = SerializeArguments(context.ActionArguments, options.SensitiveKeys);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "MVC action started {Controller}.{Action} CorrelationId={CorrelationId} UserId={UserId} Route={Route} Arguments={Arguments}",
                controller,
                action,
                context.HttpContext.TraceIdentifier,
                userId,
                route,
                actionArguments);

            ActionExecutedContext executedContext;
            try
            {
                executedContext = await next();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "MVC action failed {Controller}.{Action} CorrelationId={CorrelationId} UserId={UserId} DurationMs={DurationMs:F2}",
                    controller,
                    action,
                    context.HttpContext.TraceIdentifier,
                    userId,
                    stopwatch.Elapsed.TotalMilliseconds);
                throw;
            }

            stopwatch.Stop();

            if (!executedContext.ModelState.IsValid)
            {
                var modelErrors = executedContext.ModelState
                    .Where(x => x.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(err => string.IsNullOrWhiteSpace(err.ErrorMessage) ? "validation-error" : err.ErrorMessage).ToArray());

                _logger.LogWarning(
                    "MVC action validation failed {Controller}.{Action} CorrelationId={CorrelationId} UserId={UserId} Errors={ModelErrors} DurationMs={DurationMs:F2}",
                    controller,
                    action,
                    context.HttpContext.TraceIdentifier,
                    userId,
                    modelErrors,
                    stopwatch.Elapsed.TotalMilliseconds);
            }

            if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
            {
                _logger.LogError(
                    executedContext.Exception,
                    "MVC action completed with unhandled exception {Controller}.{Action} CorrelationId={CorrelationId} UserId={UserId} DurationMs={DurationMs:F2}",
                    controller,
                    action,
                    context.HttpContext.TraceIdentifier,
                    userId,
                    stopwatch.Elapsed.TotalMilliseconds);
                return;
            }

            var resultType = executedContext.Result?.GetType().Name ?? "<none>";
            var statusCode = context.HttpContext.Response.StatusCode;

            _logger.LogInformation(
                "MVC action completed {Controller}.{Action} CorrelationId={CorrelationId} UserId={UserId} StatusCode={StatusCode} Result={ResultType} DurationMs={DurationMs:F2}",
                controller,
                action,
                context.HttpContext.TraceIdentifier,
                userId,
                statusCode,
                resultType,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        private static Dictionary<string, object?> SerializeArguments(
            IDictionary<string, object?> actionArguments,
            IReadOnlyCollection<string>? sensitiveKeys)
        {
            var output = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var argument in actionArguments)
            {
                if (IsSensitiveKey(argument.Key, sensitiveKeys))
                {
                    output[argument.Key] = "***";
                    continue;
                }

                output[argument.Key] = SerializeValue(argument.Value, sensitiveKeys);
            }

            return output;
        }

        private static object? SerializeValue(object? value, IReadOnlyCollection<string>? sensitiveKeys)
        {
            if (value is null)
            {
                return null;
            }

            if (value is IFormFile formFile)
            {
                return new
                {
                    formFile.Name,
                    formFile.FileName,
                    formFile.ContentType,
                    formFile.Length
                };
            }

            if (value is IEnumerable<IFormFile> formFiles)
            {
                return formFiles.Select(file => new
                {
                    file.Name,
                    file.FileName,
                    file.ContentType,
                    file.Length
                }).ToArray();
            }

            if (value is string stringValue)
            {
                return TrimString(stringValue);
            }

            if (value is DateTime or DateTimeOffset or Guid or Enum)
            {
                return value;
            }

            if (value.GetType().IsPrimitive || value is decimal)
            {
                return value;
            }

            if (value is IDictionary dictionary)
            {
                var mapped = new Dictionary<string, object?>();
                foreach (DictionaryEntry item in dictionary)
                {
                    var key = item.Key?.ToString() ?? "<null>";
                    mapped[key] = IsSensitiveKey(key, sensitiveKeys) ? "***" : SerializeValue(item.Value, sensitiveKeys);
                }

                return mapped;
            }

            if (value is IEnumerable enumerable and not string)
            {
                var list = new List<object?>();
                foreach (var item in enumerable)
                {
                    list.Add(SerializeValue(item, sensitiveKeys));
                    if (list.Count >= 20)
                    {
                        list.Add("<truncated>");
                        break;
                    }
                }

                return list;
            }

            return $"<{value.GetType().Name}>";
        }

        private static string TrimString(string value)
        {
            if (value.Length <= 250)
            {
                return value;
            }

            return $"{value[..250]}... <truncated>";
        }

        private static bool IsSensitiveKey(string key, IReadOnlyCollection<string>? sensitiveKeys)
        {
            if (sensitiveKeys == null || sensitiveKeys.Count == 0)
            {
                return false;
            }

            return sensitiveKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
        }
    }
}
