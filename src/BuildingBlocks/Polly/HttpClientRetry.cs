namespace BuildingBlocks.Polly;

using System.Net;
using Ardalis.GuardClauses;
using BuildingBlocks.Web;
using global::Polly;
using global::Polly.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class HttpClientRetry
{
    // Reference: https://anthonygiretti.com/2019/03/26/best-practices-with-httpclient-and-retry-policies-with-polly-in-net-core-2-part-2/
    public static IHttpClientBuilder AddHttpClientRetryPolicyHandler(this IHttpClientBuilder httpClientBuilder)
    {
        return httpClientBuilder.AddPolicyHandler((sp, _) =>
        {
            // Retrieve policy options from configuration
            var options = sp.GetRequiredService<IConfiguration>()
                            .GetOptions<PolicyOptions>(nameof(PolicyOptions));

            // Ensure that options are not null
            Guard.Against.Null(options, nameof(options));

            // Create a logger for retry events
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("PollyHttpClientRetryPoliciesLogger");

            // Configure Polly retry policy
            return HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.BadRequest) // Handle BadRequest as a transient fault
                .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError) // Handle InternalServerError as a transient fault
                .WaitAndRetryAsync(
                    retryCount: options.Retry.RetryCount, // Number of retry attempts
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(options.Retry.SleepDuration), // Duration between retries
                    onRetry: (response, timeSpan, retryCount, context) =>
                    {
                        if (response?.Exception != null)
                        {
                            // Log errors on request failure
                            logger.LogError(response.Exception,
                                "Request failed with {StatusCode}. Waiting {TimeSpan} before next retry. Retry attempt {RetryCount}.",
                                response.Result.StatusCode,
                                timeSpan,
                                retryCount);
                        }
                    });
        });
    }
}
