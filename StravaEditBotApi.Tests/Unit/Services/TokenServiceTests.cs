using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class TokenServiceTests
{
    private IConfiguration _config = null!;
    private TokenService _sut = null!;

    private const string TestSecret = "test-secret-key-must-be-at-least-32-chars-long!!";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    [SetUp]
    public void SetUp()
    {
        _config = Substitute.For<IConfiguration>();
        _config["Jwt:Secret"].Returns(TestSecret);
        _config["Jwt:Issuer"].Returns(TestIssuer);
        _config["Jwt:Audience"].Returns(TestAudience);
        _sut = new TokenService(_config);
    }

    private static AppUser MakeUser(string? id = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        UserName = "test-user",
    };

    // ========================================================
    // GenerateAccessToken
    // ========================================================

    [Test]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyString()
    {
        string token = _sut.GenerateAccessToken(MakeUser());

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwtStructure()
    {
        string token = _sut.GenerateAccessToken(MakeUser());

        // JWTs have exactly 3 dot-separated segments: header.payload.signature
        Assert.That(token.Split('.'), Has.Length.EqualTo(3));
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ContainsCorrectSubClaim()
    {
        var user = MakeUser(id: "user-123");

        string token = _sut.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Subject, Is.EqualTo("user-123"));
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ContainsJtiClaimThatIsAGuid()
    {
        string token = _sut.GenerateAccessToken(MakeUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);

        Assert.That(jti?.Value, Is.Not.Null.And.Not.Empty);
        Assert.That(Guid.TryParse(jti!.Value, out _), Is.True);
    }

    [Test]
    public void GenerateAccessToken_CalledTwice_ProducesDifferentJti()
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(_sut.GenerateAccessToken(MakeUser()));
        var jwt2 = handler.ReadJwtToken(_sut.GenerateAccessToken(MakeUser()));

        string jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        string jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.That(jti1, Is.Not.EqualTo(jti2));
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ExpiresInApproximately15Minutes()
    {
        var before = DateTime.UtcNow.AddMinutes(14);
        var after = DateTime.UtcNow.AddMinutes(16);

        string token = _sut.GenerateAccessToken(MakeUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.ValidTo, Is.GreaterThan(before).And.LessThan(after));
    }

    [Test]
    public void GenerateAccessToken_ValidUser_HasCorrectIssuer()
    {
        string token = _sut.GenerateAccessToken(MakeUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Issuer, Is.EqualTo(TestIssuer));
    }

    [Test]
    public void GenerateAccessToken_ValidUser_HasCorrectAudience()
    {
        string token = _sut.GenerateAccessToken(MakeUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Audiences, Does.Contain(TestAudience));
    }

    // ========================================================
    // GenerateRefreshToken
    // ========================================================

    [Test]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        string token = _sut.GenerateRefreshToken();

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateRefreshToken_DecodesTo64Bytes()
    {
        string token = _sut.GenerateRefreshToken();
        byte[] bytes = Convert.FromBase64String(token);

        Assert.That(bytes, Has.Length.EqualTo(64));
    }

    [Test]
    public void GenerateRefreshToken_CalledTwice_ReturnsDifferentValues()
    {
        string first = _sut.GenerateRefreshToken();
        string second = _sut.GenerateRefreshToken();

        Assert.That(first, Is.Not.EqualTo(second));
    }

    // ========================================================
    // HashToken
    // ========================================================

    [Test]
    public void HashToken_ReturnsNonEmptyString()
    {
        string hash = _sut.HashToken("some-token");

        Assert.That(hash, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void HashToken_Returns64CharUppercaseHexString()
    {
        string hash = _sut.HashToken("some-token");

        // SHA-256 = 32 bytes = 64 hex chars; Convert.ToHexString returns uppercase
        Assert.That(hash, Has.Length.EqualTo(64));
        Assert.That(hash, Does.Match("^[0-9A-F]+$"));
    }

    [Test]
    public void HashToken_SameInput_ReturnsSameHash()
    {
        string hash1 = _sut.HashToken("same-token");
        string hash2 = _sut.HashToken("same-token");

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void HashToken_DifferentInputs_ReturnDifferentHashes()
    {
        string hash1 = _sut.HashToken("token-a");
        string hash2 = _sut.HashToken("token-b");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void HashToken_EmptyString_ReturnsValidHash()
    {
        string hash = _sut.HashToken("");

        Assert.That(hash, Has.Length.EqualTo(64));
    }
}
