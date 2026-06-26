using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetStudentLessons;

public sealed class GetStudentLessonsHandler(ILessonRepository repository)
    : IRequestHandler<GetStudentLessonsQuery, IReadOnlyList<StudentLessonDto>>
{
    public async Task<IReadOnlyList<StudentLessonDto>> Handle(
        GetStudentLessonsQuery request, CancellationToken cancellationToken)
    {
        var lessons = await repository.GetAllByStudentAsync(request.StudentId, cancellationToken);

        return lessons
            .Select(l => new StudentLessonDto(
                l.Id,
                l.EnrollmentId,
                l.InstructorId,
                l.ScheduledAt,
                l.Duration,
                l.Status.ToString(),
                l.Notes,
                l.CompletedAt,
                l.InstructorName))
            .ToList();
    }
}
