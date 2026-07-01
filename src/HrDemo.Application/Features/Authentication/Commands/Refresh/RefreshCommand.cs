using Mediator;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Features.Authentication.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken, string IpAddress = "127.0.0.1") : IRequest<ResponseResult<LoginResponseDto>>;
