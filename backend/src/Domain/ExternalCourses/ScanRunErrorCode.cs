namespace StudyOrganizer.Domain.ExternalCourses;

public enum ScanRunErrorCode
{
    SourceUnreachable = 0,
    AccessDenied = 1,
    Timeout = 2,
    InvalidSourceData = 3,
    PersistenceConflict = 4,
    Unexpected = 5
}
