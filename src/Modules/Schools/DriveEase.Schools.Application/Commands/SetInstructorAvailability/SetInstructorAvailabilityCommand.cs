using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Commands.SetInstructorAvailability;

public sealed record SetInstructorAvailabilityCommand(Guid InstructorId, bool IsAvailable) : IRequest;

public sealed class SetInstructorAvailabilityHandler(IInstructorRepository repository)
    : IRequestHandler<SetInstructorAvailabilityCommand>
{
    public async Task Handle(SetInstructorAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var instructor = await repository.GetByIdAsync(request.InstructorId, cancellationToken)
            ?? throw new InvalidOperationException($"Instructor {request.InstructorId} not found.");
        instructor.SetAvailability(request.IsAvailable);
        await repository.UpdateAsync(instructor, cancellationToken);
    }
}
