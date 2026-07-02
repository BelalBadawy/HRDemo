using Mediator;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<ResponseResult>;
