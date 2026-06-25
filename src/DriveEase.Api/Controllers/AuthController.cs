using Asp.Versioning;
using DriveEase.Students.Application.Commands.LoginStudent;
using DriveEase.Students.Application.Commands.RegisterStudent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterStudentCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginStudentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }
}
