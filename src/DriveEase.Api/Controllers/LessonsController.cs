using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using DriveEase.Lessons.Application.Commands.BookLesson;
using DriveEase.Lessons.Application.Commands.CancelLesson;
using DriveEase.Lessons.Application.Commands.CompleteLesson;
using DriveEase.Lessons.Application.Queries.GetInstructorLessons;
using DriveEase.Lessons.Application.Queries.GetLesson;
using DriveEase.Lessons.Application.Queries.GetStudentLessons;
using DriveEase.Lessons.Application.Queries.GetInstructorBookedSlots;
using DriveEase.Lessons.Application.Queries.GetEnrollmentLessonCount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class LessonsController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "Student")]
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookLessonCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var studentId))
            return Unauthorized();

        var id = await sender.Send(command with { StudentId = studentId }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetLessonQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var lessons = await sender.Send(new GetStudentLessonsQuery(studentId), cancellationToken);
        return Ok(lessons);
    }

    [AllowAnonymous]
    [HttpGet("instructor/{instructorId:guid}")]
    public async Task<IActionResult> GetByInstructor(Guid instructorId, CancellationToken cancellationToken)
    {
        var lessons = await sender.Send(new GetInstructorLessonsQuery(instructorId), cancellationToken);
        return Ok(lessons);
    }

    [AllowAnonymous]
    [HttpGet("enrollment/{enrollmentId:guid}/count")]
    public async Task<IActionResult> GetEnrollmentLessonCount(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEnrollmentLessonCountQuery(enrollmentId), cancellationToken);
        return Ok(new { count = result.Completed, scheduledCount = result.Scheduled });
    }

    [AllowAnonymous]
    [HttpGet("instructor/{instructorId:guid}/booked-slots")]
    public async Task<IActionResult> GetBookedSlots(Guid instructorId, [FromQuery] string date, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");
        var slots = await sender.Send(new GetInstructorBookedSlotsQuery(instructorId, DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc)), cancellationToken);
        return Ok(slots);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelLessonCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "Instructor")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteLessonRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var instructorId))
            return Unauthorized();

        await sender.Send(new CompleteLessonCommand(id, instructorId, request?.Notes), cancellationToken);
        return NoContent();
    }

    private bool TryGetCallerId(out Guid callerId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out callerId);
    }
}

public sealed record CompleteLessonRequest(string? Notes);
