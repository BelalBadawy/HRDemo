using Mediator;
using HrDemo.API.Extensions;
using HrDemo.Application.Features.Authentication.Commands.Register;

namespace HrDemo.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RegisterUser");

        return app;
    }
}
