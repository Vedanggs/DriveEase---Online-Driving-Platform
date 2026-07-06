using DriveEase.Shared.Domain;

namespace DriveEase.Schools.Domain.Entities;

public sealed class Instructor : Entity<Guid>
{
    public Guid SchoolId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool IsAvailable { get; private set; }

    private Instructor() { }

    public static Instructor Create(Guid schoolId, string fullName, string licenseNumber, string? email = null, string? passwordHash = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FullName = fullName,
            LicenseNumber = licenseNumber,
            Email = email,
            PasswordHash = passwordHash,
            IsAvailable = true
        };

    public void SetAvailability(bool available) => IsAvailable = available;

    public void UpdateCredentials(string licenseNumber, string? email, string? passwordHash)
    {
        LicenseNumber = licenseNumber;
        Email = email;
        PasswordHash = passwordHash;
    }
}
