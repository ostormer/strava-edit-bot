using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class ActivitiesControllerTests
{
    private IActivityService _service = null!;
    private ActivitiesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IActivityService>();
        _sut = new ActivitiesController(
            _service,
            Substitute.For<ILogger<ActivitiesController>>()
        );
    }

    private static CreateActivityDto MakeCreateDto(string name = "Test Run") =>
        new(
            Name: name,
            Description: "desc",
            ActivitySport: "Run",
            StartTime: DateTime.UtcNow.AddHours(-1),
            Distance: 5.0,
            ElapsedTime: TimeSpan.FromMinutes(30)
        );

    private static UpdateActivityDto MakeUpdateDto(string name = "Updated Run") =>
        new(
            Name: name,
            Description: "updated desc",
            ActivitySport: "Run",
            StartTime: DateTime.UtcNow.AddHours(-2),
            Distance: 10.0,
            ElapsedTime: TimeSpan.FromMinutes(60)
        );

    private static Activity MakeActivity(int id = 1, string name = "Test Run") =>
        new(name, "desc", "Run", DateTime.UtcNow.AddHours(-1), 5.0, TimeSpan.FromMinutes(30))
        {
            Id = id
        };

    // ========================================================
    // GetAll
    // ========================================================

    [Test]
    public async Task GetAll_ReturnsOkWithActivities()
    {
        var activities = new List<Activity>
        {
            MakeActivity(1, "Run 1"),
            MakeActivity(2, "Run 2")
        };
        _service.GetAllAsync().Returns(activities);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.AssignableTo<IEnumerable<Activity>>());
        var returned = (IEnumerable<Activity>)okResult.Value!;
        Assert.That(returned, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyCollection()
    {
        _service.GetAllAsync().Returns(new List<Activity>());

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.AssignableTo<IEnumerable<Activity>>());
        var returned = (IEnumerable<Activity>)okResult.Value!;
        Assert.That(returned, Is.Empty);
    }

    // ========================================================
    // GetById
    // ========================================================

    [Test]
    public async Task GetById_ExistingId_ReturnsOkWithActivity()
    {
        var activity = MakeActivity(42, "Found Me");
        _service.GetByIdAsync(42).Returns(activity);

        var result = await _sut.GetByIdAsync(42);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.InstanceOf<Activity>());
        var returned = (Activity)okResult.Value!;
        Assert.That(returned.Name, Is.EqualTo("Found Me"));
        Assert.That(returned.Id, Is.EqualTo(42));
    }

    [Test]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        _service.GetByIdAsync(999).Returns((Activity?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ========================================================
    // Create
    // ========================================================

    [Test]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = MakeCreateDto("New Run");
        var created = MakeActivity(7, "New Run");
        _service.CreateAsync(dto).Returns(created);

        var result = await _sut.CreateAsync(dto);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result;
        Assert.That(createdResult.StatusCode, Is.EqualTo(201));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(ActivitiesController.GetByIdAsync)));
        Assert.That(createdResult.RouteValues!["id"], Is.EqualTo(7));
        Assert.That(createdResult.Value, Is.InstanceOf<Activity>());
        var returned = (Activity)createdResult.Value!;
        Assert.That(returned.Name, Is.EqualTo("New Run"));
    }

    [Test]
    public async Task Create_ValidDto_CallsServiceExactlyOnce()
    {
        var dto = MakeCreateDto();
        _service.CreateAsync(dto).Returns(MakeActivity());

        await _sut.CreateAsync(dto);

        await _service.Received(1).CreateAsync(dto);
    }

    // ========================================================
    // Put (Update)
    // ========================================================

    [Test]
    public async Task Put_ExistingId_ReturnsNoContent()
    {
        var dto = MakeUpdateDto("Updated");
        _service.UpdateAsync(1, dto).Returns(true);

        var result = await _sut.PutAsync(1, dto);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Put_NonExistentId_ReturnsNotFound()
    {
        var dto = MakeUpdateDto();
        _service.UpdateAsync(999, dto).Returns(false);

        var result = await _sut.PutAsync(999, dto);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Put_CallsServiceWithCorrectIdAndDto()
    {
        var dto = MakeUpdateDto("Specific Name");
        _service.UpdateAsync(Arg.Any<int>(), Arg.Any<UpdateActivityDto>()).Returns(true);

        await _sut.PutAsync(42, dto);

        await _service.Received(1).UpdateAsync(42, dto);
    }

    // ========================================================
    // Delete
    // ========================================================

    [Test]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _service.DeleteAsync(1).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        _service.DeleteAsync(999).Returns(false);

        var result = await _sut.DeleteAsync(999);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        _service.DeleteAsync(Arg.Any<int>()).Returns(true);

        await _sut.DeleteAsync(42);

        await _service.Received(1).DeleteAsync(42);
    }
}
