using System.Text;

namespace Zeta.APiGateway.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request
            var requestTime = DateTime.UtcNow;
            var requestId = Guid.NewGuid().ToString();
            
            _logger.LogInformation(
                "Request {RequestId} {Method} {Path} started at {RequestTime}",
                requestId, context.Request.Method, context.Request.Path, requestTime);

            // Capture the original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create a new memory stream to capture the response
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Continue down the middleware pipeline
                await _next(context);

                // Log the response
                var responseTime = DateTime.UtcNow;
                var elapsed = responseTime - requestTime;

                _logger.LogInformation(
                    "Response {RequestId} {Method} {Path} completed with status {StatusCode} in {ElapsedMs}ms",
                    requestId, context.Request.Method, context.Request.Path, 
                    context.Response.StatusCode, elapsed.TotalMilliseconds);

                // Copy the response body to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
            }
        }
    }

    // Extension method to add the middleware to the request pipeline
    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
