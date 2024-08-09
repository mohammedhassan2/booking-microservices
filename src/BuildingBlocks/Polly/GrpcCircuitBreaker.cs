namespace BuildingBlocks.Polly;

using Ardalis.GuardClauses;
using BuildingBlocks.Web;
using global::Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class GrpcCircuitBreaker
{
    // Reference: https://anthonygiretti.com/2020/03/31/grpc-asp-net-core-3-1-resiliency-with-polly/
    public static IHttpClientBuilder AddGrpcCircuitBreakerPolicyHandler(this IHttpClientBuilder httpClientBuilder)
    {
        return httpClientBuilder.AddPolicyHandler((sp, _) =>
        {
            // Retrieve policy options from configuration
            var options = sp.GetRequiredService<IConfiguration>()
                            .GetOptions<PolicyOptions>(nameof(PolicyOptions));

            // Ensure that options are not null
            Guard.Against.Null(options, nameof(options));

            // Create a logger for circuit breaker events
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("PollyGrpcCircuitBreakerPoliciesLogger");

            // Configure Polly Circuit Breaker policy
            return Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode) // Handle unsuccessful HTTP responses
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.CircuitBreaker.RetryCount, // Number of allowed failures before breaking
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDuration), // Duration of the break
                    onBreak: (response, breakDuration) =>
                    {
                        if (response?.Exception != null)
                        {
                            // Log errors when the circuit breaker is triggered
                            logger.LogError(response.Exception,
                                "Service shut down for {BreakDuration} after {RetryCount} failed retries",
                                breakDuration,
                                options.CircuitBreaker.RetryCount);
                        }
                    },
                    onReset: () =>
                    {
                        // Log information when the circuit breaker resets
                        logger.LogInformation("Service restarted");
                    });
        });
    }
}
