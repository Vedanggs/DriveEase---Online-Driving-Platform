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
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollStudentCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrollment(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var studentId))
            return Unauthorized();

        var dto = await sender.Send(new GetMyEnrollmentQuery(studentId), cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetEnrollmentQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/payment")]
    public async Task<IActionResult> ProcessPayment(Guid id, CancellationToken cancellationToken)
    {
        var success = await sender.Send(new ProcessPaymentCommand(id), cancellationToken);
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
}

public sealed record AssignInstructorRequest(Guid InstructorId);
