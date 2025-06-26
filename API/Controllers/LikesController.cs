namespace API.Controllers;

using API.Data;
using API.DataEntities;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.UnitOfWork;
using Microsoft.AspNetCore.Mvc;

public class LikesController(IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpPost("{targetUserId:int}")]
    public async Task<ActionResult> ToggleLike(int targetUserId)
    {
        var sourceUserId = User.GetUserId();
        if (sourceUserId == targetUserId) { return BadRequest("You already like yourself! :D"); }

        var existingLike = await unitOfWork.LikesRepository.GetUserLikeAsync(sourceUserId, targetUserId);
        if (existingLike == null)
        {
            var like = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId
            };
            unitOfWork.LikesRepository.AddLike(like);
        }
        else { unitOfWork.LikesRepository.RemoveLike(existingLike); }

        if (await unitOfWork.Complete()) { return Ok(); }
        return BadRequest("Failed to update like");
    }

    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<int>>> GetCurrentUSerLikeIds()
        => Ok(await unitOfWork.LikesRepository.GetCurrentUserLikeIdsAsync(User.GetUserId()));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberResponse>>> GetUserLikes([FromQuery] LikesParams likesParams)
    {
        likesParams.UserId = User.GetUserId();
        var users = await unitOfWork.LikesRepository.GetUserLikesAsync(likesParams);
        Response.AddPaginationHeader(users);
        return Ok(users);
    }
}
