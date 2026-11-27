using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Api.Contracts;
using TripService.Api.Extensions;
using TripService.Application.Trips;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:int}/members")]
[Authorize]
public sealed class MembersController : ControllerBase
{
    private readonly TripMemberService _memberService;

    public MembersController(TripMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int tripId,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var members = await _memberService.GetMembersAsync(
            userId,
            tripId,
            ct);

        if (members is null)
        {
            return NotFound();
        }

        return Ok(new { members });
    }

    [HttpPost("virtual")]
    public async Task<IActionResult> AddVirtual(
        int tripId,
        [FromBody] AddVirtualMemberRequest request,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        try
        {
            var member = await _memberService.AddVirtualAsync(
                userId,
                tripId,
                request.VirtualName,
                ct);

            if (member is null)
            {
                return Forbid();
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    memberId = member.MemberId,
                    virtualName = member.DisplayName,
                    role = member.Role,
                    status = member.Status
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