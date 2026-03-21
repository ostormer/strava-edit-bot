using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Tests.Integration;

[TestFixture]
public class ActivitiesIntegrationTests
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebAppFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public void SetUp()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Activities.RemoveRange(db.Activities);
        db.SaveChanges();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static CreateActivityDto ValidDto(string name = "Morning Run") => new(
        Name: name,
        Description: "A nice run",
        ActivitySport: "Run",
        StartTime: DateTime.UtcNow.AddHours(-1),
        Distance: 5.0,
        ElapsedTime: TimeSpan.FromMinutes(30)
    );

    // ========================================================
    // POST
    // ========================================================

    [Test]
    public async Task Post_ValidActivity_Returns201WithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/activities", ValidDto());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);
    }

    [Test]
    public async Task Post_ValidActivity_ReturnsCreatedActivityInBody()
    {
        var response = await _client.PostAsJsonAsync("/api/activities", ValidDto("Test Run"));
        var activity = await response.Content.ReadFromJsonAsync<Activity>();

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Id, Is.GreaterThan(0));
        Assert.That(activity.Name, Is.EqualTo("Test Run"));
    }

    [Test]
    public async Task Post_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/activities", ValidDto(name: ""));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Post_InvalidSport_Returns400()
    {
        var dto = new CreateActivityDto(
            Name: "Test",
            Description: null,
            ActivitySport: "Surfing",
            StartTime: DateTime.UtcNow.AddHours(-1),
            Distance: 5.0,
            ElapsedTime: TimeSpan.FromMinutes(30)
        );

        var response = await _client.PostAsJsonAsync("/api/activities", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ========================================================
    // GET all
    // ========================================================

    [Test]
    public async Task GetAll_EmptyDatabase_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/activities");
        var activities = await response.Content.ReadFromJsonAsync<List<Activity>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task GetAll_WithActivities_ReturnsAll()
    {
        await _client.PostAsJsonAsync("/api/activities", ValidDto("Run 1"));
        await _client.PostAsJsonAsync("/api/activities", ValidDto("Run 2"));

        var response = await _client.GetAsync("/api/activities");
        var activities = await response.Content.ReadFromJsonAsync<List<Activity>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(activities, Has.Count.EqualTo(2));
    }
    // ========================================================
    // GET by id
    // ========================================================

    [Test]
    public async Task GetById_ExistingId_Returns200WithActivity()
    {
        var created = await (await _client.PostAsJsonAsync("/api/activities", ValidDto("Find Me")))
            .Content.ReadFromJsonAsync<Activity>();

        var response = await _client.GetAsync($"/api/activities/{created!.Id}");
        var activity = await response.Content.ReadFromJsonAsync<Activity>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(activity!.Name, Is.EqualTo("Find Me"));
    }

    [Test]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/activities/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ========================================================
    // PUT
    // ========================================================

    [Test]
    public async Task Put_ExistingActivity_Returns204()
    {
        var created = await (await _client.PostAsJsonAsync("/api/activities", ValidDto()))
            .Content.ReadFromJsonAsync<Activity>();

        var updateDto = new UpdateActivityDto(Name: "Updated", null, null, null, null, null);
        var response = await _client.PutAsJsonAsync($"/api/activities/{created!.Id}", updateDto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Put_NonExistentId_Returns404()
    {
        var updateDto = new UpdateActivityDto(Name: "Updated", null, null, null, null, null);
        var response = await _client.PutAsJsonAsync("/api/activities/999", updateDto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ========================================================
    // DELETE
    // ========================================================

    [Test]
    public async Task Delete_ExistingActivity_Returns204()
    {
        var created = await (await _client.PostAsJsonAsync("/api/activities", ValidDto()))
            .Content.ReadFromJsonAsync<Activity>();

        var response = await _client.DeleteAsync($"/api/activities/{created!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/activities/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ========================================================
    // Error handling
    // ========================================================

    [Test]
    public async Task TestError_Returns500WithProblemDetailsContentType()
    {
        var response = await _client.GetAsync("/api/activities/test-error");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/problem+json"));
    }
}