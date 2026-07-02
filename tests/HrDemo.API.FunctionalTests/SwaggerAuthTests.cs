using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HrDemo.API.FunctionalTests;

public sealed class SwaggerAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerAuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        }).CreateClient();
    }

    [Fact]
    public async Task GivenSwaggerUiEndpoint_WhenAccessedWithoutCredentials_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/swagger/index.html", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains("WWW-Authenticate").Should().BeTrue();
        var authHeader = response.Headers.WwwAuthenticate.ToString();
        authHeader.Should().Contain("Basic");
        authHeader.Should().Contain("HrDemo Swagger");
    }

    [Fact]
    public async Task GivenSwaggerJsonEndpoint_WhenAccessedWithoutCredentials_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains("WWW-Authenticate").Should().BeTrue();
        var authHeader = response.Headers.WwwAuthenticate.ToString();
        authHeader.Should().Contain("Basic");
        authHeader.Should().Contain("HrDemo Swagger");
    }

    [Fact]
    public async Task GivenSwaggerUiEndpoint_WhenAccessedWithCorrectCredentials_ShouldReturnOk()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/swagger/index.html", UriKind.Relative));
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("HrAdmin:HR@20226$"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenSwaggerJsonEndpoint_WhenAccessedWithCorrectCredentials_ShouldReturnOk()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/swagger/v1/swagger.json", UriKind.Relative));
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("HrAdmin:HR@20226$"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenSwaggerUiEndpoint_WhenAccessedWithIncorrectCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/swagger/index.html", UriKind.Relative));
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("HrAdmin:WrongPassword"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
