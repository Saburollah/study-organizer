namespace StudyOrganizer.Application.Tasks;

public enum StudyTaskMutationOutcome
{
    Succeeded,
    NotFound,
    ExternallyManaged
}

public sealed record ExternalTaskSourceResult(
    string ProviderKey,
    string CourseName,
    string SourceUrl);

public sealed record StudyTaskMutationResult(
    StudyTaskMutationOutcome Outcome,
    StudyTaskResult? Task);
