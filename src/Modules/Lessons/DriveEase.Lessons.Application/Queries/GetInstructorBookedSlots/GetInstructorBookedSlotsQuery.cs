using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetInstructorBookedSlots;

public sealed record BookedSlotDto(DateTime ScheduledAt, TimeSpan Duration);

public sealed record GetInstructorBookedSlotsQuery(Guid InstructorId, DateTime DateUtc)
    : IRequest<IReadOnlyList<BookedSlotDto>>;

public sealed class GetInstructorBookedSlotsHandler(ILessonRepository repository)
    : IRequestHandler<GetInstructorBookedSlotsQuery, IReadOnlyList<BookedSlotDto>>
{
    public async Task<IReadOnlyList<BookedSlotDto>> Handle(
        GetInstructorBookedSlotsQuery request, CancellationToken cancellationToken)
    {
        var lessons = await repository.GetBookedSlotsAsync(
            request.InstructorId, request.DateUtc, cancellationToken);

        return lessons
            .Select(l => new BookedSlotDto(l.ScheduledAt, l.Duration))
            .ToList();
    }
}
