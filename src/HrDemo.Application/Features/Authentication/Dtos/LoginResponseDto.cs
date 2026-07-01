namespace HrDemo.Application.Features.Authentication.Dtos;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
