using System.ComponentModel.DataAnnotations;

namespace ScholarNotify.ViewModels;

public sealed class MonitorInput : IValidatableObject
{
    [Required, Url, Display(Name = "Google Scholar profile URL")]
    public string ScholarUrl { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(-12, 14), Display(Name = "UTC offset")]
    public int UtcOffsetHours { get; set; } = -6;

    [Range(0, 23), Display(Name = "Notify from")]
    public int NotificationStartHour { get; set; } = 9;

    [Range(1, 24), Display(Name = "Notify until")]
    public int NotificationEndHour { get; set; } = 17;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NotificationEndHour <= NotificationStartHour)
        {
            yield return new ValidationResult(
                "The notification end time must be after the start time.",
                [nameof(NotificationEndHour)]);
        }
    }
}