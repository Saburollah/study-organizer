namespace StudyOrganizer.Domain.ExternalCourses;

public enum ExternalContentKind
{
    Assignment = 0,
    Announcement = 1,
    Resource = 2
}

public enum ExternalContentProcessingState
{
    TaskEligible = 0,
    ReviewRequired = 1
}

public enum ExternalContentReviewReason
{
    None = 0,
    NotAnAssignment = 1,
    MissingStructuredDeadline = 2
}

public enum ExternalContentVisibility
{
    Visible = 0,
    NotVisible = 1
}

public enum ScanRunStatus
{
    InProgress = 0,
    Succeeded = 1,
    Failed = 2
}
