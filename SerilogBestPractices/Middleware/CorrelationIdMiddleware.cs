using Serilog.Context;

namespace SerilogBestPractices.Middleware
{
    public class CorrelationIdMiddleware(
        RequestDelegate next
        )
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // 从 HttpContext.TraceIdentifier 获取关联 ID
            var correlationId = context.TraceIdentifier;

            // 将 CorrelationId 推入 LogContext，在整个请求生命周期内可用
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Correlation-Id"] = correlationId;
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
