using DriveEase.Schools.Domain.Entities;

namespace DriveEase.Schools.Domain.Repositories;

public interface IDrivingSchoolRepository
{
    Task<DrivingSchool?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DrivingSchool>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(DrivingSchool school, CancellationToken cancellationToken = default);
}

public interface IInstructorRepository
{
    Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Instructor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Instructor>> GetAvailableBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task AddAsync(Instructor instructor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Instructor instructor, CancellationToken cancellationToken = default);
}
