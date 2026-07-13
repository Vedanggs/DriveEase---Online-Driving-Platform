using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using DriveEase.Enrollments.Application.Commands.AssignInstructor;
using DriveEase.Enrollments.Application.Commands.EnrollStudent;
using DriveEase.Enrollments.Application.Commands.ProcessPayment;
using DriveEase.Enrollments.Application.Queries.GetEnrollment;
using DriveEase.Enrollments.Application.Queries.GetMyEnrollment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class EnrollmentsController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "Student")]
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var studentId))
            return Unauthorized();

        var id = await sender.Send(command with { StudentId = studentId }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrollment(CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var studentId))
            return Unauthorized();

        var dto = await sender.Send(new GetMyEnrollmentQuery(studentId), cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetEnrollmentQuery(id), cancellationToken);
        if (dto is null) return NotFound();

        if (!TryGetCallerId(out var callerId) || dto.StudentId != callerId)
            return Unauthorized();

        return Ok(dto);
    }

    [Authorize(Policy = "Student")]
    [HttpPost("{id:guid}/payment")]
    public async Task<IActionResult> ProcessPayment(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var studentId))
            return Unauthorized();

        var success = await sender.Send(new ProcessPaymentCommand(id, studentId), cancellationToken);
        return Ok(new { success });
    }

    [HttpPost("{id:guid}/instructor")]
    public async Task<IActionResult> AssignInstructor(
        Guid id,
        [FromBody] AssignInstructorRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AssignInstructorCommand(id, request.InstructorId), cancellationToken);
        return NoContent();
    }

    private bool TryGetCallerId(out Guid callerId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out callerId);
    }
}

public sealed record AssignInstructorRequest(Guid InstructorId);
