using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed class BookLessonHandler(
    ILessonRepository repository) : IRequestHandler<BookLessonCommand, Guid>
{
    public async Task<Guid> Handle(BookLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = Lesson.Book(
            request.EnrollmentId,
            request.StudentId,
            request.InstructorId,
            request.ScheduledAt,
            request.Duration);

        await repository.AddAsync(lesson, cancellationToken);
        // OutboxInterceptor captures LessonBookedEvent atomically during AddAsync

        return lesson.Id;
    }
}
