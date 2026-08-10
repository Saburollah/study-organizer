using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Domain.Tests.Tasks;

public sealed class StudyTaskTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesOpenTask()
    {
        // Arrange
        var moduleId = Guid.NewGuid();
        var dueDate = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var task = new StudyTask(
            moduleId,
            "Prepare security exercise",
            dueDate);

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(moduleId, task.ModuleId);
        Assert.Equal("Prepare security exercise", task.Title);
        Assert.Equal(dueDate, task.DueDate);
        Assert.Equal(StudyTaskStatus.Open, task.Status);
    }

    [Fact]
    public void Constructor_WithEmptyModuleId_ThrowsArgumentException()
    {
        // Act
        var action = () => new StudyTask(
            Guid.Empty,
            "Prepare exercise",
            DateTimeOffset.UtcNow.AddDays(7));

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("moduleId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act
        var action = () => new StudyTask(
            Guid.NewGuid(),
            "   ",
            DateTimeOffset.UtcNow.AddDays(7));

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsTitle()
    {
        // Act
        var task = new StudyTask(
            Guid.NewGuid(),
            "  Prepare exercise  ",
            DateTimeOffset.UtcNow.AddDays(7));

        // Assert
        Assert.Equal("Prepare exercise", task.Title);
    }

    [Fact]
    public void Constructor_WithDescription_StoresNormalizedDescriptionAndCreationTime()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var task = new StudyTask(
            Guid.NewGuid(),
            "Prepare exercise",
            DateTimeOffset.UtcNow.AddDays(7),
            "  Complete chapter three  ");

        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(
            "Complete chapter three",
            task.Description);

        Assert.InRange(
            task.CreatedAt,
            beforeCreation,
            afterCreation);

        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void Complete_WhenTaskIsOpen_SetsStatusToCompleted()
{
    // Arrange
    var task = new StudyTask(
        Guid.NewGuid(),
        "Prepare exercise",
        DateTimeOffset.UtcNow.AddDays(7));

    var beforeUpdate = DateTimeOffset.UtcNow;

    // Act
    task.Complete();

    var afterUpdate = DateTimeOffset.UtcNow;

    // Assert
    Assert.Equal(StudyTaskStatus.Completed, task.Status);
    Assert.NotNull(task.UpdatedAt);
    Assert.InRange(
        task.UpdatedAt.Value,
        beforeUpdate,
        afterUpdate);
}

    [Fact]
    public void Reopen_WhenTaskIsCompleted_SetsStatusToOpen()
    {
        // Arrange
        var task = new StudyTask(
            Guid.NewGuid(),
            "Prepare exercise",
            DateTimeOffset.UtcNow.AddDays(7));

        task.Complete();
        var beforeReopen = DateTimeOffset.UtcNow;

        // Act
        task.Reopen();

        var afterReopen = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(StudyTaskStatus.Open, task.Status);
        Assert.NotNull(task.UpdatedAt);
        Assert.InRange(
            task.UpdatedAt.Value,
            beforeReopen,
            afterReopen);
    }
}