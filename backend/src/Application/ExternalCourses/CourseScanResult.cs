using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public sealed record CourseScanResult(
    Guid ScanRunId,
    ScanRunStatus Status,
    ScanRunCounts Counts,
    ScanRunErrorCode? ErrorCode,
    bool ReusedExistingRun);
