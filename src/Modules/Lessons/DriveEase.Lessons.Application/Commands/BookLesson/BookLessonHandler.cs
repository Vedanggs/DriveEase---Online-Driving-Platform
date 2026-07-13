using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed class BookLessonHandler(
    ILessonRepository repository,
    IEnrollmentRepository enrollmentRepository) : IRequestHandler<BookLessonCommand, Guid>
{
    private const int MaxLessonsPerEnrollment = 5;
    private const int MaxPendingLessons = 2;

    public async Task<Guid> Handle(BookLessonCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment {request.EnrollmentId} not found.");

        if (enrollment.StudentId != request.StudentId)
            throw new UnauthorizedAccessException("You can only book lessons for your own enrollment.");

        // Only instructor-confirmed (Completed) lessons count toward the 5-lesson package.
        // Scheduled (pending) and Cancelled lessons do not consume a slot.
        var completedCount = await repository.CountCompletedByEnrollmentAsync(request.EnrollmentId, cancellationToken);
        if (completedCount >= MaxLessonsPerEnrollment)
            throw new InvalidOperationException(
                $"Lesson package limit reached. Maximum {MaxLessonsPerEnrollment} lessons per enrollment.");

        // At most 2 lessons may be awaiting completion at any time — the rest of the
        // package can only be booked as those are completed or cancelled.
        var scheduledCount = await repository.CountScheduledByEnrollmentAsync(request.EnrollmentId, cancellationToken);
        if (scheduledCount >= MaxPendingLessons)
            throw new InvalidOperationException(
                $"You already have {MaxPendingLessons} lessons scheduled. " +
                "Complete or cancel one before booking another.");

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
