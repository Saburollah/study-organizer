using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public sealed record CourseSourceItem(
    ExternalContentKey ExternalContentKey,
    ExternalLearningContentType Type,
    string Title,
    DateTimeOffset? DueDate,
    string? MediaType,
    string? SourceReference);
