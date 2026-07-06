namespace DriveEase.Notifications.Domain.Entities;

public sealed class InstructorNotification
{
    public Guid Id { get; private set; }
    public Guid InstructorId { get; private set; }
    public string Type { get; private set; } = string.Empty;       // "enrollment" | "lesson"
    public string StudentName { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public DateTime? ScheduledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    private InstructorNotification() { }

    public static InstructorNotification Create(
        Guid instructorId, string type, string studentName, string detail, DateTime? scheduledAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            Type = type,
            StudentName = studentName,
            Detail = detail,
            ScheduledAt = scheduledAt,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

    public void MarkRead() => IsRead = true;
}
