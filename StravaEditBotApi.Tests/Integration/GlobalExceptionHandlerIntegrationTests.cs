using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Tests.Integration;

[TestFixture]
public class GlobalExceptionHandlerIntegrationTests
{
    private ThrowingTokenServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ThrowingTokenServiceWebAppFactory();
        // Disable cookie jar so we can set the Cookie header manually
        // and the response Set-Cookie header is not consumed silently.
        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // POST /api/auth/refresh with a refreshToken cookie causes AuthController
    // to call tokenService.HashToken — which throws an unhandled exception.
    // GlobalExceptionHandler should catch it before ASP.NET returns a plain 500.
    private static HttpRequestMessage MakeRefreshRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=test-token");
        return request;
    }

    // ========================================================
    // Unhandled exception handling
    // ========================================================

    [Test]
    public async Task UnhandledException_Returns500()
    {
        var response = await _client.SendAsync(MakeRefreshRequest());

        Assert.That((int)response.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task UnhandledException_ReturnsApplicationProblemJsonContentType()
    {
        var response = await _client.SendAsync(MakeRefreshRequest());

        Assert.That(response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task UnhandledException_ResponseHasProblemDetailsShape()
    {
        var response = await _client.SendAsync(MakeRefreshRequest());
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.That(doc.RootElement.TryGetProperty("title", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("status", out var statusProp), Is.True);
        Assert.That(statusProp.GetInt32(), Is.EqualTo(500));
    }

    [Test]
    public async Task UnhandledException_InDevelopment_ExposesExceptionMessageInDetail()
    {
        var response = await _client.SendAsync(MakeRefreshRequest());
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.That(doc.RootElement.TryGetProperty("detail", out var detailProp), Is.True);
        Assert.That(detailProp.GetString(), Does.Contain("Simulated unhandled exception"));
    }

    [Test]
    public async Task UnhandledException_InstanceMatchesRequestPath()
    {
        var response = await _client.SendAsync(MakeRefreshRequest());
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.That(doc.RootElement.TryGetProperty("instance", out var instanceProp), Is.True);
        Assert.That(instanceProp.GetString(), Is.EqualTo("/api/auth/refresh"));
    }

    // ========================================================
    // Factory
    // ========================================================

    // Replaces ITokenService with a stub whose HashToken always throws.
    // This causes AuthController.RefreshAsync to throw an unhandled exception
    // without any broad catch block swallowing it first.
    private sealed class ThrowingTokenServiceWebAppFactory : WebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services
                    .Where(s => s.ServiceType == typeof(ITokenService))
                    .ToList())
                {
                    services.Remove(descriptor);
                }

                var throwingService = Substitute.For<ITokenService>();
                throwingService.HashToken(Arg.Any<string>())
                    .Throws(new InvalidOperationException("Simulated unhandled exception"));
                services.AddScoped<ITokenService>(_ => throwingService);
            });
        }
    }
}
