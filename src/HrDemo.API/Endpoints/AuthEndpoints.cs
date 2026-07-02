using Mediator;
using HrDemo.API.Extensions;
using HrDemo.Application.Features.Authentication.Commands.Register;
using HrDemo.Application.Features.Authentication.Commands.Login;
using HrDemo.Application.Features.Authentication.Commands.Refresh;
using HrDemo.Application.Features.Authentication.Commands.Logout;
using HrDemo.Application.Features.Authentication.Commands.AssignRole;
using HrDemo.Application.Features.Authentication.Commands.AssignClaim;
using Microsoft.AspNetCore.Http;

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

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("LoginUser");

        group.MapPost("/refresh", async (RefreshCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RefreshToken");

        group.MapPost("/logout", async (LogoutCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("LogoutUser");

        group.MapPost("/assign-role", async (AssignRoleCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AssignRole")
        .RequireAuthorization("roles.assign");

        group.MapPost("/assign-claim", async (AssignClaimCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AssignClaim")
        .RequireAuthorization("claims.assign");

        return app;
    }
}
