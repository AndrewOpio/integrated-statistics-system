using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Models;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController(INotificationAppService notificationService) : ControllerBase
{
    /// <summary>Creates and dispatches a new notification.</summary>
    /// <returns>The ID of the created notification.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SendNotificationResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationRequestDto request,
        CancellationToken ct)
    {
        var id = await notificationService.SendAsync(request, ct);
        return Accepted(new SendNotificationResponseDto(id));
    }

    /// <summary>
    /// Gets a notification by ID including its current status.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await notificationService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
