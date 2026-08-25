using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Application.ExternalCourses;

public sealed class ExternalCourseSourceException
    : Exception
{
    public ScanRunErrorCode ErrorCode { get; }

    public ExternalCourseSourceException(
        ScanRunErrorCode errorCode)
        : base("The external course source request failed.")
    {
        if (errorCode == ScanRunErrorCode.PersistenceConflict
            || !Enum.IsDefined(errorCode))
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode));
        }

        ErrorCode = errorCode;
    }
}
