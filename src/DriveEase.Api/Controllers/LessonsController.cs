using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using DriveEase.Lessons.Application.Commands.BookLesson;
using DriveEase.Lessons.Application.Commands.CompleteLesson;
using DriveEase.Lessons.Application.Queries.GetInstructorLessons;
using DriveEase.Lessons.Application.Queries.GetLesson;
using DriveEase.Lessons.Application.Queries.GetStudentLessons;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class LessonsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookLessonCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
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
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteLessonRequest? request, CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteLessonCommand(id, request?.Notes), cancellationToken);
        return NoContent();
    }
}

public sealed record CompleteLessonRequest(string? Notes);
