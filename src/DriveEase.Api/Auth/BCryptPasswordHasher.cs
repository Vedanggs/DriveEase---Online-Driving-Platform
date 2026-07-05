using StudentsHasher = DriveEase.Students.Application.IPasswordHasher;
using SchoolsHasher = DriveEase.Schools.Application.IPasswordHasher;

namespace DriveEase.Api.Auth;

public sealed class BCryptPasswordHasher : StudentsHasher, SchoolsHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string storedHash) =>
        BCrypt.Net.BCrypt.Verify(password, storedHash);
}