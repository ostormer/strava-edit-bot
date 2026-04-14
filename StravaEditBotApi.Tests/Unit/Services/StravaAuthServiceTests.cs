using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class StravaAuthServiceTests
{
    private IConfiguration _config = null!;
    private StubHttpMessageHandler _handler = null!;
    private StravaAuthService _sut = null!;

    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";

    [SetUp]
    public void SetUp()
    {
        _config = Substitute.For<IConfiguration>();
        _config["Strava:ClientId"].Returns(ClientId);
        _config["Strava:ClientSecret"].Returns(ClientSecret);
        BuildSut(MakeSuccessResponse());
    }

    private void BuildSut(HttpResponseMessage response)
    {
        _handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(_handler);
        _sut = new StravaAuthService(httpClient, _config);
    }

    // ========================================================
    // ExchangeCodeAsync
    // ========================================================

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectAthleteId()
    {
        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.AthleteId, Is.EqualTo(12345L));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectAccessToken()
    {
        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.AccessToken, Is.EqualTo("test-access-token"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectRefreshToken()
    {
        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.RefreshToken, Is.EqualTo("test-refresh-token"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ConvertsUnixExpiresAtToUtcDateTime()
    {
        long unixExpiry = 1700000000L;
        BuildSut(MakeSuccessResponse(expiresAt: unixExpiry));
        var expected = DateTimeOffset.FromUnixTimeSeconds(unixExpiry).UtcDateTime;

        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.ExpiresAt, Is.EqualTo(expected));
    }

    [Test]
    public async Task ExchangeCodeAsync_AnyCode_PostsToStravaTokenEndpoint()
    {
        await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(
            _handler.CapturedRequest!.RequestUri!.AbsoluteUri,
            Is.EqualTo("https://www.strava.com/oauth/token"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidCode_SendsCodeInFormBody()
    {
        await _sut.ExchangeCodeAsync("my-auth-code");

        string body = await _handler.CapturedRequest!.Content!.ReadAsStringAsync();

        Assert.That(body, Does.Contain("code=my-auth-code"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidCode_SendsClientIdInFormBody()
    {
        await _sut.ExchangeCodeAsync("auth-code");

        string body = await _handler.CapturedRequest!.Content!.ReadAsStringAsync();

        Assert.That(body, Does.Contain($"client_id={ClientId}"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidCode_SendsClientSecretInFormBody()
    {
        await _sut.ExchangeCodeAsync("auth-code");

        string body = await _handler.CapturedRequest!.Content!.ReadAsStringAsync();

        Assert.That(body, Does.Contain($"client_secret={ClientSecret}"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidCode_SendsGrantTypeAuthorizationCode()
    {
        await _sut.ExchangeCodeAsync("auth-code");

        string body = await _handler.CapturedRequest!.Content!.ReadAsStringAsync();

        Assert.That(body, Does.Contain("grant_type=authorization_code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingClientId_ThrowsInvalidOperationException()
    {
        _config["Strava:ClientId"].Returns((string?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingClientSecret_ThrowsInvalidOperationException()
    {
        _config["Strava:ClientSecret"].Returns((string?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_HttpFailure_ThrowsHttpRequestException()
    {
        BuildSut(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"Unauthorized\"}"),
        });

        Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MalformedJson_ThrowsJsonException()
    {
        BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json"),
        });

        // JsonNode.Parse throws JsonReaderException (subclass of JsonException)
        Assert.CatchAsync<JsonException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingAthleteId_ThrowsJsonException()
    {
        BuildSut(MakeSuccessResponse(includeAthleteId: false));

        Assert.ThrowsAsync<JsonException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingAccessToken_ThrowsJsonException()
    {
        BuildSut(MakeSuccessResponse(includeAccessToken: false));

        Assert.ThrowsAsync<JsonException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingRefreshToken_ThrowsJsonException()
    {
        BuildSut(MakeSuccessResponse(includeRefreshToken: false));

        Assert.ThrowsAsync<JsonException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    [Test]
    public void ExchangeCodeAsync_MissingExpiresAt_ThrowsJsonException()
    {
        BuildSut(MakeSuccessResponse(includeExpiresAt: false));

        Assert.ThrowsAsync<JsonException>(
            () => _sut.ExchangeCodeAsync("auth-code"));
    }

    // ========================================================
    // Helpers
    // ========================================================

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectFirstname()
    {
        BuildSut(MakeSuccessResponse(firstName: "Jane"));

        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.Firstname, Is.EqualTo("Jane"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectLastname()
    {
        BuildSut(MakeSuccessResponse(lastName: "Smith"));

        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.Lastname, Is.EqualTo("Smith"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectProfileMedium()
    {
        BuildSut(MakeSuccessResponse(profileMedium: "https://example.com/medium.jpg"));

        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.ProfileMedium, Is.EqualTo("https://example.com/medium.jpg"));
    }

    [Test]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsCorrectProfile()
    {
        BuildSut(MakeSuccessResponse(profile: "https://example.com/large.jpg"));

        var result = await _sut.ExchangeCodeAsync("auth-code");

        Assert.That(result.Profile, Is.EqualTo("https://example.com/large.jpg"));
    }

    private static HttpResponseMessage MakeSuccessResponse(
        long athleteId = 12345L,
        string firstName = "John",
        string lastName = "Doe",
        string profileMedium = "https://example.com/medium.jpg",
        string profile = "https://example.com/large.jpg",
        string accessToken = "test-access-token",
        string refreshToken = "test-refresh-token",
        long expiresAt = 1700000000L,
        bool includeAthleteId = true,
        bool includeAccessToken = true,
        bool includeRefreshToken = true,
        bool includeExpiresAt = true)
    {
        var obj = new Dictionary<string, object?>();

        if (includeAthleteId)
        {
            obj["athlete"] = new Dictionary<string, object?>
            {
                ["id"] = athleteId,
                ["firstname"] = firstName,
                ["lastname"] = lastName,
                ["profile_medium"] = profileMedium,
                ["profile"] = profile,
            };
        }

        if (includeAccessToken)
        {
            obj["access_token"] = accessToken;
        }

        if (includeRefreshToken)
        {
            obj["refresh_token"] = refreshToken;
        }

        if (includeExpiresAt)
        {
            obj["expires_at"] = expiresAt;
        }

        string json = JsonSerializer.Serialize(obj);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(response);
        }
    }
}
