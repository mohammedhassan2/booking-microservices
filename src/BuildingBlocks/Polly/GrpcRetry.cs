namespace BuildingBlocks.Polly;

using System.Net;
using Ardalis.GuardClauses;
using BuildingBlocks.Web;
using global::Polly;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class GrpcRetry
{
    // Reference: https://anthonygiretti.com/2020/03/31/grpc-asp-net-core-3-1-resiliency-with-polly/
    public static IHttpClientBuilder AddGrpcRetryPolicyHandler(this IHttpClientBuilder httpClientBuilder)
    {
        return httpClientBuilder.AddPolicyHandler((sp, _) =>
        {
            // Retrieve retry policy options from configuration
            var options = sp.GetRequiredService<IConfiguration>()
                            .GetOptions<PolicyOptions>(nameof(PolicyOptions));

            // Ensure that options are not null
            Guard.Against.Null(options, nameof(options));

            // Configure Polly retry policy
            return Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode) // Handle unsuccessful HTTP responses
                .WaitAndRetryAsync(
                    options.Retry.RetryCount, // Number of retry attempts
                    retryAttempt => TimeSpan.FromSeconds(options.Retry.SleepDuration), // Wait time between retries
                    onRetry: (response, timeSpan, retryCount, context) =>
                    {
                        if (response?.Exception != null)
                        {
                            // Log errors on request failure
                            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger("PollyGrpcRetryPoliciesLogger");

                            logger.LogError(response.Exception,
                                "Request failed with {StatusCode}. Waiting {TimeSpan} before next retry. Retry attempt {RetryCount}.",
                                response.Result?.StatusCode,
                                timeSpan,
                                retryCount);
                        }
                    });
        });
    }
}
