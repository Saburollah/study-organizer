using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.Modules;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Modules;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalSourceProtectionTests
{
    [Fact]
    public async Task UpdateAsync_LinkedTask_ReturnsExternallyManagedAndKeepsValues()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var original = await setup.ReloadTaskAsync(setup.TaskIds[0]);

        var result = await setup.TaskHandler.UpdateAsync(
            setup.OwnerIds[0],
            setup.ModuleIds[0],
            setup.TaskIds[0],
            "Local override",
            setup.DueDate.AddDays(1),
            "Local description");

        var persisted = await setup.ReloadTaskAsync(setup.TaskIds[0]);
        Assert.Equal(StudyTaskMutationOutcome.ExternallyManaged, result.Outcome);
        Assert.Null(result.Task);
        Assert.Equal(original.Title, persisted.Title);
        Assert.Equal(original.Description, persisted.Description);
        Assert.Equal(original.DueDate, persisted.DueDate);
    }

    [Fact]
    public async Task DeleteAsync_LinkedTask_ReturnsExternallyManagedAndKeepsTask()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);

        var result = await setup.TaskHandler.DeleteAsync(
            setup.OwnerIds[0],
            setup.ModuleIds[0],
            setup.TaskIds[0]);

        Assert.Equal(StudyTaskMutationOutcome.ExternallyManaged, result.Outcome);
        Assert.Null(result.Task);
        Assert.NotNull(await setup.ReloadTaskAsync(setup.TaskIds[0]));
    }

    [Fact]
    public async Task SetStatusAsync_LinkedTask_RemainsAllowed()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);

        var result = await setup.TaskHandler.SetStatusAsync(
            setup.OwnerIds[0],
            setup.ModuleIds[0],
            setup.TaskIds[0],
            StudyTaskStatus.Completed);

        Assert.NotNull(result);
        Assert.Equal(StudyTaskStatus.Completed, result.Status);
        Assert.Equal(
            StudyTaskStatus.Completed,
            (await setup.ReloadTaskAsync(setup.TaskIds[0])).Status);
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_ManualTask_StillSucceed()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var moduleHandler = new ModuleHandler(setup.Database.Context);
        var module = await moduleHandler.CreateAsync(
            setup.OwnerIds[0],
            "Manual module",
            null,
            null,
            null);
        var task = await setup.TaskHandler.CreateAsync(
            setup.OwnerIds[0],
            module.Id,
            "Manual task",
            setup.Database.Now.AddDays(2),
            null);

        var updated = await setup.TaskHandler.UpdateAsync(
            setup.OwnerIds[0],
            module.Id,
            task!.Id,
            "Updated manual task",
            setup.Database.Now.AddDays(3),
            "Updated locally");
        var deleted = await setup.TaskHandler.DeleteAsync(
            setup.OwnerIds[0],
            module.Id,
            task.Id);

        Assert.Equal(StudyTaskMutationOutcome.Succeeded, updated.Outcome);
        Assert.Equal("Updated manual task", updated.Task!.Title);
        Assert.Null(updated.Task.ExternalSource);
        Assert.Equal(StudyTaskMutationOutcome.Succeeded, deleted.Outcome);
        Assert.False(await setup.Database.Context.Tasks.AnyAsync(item => item.Id == task.Id));
    }

    [Fact]
    public async Task DeleteAsync_LinkedModule_ReturnsLinkedOutcomeAndKeepsModule()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var handler = new ModuleHandler(setup.Database.Context);

        var result = await handler.DeleteAsync(
            setup.OwnerIds[0],
            setup.ModuleIds[0]);

        Assert.Equal(ModuleDeleteOutcome.LinkedToExternalCourse, result);
        Assert.True(await setup.Database.Context.Modules.AnyAsync(
            module => module.Id == setup.ModuleIds[0]));
    }

    [Fact]
    public async Task Queries_ExposeTaskSourceAndLinkedModuleMetadata()
    {
        await using var setup = await ExternalCourseScenario.CreateScannedAsync(
            subscriberCount: 1);
        var moduleHandler = new ModuleHandler(setup.Database.Context);

        var tasks = await setup.TaskHandler.GetByModuleAsync(
            setup.OwnerIds[0],
            setup.ModuleIds[0]);
        var modules = await moduleHandler.GetByOwnerAsync(setup.OwnerIds[0]);

        var source = Assert.Single(tasks!).ExternalSource;
        Assert.NotNull(source);
        Assert.Equal("mock-moodle", source.ProviderKey);
        Assert.Equal("Software Engineering", source.CourseName);
        Assert.Equal(
            "https://mock-moodle.local/content/exercise-1",
            source.SourceUrl);
        Assert.True(Assert.Single(modules).IsExternalCourseLinked);
    }
}
