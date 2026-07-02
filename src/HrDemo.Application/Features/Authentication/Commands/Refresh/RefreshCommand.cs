using Mediator;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Features.Authentication.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<ResponseResult<LoginResponseDto>>;
