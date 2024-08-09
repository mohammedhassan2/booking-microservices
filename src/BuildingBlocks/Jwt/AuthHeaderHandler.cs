namespace BuildingBlocks.Jwt;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

public class AuthHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Retrieve the Authorization header from the current HTTP context
        var authorizationHeader = httpContextAccessor?.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            // Remove the "Bearer " prefix and set the Authorization header for the outgoing request
            var token = authorizationHeader.Replace("Bearer ", string.Empty);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Proceed with the request
        return base.SendAsync(request, cancellationToken);
    }
}
