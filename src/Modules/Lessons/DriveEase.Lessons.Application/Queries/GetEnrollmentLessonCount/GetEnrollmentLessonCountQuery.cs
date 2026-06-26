using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetEnrollmentLessonCount;

public sealed record GetEnrollmentLessonCountQuery(Guid EnrollmentId) : IRequest<int>;

public sealed class GetEnrollmentLessonCountHandler(ILessonRepository repository)
    : IRequestHandler<GetEnrollmentLessonCountQuery, int>
{
    // Returns only instructor-confirmed (Completed) lessons — not Scheduled or Cancelled.
    public Task<int> Handle(GetEnrollmentLessonCountQuery request, CancellationToken cancellationToken)
        => repository.CountCompletedByEnrollmentAsync(request.EnrollmentId, cancellationToken);
}
