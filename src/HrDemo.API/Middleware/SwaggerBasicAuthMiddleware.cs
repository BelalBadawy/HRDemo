using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HrDemo.API.Middleware;

public sealed class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path != null && path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsAuthenticated(context))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"HrDemo Swagger\"");
                return;
            }
        }

        await _next(context);
    }

    private bool IsAuthenticated(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }

        try
        {
            var headerValue = AuthenticationHeaderValue.Parse(authHeader!);
            if (!"Basic".Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (headerValue.Parameter == null)
            {
                return false;
            }

            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter)).Split(':', 2);
            if (credentials.Length != 2)
            {
                return false;
            }

            var username = credentials[0];
            var password = credentials[1];

            var expectedUsername = _configuration["SwaggerAuth:Username"];
            var expectedPassword = _configuration["SwaggerAuth:Password"];

            if (string.IsNullOrEmpty(expectedUsername) || string.IsNullOrEmpty(expectedPassword))
            {
                return false;
            }

            return username == expectedUsername && password == expectedPassword;
        }
        catch
        {
            return false;
        }
    }
}
