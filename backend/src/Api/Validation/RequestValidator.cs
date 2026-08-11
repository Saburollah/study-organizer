using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.Validation;

public static class RequestValidator
{
    public static Dictionary<string, string[]> Validate(
        object request)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(request);

        Validator.TryValidateObject(
            request,
            context,
            results,
            validateAllProperties: true);

        return results
            .SelectMany(
                result => result.MemberNames
                    .DefaultIfEmpty("request"),
                (result, memberName) => new
                {
                    MemberName = memberName,
                    Message = result.ErrorMessage
                        ?? "Invalid value."
                })
            .GroupBy(item => item.MemberName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Message)
                    .ToArray());
    }
}
