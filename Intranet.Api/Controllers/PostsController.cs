using Intranet.Application.Security;
using Intranet.Application.Services;
using Intranet.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController]
[Route("api/intranet/posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _svc;
    private readonly IOrganisationContext _org;
    public PostsController(IPostService svc, IOrganisationContext org) { _svc = svc; _org = org; }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _svc.ListAsync(_org.OrganisationBusinessEntityID, page, pageSize, ct));
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct = default)
    {
        var p = await _svc.GetByIdAsync(id, _org.OrganisationBusinessEntityID, ct);
        return p is null ? NotFound() : Ok(p);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IntranetPost post, CancellationToken ct = default)
    { post.OrganisationBusinessEntityID = _org.OrganisationBusinessEntityID; post.CreatedBy = _org.IndividualID; return Ok(await _svc.CreateAsync(post, ct)); }
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] IntranetPost post, CancellationToken ct = default)
    { post.IntranetPostID = id; return Ok(await _svc.UpdateAsync(post, ct)); }
    [HttpPost("{id:long}/publish")]
    public async Task<IActionResult> Publish(long id, CancellationToken ct = default)
    { await _svc.PublishAsync(id, _org.IndividualID, ct); return NoContent(); }
    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct = default)
    { await _svc.ArchiveAsync(id, _org.IndividualID, ct); return NoContent(); }
}