// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user-todo")]
public sealed class UserTodoController(global::HRM.Services.Interfaces.IUserTodoService service) : HrmApiController
{
    [HttpGet("get-my-todos")]
    public async Task<ActionResult<global::HRM.DTOs.UserTodoPage>> GetMyTodos([FromQuery] global::System.Boolean includeCompleted, [FromQuery] global::System.Int32 page)
    {
        return Ok(await service.GetMyTodosAsync(includeCompleted, page));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] UserTodoSaveRequest request)
    {
        await service.SaveAsync(request.model);
        return NoContent();
    }

    public sealed class UserTodoSaveRequest
    {
        public global::HRM.DTOs.UserTodoDto model { get; set; } = default!;
    }

    [HttpPost("set-completed")]
    public async Task<IActionResult> SetCompleted([FromBody] UserTodoSetCompletedRequest request)
    {
        await service.SetCompletedAsync(request.id, request.rowVersion, request.completed);
        return NoContent();
    }

    public sealed class UserTodoSetCompletedRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Byte[] rowVersion { get; set; } = default!;
        public global::System.Boolean completed { get; set; }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] UserTodoDeleteRequest request)
    {
        await service.DeleteAsync(request.id, request.rowVersion);
        return NoContent();
    }

    public sealed class UserTodoDeleteRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Byte[] rowVersion { get; set; } = default!;
    }

}
