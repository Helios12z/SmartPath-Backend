using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendships;
        public FriendshipController(IFriendshipService friendships) => _friendships = friendships;

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.GetUserIdOrThrow();
            var friends = await _friendships.GetFriendsAsync(userId);
            return Ok(friends);
        }

        [HttpPost("follow")]
        [Authorize]
        public async Task<IActionResult> Follow([FromBody] FriendshipRequestDto request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var followerId = User.GetUserIdOrThrow();
            var result = await _friendships.AddFriendAsync(followerId, request);
            return Ok(result); 
        }

        [HttpDelete("{followedUserId:guid}")]
        [Authorize]
        public async Task<IActionResult> CancelFollow([FromRoute] Guid followedUserId)
        {
            var followerId = User.GetUserIdOrThrow();
            var ok = await _friendships.CancelFollowAsync(followerId, followedUserId);
            return ok ? NoContent() : NotFound();
        }
    }
}
