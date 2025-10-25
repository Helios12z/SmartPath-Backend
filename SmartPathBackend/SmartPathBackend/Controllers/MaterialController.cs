using AutoMapper;
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
    public class MaterialController : ControllerBase
    {
        private readonly IMaterialService _service;
        private readonly IMapper _mapper;

        public MaterialController(IMaterialService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [Authorize]
        [HttpPost("images")]
        [RequestSizeLimit(40_000_000)] 
        public async Task<ActionResult<MaterialResponse>> UploadImage(
            [FromForm] MaterialCreateRequest meta,
            [FromForm] IFormFile file)
        {
            var uid = User.GetUserIdOrThrow();
            var res = await _service.UploadImageAsync(uid, meta, file);
            return Ok(res);
        }

        [Authorize]
        [HttpPost("documents")]
        [RequestSizeLimit(120_000_000)] 
        public async Task<ActionResult<IReadOnlyList<MaterialResponse>>> UploadDocuments(
            [FromForm] MaterialCreateRequest meta,
            [FromForm] IFormFile[] files)
        {
            var uid = User.GetUserIdOrThrow();
            var res = await _service.UploadDocumentsAsync(uid, meta, files);
            return Ok(res);
        }

        [HttpGet("by-post/{postId:guid}")]
        public async Task<ActionResult<IReadOnlyList<MaterialResponse>>> GetByPost([FromRoute] Guid postId) =>
            Ok(await _service.GetByPostAsync(postId));

        [HttpGet("by-comment/{commentId:guid}")]
        public async Task<ActionResult<IReadOnlyList<MaterialResponse>>> GetByComment([FromRoute] Guid commentId) =>
            Ok(await _service.GetByCommentAsync(commentId));

        [HttpGet("by-message/{messageId:guid}")]
        public async Task<ActionResult<IReadOnlyList<MaterialResponse>>> GetByMessage([FromRoute] Guid messageId) =>
            Ok(await _service.GetByMessageAsync(messageId));

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var uid = User.GetUserIdOrThrow();
            var ok = await _service.DeleteAsync(id, uid);
            return ok ? NoContent() : Forbid();
        }
    }
}
