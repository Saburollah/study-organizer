using System.ComponentModel.DataAnnotations;

namespace StudyOrganizer.Api.Modules;

public sealed class CreateModuleRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Code { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [RegularExpression(
        "^#[0-9A-Fa-f]{6}$",
        ErrorMessage =
            "Color must use the format #RRGGBB.")]
    public string? Color { get; init; }
}

public sealed record ModuleResponse(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    string? Color,
    DateTimeOffset CreatedAtUtc);
