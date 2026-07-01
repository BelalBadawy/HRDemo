using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace HrDemo.API.FunctionalTests;

public sealed class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GivenLiveEndpoint_WhenCalled_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
