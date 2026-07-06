using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetEnrollmentLessonCount;

public sealed record EnrollmentLessonCountDto(int Completed, int Scheduled);

public sealed record GetEnrollmentLessonCountQuery(Guid EnrollmentId) : IRequest<EnrollmentLessonCountDto>;

public sealed class GetEnrollmentLessonCountHandler(ILessonRepository repository)
    : IRequestHandler<GetEnrollmentLessonCountQuery, EnrollmentLessonCountDto>
{
    // Completed = instructor-confirmed lessons (consume package slots).
    // Scheduled = pending lessons (capped at 2 concurrently by BookLessonHandler).
    public async Task<EnrollmentLessonCountDto> Handle(GetEnrollmentLessonCountQuery request, CancellationToken cancellationToken)
    {
        var completed = await repository.CountCompletedByEnrollmentAsync(request.EnrollmentId, cancellationToken);
        var scheduled = await repository.CountScheduledByEnrollmentAsync(request.EnrollmentId, cancellationToken);
        return new EnrollmentLessonCountDto(completed, scheduled);
    }
}
