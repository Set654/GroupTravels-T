using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Api.Contracts;
using TripService.Api.Extensions;
using TripService.Application.Trips;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public sealed class InvitationsController : ControllerBase
{
    private readonly TripMemberService _memberService;

    public InvitationsController(
        TripMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpPost("{tripId:int}/invite")]
    public async Task<IActionResult> Invite(
        int tripId,
        [FromBody] InviteMemberRequest request,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _memberService.InviteAsync(
                userId,
                tripId,
                request.Email,
                request.Phone,
                ct);

            switch (result.Status)
            {
                case InviteStatus.Created:
                    return StatusCode(
                        StatusCodes.Status201Created,
                        new
                        {
                            memberId = result.MemberId,
                            status = "Pending"
                        });

                case InviteStatus.UserNotFound:
                    return NotFound(new
                    {
                        message = "Пользователь не зарегистрирован."
                    });

                case InviteStatus.AlreadyInvited:
                    return Conflict(new
                    {
                        message = "Пользователь уже состоит " +
                                  "в поездке или был приглашён."
                    });

                case InviteStatus.Forbidden:
                    return Forbid();

                default:
                    return StatusCode(
                        StatusCodes.Status500InternalServerError);
            }
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: ex.Message);
        }
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var invitations =
            await _memberService.GetInvitationsAsync(
                userId,
                ct);

        return Ok(new { invitations });
    }

    [HttpPut("{tripId:int}/invite/respond")]
    public async Task<IActionResult> Respond(
        int tripId,
        [FromBody] RespondInvitationRequest request,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        try
        {
            var success =
                await _memberService.RespondToInviteAsync(
                    userId,
                    tripId,
                    request.MemberId,
                    request.Decision,
                    ct);

            if (!success)
            {
                return NotFound(new
                {
                    message = "Активное приглашение не найдено."
                });
            }

            return Ok(new
            {
                memberId = request.MemberId,
                status = request.Decision == "Accept"
                    ? "Accepted"
                    : "Declined"
            });
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: ex.Message);
        }
    }
}