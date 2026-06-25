using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetInstructorLessons;

public sealed class GetInstructorLessonsHandler(ILessonRepository repository)
    : IRequestHandler<GetInstructorLessonsQuery, IReadOnlyList<InstructorLessonDto>>
{
    public async Task<IReadOnlyList<InstructorLessonDto>> Handle(
        GetInstructorLessonsQuery request, CancellationToken cancellationToken)
    {
        var lessons = await repository.GetByInstructorAsync(request.InstructorId, cancellationToken);

        return lessons
            .Select(l => new InstructorLessonDto(
                l.Id,
                l.StudentId,
                l.StudentName,
                l.ScheduledAt,
                l.Duration,
                l.Status.ToString(),
                l.Notes,
                l.CompletedAt))
            .ToList();
    }
}
