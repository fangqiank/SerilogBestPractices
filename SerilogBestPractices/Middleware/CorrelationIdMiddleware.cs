using Serilog.Context;

namespace SerilogBestPractices.Middleware
{
    public class CorrelationIdMiddleware(
        RequestDelegate next
        )
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";

        public async Task InvokeAsync(HttpContext context)
        {
            // 优先使用客户端传入的 CorrelationId，否则回退到 TraceIdentifier
            var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
                ? headerValue.ToString()
                : context.TraceIdentifier;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[CorrelationIdHeader] = correlationId;
                    return Task.CompletedTask;
                });

                await next(context);
            }
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
