using MediatR;
using Serilog.Context;
using System.Diagnostics;

namespace SerilogBestPractices.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger
        ) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopwatch = Stopwatch.StartNew();

            logger.LogInformation(
               "Processing {RequestName}",
               requestName);

            try
            {
                var response = await next();

                logger.LogInformation(
                    "Processed {RequestName} in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                using (LogContext.PushProperty("Error", ex, destructureObjects: true))
                {
                    logger.LogError(
                        ex,
                        "Error processing {RequestName} after {ElapsedMilliseconds} ms",
                        requestName,
                        stopwatch.ElapsedMilliseconds);
                }

                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
