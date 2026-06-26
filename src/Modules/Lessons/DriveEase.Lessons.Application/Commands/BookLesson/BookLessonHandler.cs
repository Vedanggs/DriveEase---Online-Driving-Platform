using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed class BookLessonHandler(
    ILessonRepository repository) : IRequestHandler<BookLessonCommand, Guid>
{
    private const int MaxLessonsPerEnrollment = 5;

    public async Task<Guid> Handle(BookLessonCommand request, CancellationToken cancellationToken)
    {
        // Only instructor-confirmed (Completed) lessons count toward the 5-lesson package.
        // Scheduled (pending) and Cancelled lessons do not consume a slot.
        var completedCount = await repository.CountCompletedByEnrollmentAsync(request.EnrollmentId, cancellationToken);
        if (completedCount >= MaxLessonsPerEnrollment)
            throw new InvalidOperationException(
                $"Lesson package limit reached. Maximum {MaxLessonsPerEnrollment} lessons per enrollment.");

        var conflict = await repository.HasConflictAsync(
            request.InstructorId, request.ScheduledAt, request.Duration, cancellationToken);
        if (conflict)
            throw new InvalidOperationException(
                $"{request.InstructorName} is already booked at this time. Please choose a different slot.");

        var lesson = Lesson.Book(
            request.EnrollmentId,
            request.StudentId,
            request.StudentName,
            request.InstructorId,
            request.InstructorName,
            request.ScheduledAt,
            request.Duration);

        await repository.AddAsync(lesson, cancellationToken);

        return lesson.Id;
    }
}
