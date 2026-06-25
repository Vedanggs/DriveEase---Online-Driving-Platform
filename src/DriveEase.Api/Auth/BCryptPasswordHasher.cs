using DriveEase.Students.Application;

namespace DriveEase.Api.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string storedHash) =>
        BCrypt.Net.BCrypt.Verify(password, storedHash);
}