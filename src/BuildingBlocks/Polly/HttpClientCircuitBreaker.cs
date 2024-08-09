namespace BuildingBlocks.Polly;

using System.Net;
using Ardalis.GuardClauses;
using BuildingBlocks.Web;
using global::Polly;
using global::Polly.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

public static class HttpClientCircuitBreaker
{
    // Reference: https://anthonygiretti.com/2019/03/26/best-practices-with-httpclient-and-retry-policies-with-polly-in-net-core-2-part-2/
    public static IHttpClientBuilder AddHttpClientCircuitBreakerPolicyHandler(this IHttpClientBuilder httpClientBuilder)
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
            var logger = loggerFactory.CreateLogger("PollyHttpClientCircuitBreakerPoliciesLogger");

            // Configure Polly Circuit Breaker policy
            return HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.BadRequest) // Handle BadRequest as a transient fault
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.CircuitBreaker.RetryCount, // Number of allowed failed attempts before breaking
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDuration), // Duration of the circuit break
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
