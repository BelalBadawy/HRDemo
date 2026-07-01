using Mediator;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken, string IpAddress = "127.0.0.1") : IRequest<ResponseResult>;
