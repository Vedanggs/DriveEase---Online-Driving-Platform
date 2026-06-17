using Microsoft.AspNetCore.Authorization;
using DriveEase.Students.Application.Commands.RegisterStudent;
using DriveEase.Students.Application.Queries.GetStudent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class StudentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterStudentCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetStudentQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}
