using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCoursePersistenceTests
{
    [Fact]
    public async Task SaveChanges_DuplicateCanonicalCourse_Throws()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        database.Context.ExternalCourses.AddRange(
            new ExternalCourse(
                "mock-moodle",
                "software-engineering-2026",
                "SE",
                database.Now),
            new ExternalCourse(
                "mock-moodle",
                "software-engineering-2026",
                "SE copy",
                database.Now));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_DuplicateOwnerCourseSubscription_Throws()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var course = CreateCourse(database.Now);
        var module = new StudyModule(ownerId, "Software Engineering");
        database.Context.AddRange(course, module);
        database.Context.CourseSubscriptions.AddRange(
            new CourseSubscription(ownerId, course.Id, module.Id, database.Now),
            new CourseSubscription(ownerId, course.Id, module.Id, database.Now));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_DuplicateProviderContent_Throws()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var course = CreateCourse(database.Now);
        database.Context.ExternalCourses.Add(course);
        database.Context.ExternalContents.AddRange(
            CreateContent(course.Id, "assignment-1", "Assignment", database.Now),
            CreateContent(course.Id, "assignment-1", "Assignment copy", database.Now));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_DuplicateSubscriptionContentLink_Throws()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var course = CreateCourse(database.Now);
        var module = new StudyModule(ownerId, "Software Engineering");
        var subscription = new CourseSubscription(
            ownerId,
            course.Id,
            module.Id,
            database.Now);
        var content = CreateContent(
            course.Id,
            "assignment-1",
            "Assignment",
            database.Now);
        var firstTask = new StudyTask(module.Id, "First", database.Now.AddDays(1));
        var secondTask = new StudyTask(module.Id, "Second", database.Now.AddDays(1));
        database.Context.AddRange(
            course,
            module,
            subscription,
            content,
            firstTask,
            secondTask);
        database.Context.ExternalTaskLinks.AddRange(
            new ExternalTaskLink(
                subscription.Id,
                content.Id,
                firstTask.Id,
                database.Now),
            new ExternalTaskLink(
                subscription.Id,
                content.Id,
                secondTask.Id,
                database.Now));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_DuplicateLinkedTask_Throws()
    {
        await using var database = await ExternalCourseTestDatabase.CreateAsync();
        var ownerId = await database.CreateUserAsync("owner@example.com");
        var course = CreateCourse(database.Now);
        var module = new StudyModule(ownerId, "Software Engineering");
        var subscription = new CourseSubscription(
            ownerId,
            course.Id,
            module.Id,
            database.Now);
        var firstContent = CreateContent(
            course.Id,
            "assignment-1",
            "First assignment",
            database.Now);
        var secondContent = CreateContent(
            course.Id,
            "assignment-2",
            "Second assignment",
            database.Now);
        var task = new StudyTask(module.Id, "Study task", database.Now.AddDays(1));
        database.Context.AddRange(
            course,
            module,
            subscription,
            firstContent,
            secondContent,
            task);
        database.Context.ExternalTaskLinks.AddRange(
            new ExternalTaskLink(
                subscription.Id,
                firstContent.Id,
                task.Id,
                database.Now),
            new ExternalTaskLink(
                subscription.Id,
                secondContent.Id,
                task.Id,
                database.Now));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            database.Context.SaveChangesAsync());
    }

    private static ExternalCourse CreateCourse(DateTimeOffset now)
    {
        return new ExternalCourse(
            "mock-moodle",
            "software-engineering-2026",
            "Software Engineering",
            now);
    }

    private static ExternalContent CreateContent(
        Guid courseId,
        string providerContentId,
        string title,
        DateTimeOffset now)
    {
        return ExternalContent.Create(
            courseId,
            providerContentId,
            ExternalContentKind.Assignment,
            title,
            null,
            $"https://moodle.example/{providerContentId}",
            now.AddDays(1),
            ExternalContentProcessingState.TaskEligible,
            ExternalContentReviewReason.None,
            now);
    }
}
