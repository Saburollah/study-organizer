using StudyOrganizer.Domain.Modules;

namespace StudyOrganizer.Domain.Tests.Modules;

public sealed class StudyModuleTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesModule()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var module = new StudyModule(ownerId, "Secure Systems");

        // Assert
        Assert.NotEqual(Guid.Empty, module.Id);
        Assert.Equal(ownerId, module.OwnerId);
        Assert.Equal("Secure Systems", module.Name);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsName()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var module = new StudyModule(ownerId, "  Secure Systems  ");

        // Assert
        Assert.Equal("Secure Systems", module.Name);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var action = () => new StudyModule(ownerId, "   ");

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyOwnerId_ThrowsArgumentException()
    {
        // Act
        var action = () => new StudyModule(
            Guid.Empty,
            "Secure Systems");

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("ownerId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithOptionalValues_StoresValues()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var module = new StudyModule(
            ownerId,
            "Secure Systems",
            "SEC-101",
            "Introduction to secure software systems",
            "#3366FF");

        // Assert
        Assert.Equal("SEC-101", module.Code);
        Assert.Equal(
            "Introduction to secure software systems",
            module.Description);
        Assert.Equal("#3366FF", module.Color);
    }

    [Fact]
    public void Constructor_SetsCreationTime()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var module = new StudyModule(
            Guid.NewGuid(),
            "Secure Systems");

        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.InRange(
            module.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void Update_WithValidValues_UpdatesAndNormalizesModule()
    {
        // Arrange
        var module = new StudyModule(
            Guid.NewGuid(),
            "Altes Modul",
            "ALT",
            "Alte Beschreibung",
            "#000000");

        // Act
        module.Update(
            "  Neues Modul  ",
            "  NEU  ",
            "  Neue Beschreibung  ",
            "  #3366FF  ");

        // Assert
        Assert.Equal("Neues Modul", module.Name);
        Assert.Equal("NEU", module.Code);
        Assert.Equal(
            "Neue Beschreibung",
            module.Description);
        Assert.Equal("#3366FF", module.Color);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var module = new StudyModule(
            Guid.NewGuid(),
            "Secure Systems");

        // Act
        var action = () => module.Update(
            "   ",
            null,
            null,
            null);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }
}
