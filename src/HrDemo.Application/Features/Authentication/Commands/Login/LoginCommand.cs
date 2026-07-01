using Mediator;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(string UserNameOrEmail, string Password, string IpAddress = "127.0.0.1") : IRequest<ResponseResult<LoginResponseDto>>;
