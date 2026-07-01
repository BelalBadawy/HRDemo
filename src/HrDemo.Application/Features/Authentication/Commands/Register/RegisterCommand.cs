using Mediator;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(string UserName, string Email, string Password) : IRequest<ResponseResult<int>>;
