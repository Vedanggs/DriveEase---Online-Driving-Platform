using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using DriveEase.Schools.Application.Commands.RegisterInstructor;
using DriveEase.Schools.Application.Commands.RegisterSchool;
using DriveEase.Schools.Application.Queries.GetAllSchools;
using DriveEase.Schools.Application.Queries.GetSchool;
using DriveEase.Schools.Application.Queries.GetSchoolInstructors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class SchoolsController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var schools = await sender.Send(new GetAllSchoolsQuery(), cancellationToken);
        return Ok(schools);
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterSchoolCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetSchoolQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [AllowAnonymous]
    [HttpGet("{schoolId:guid}/instructors")]
    public async Task<IActionResult> GetInstructors(Guid schoolId, CancellationToken cancellationToken)
    {
        var instructors = await sender.Send(new GetSchoolInstructorsQuery(schoolId), cancellationToken);
        return Ok(instructors);
    }

    [AllowAnonymous]
    [HttpPost("{schoolId:guid}/instructors")]
    public async Task<IActionResult> RegisterInstructor(
        Guid schoolId, [FromBody] RegisterInstructorRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new RegisterInstructorCommand(schoolId, request.FullName, request.LicenseNumber, request.Email, request.Password),
            cancellationToken);
        return Ok(new { id });
    }
}

public sealed record RegisterInstructorRequest(string FullName, string LicenseNumber, string Email, string Password);
