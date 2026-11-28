using Intranet.Application.Security;
using Intranet.Domain.Entities;
using Intranet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Intranet.Api.Controllers;
[ApiController]
[Route("api/intranet/feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IntranetDbContext _db;
    private readonly IOrganisationContext _org;
    public FeedController(IntranetDbContext db, IOrganisationContext org) { _db = db; _org = org; }
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = _org.OrganisationBusinessEntityID;
        var items = await _db.IntranetPosts
            .Where(p => p.OrganisationBusinessEntityID == orgId && p.Status == Domain.Enums.PostStatus.Published)
            .OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.PublishedAt)
            .Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(items);
    }
}