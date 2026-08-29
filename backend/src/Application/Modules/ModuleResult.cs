namespace StudyOrganizer.Application.Modules;

public sealed record ModuleResult(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    string? Color,
    DateTimeOffset CreatedAt,
    bool IsExternalCourseLinked = false);
