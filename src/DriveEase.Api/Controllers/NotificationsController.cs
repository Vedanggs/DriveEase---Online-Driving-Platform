using Asp.Versioning;
using DriveEase.Notifications.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("instructor/{instructorId:guid}")]
    public async Task<IActionResult> GetInstructorNotifications(
        Guid instructorId, CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(
            new GetInstructorNotificationsQuery(instructorId), cancellationToken);
        return Ok(notifications);
    }
}
