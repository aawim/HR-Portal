using Intranet.Application.Security;
using Intranet.Domain.Entities;
using Intranet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Intranet.Api.Controllers;
[ApiController]
[Route("api/intranet")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IntranetDbContext _db;
    private readonly IOrganisationContext _org;
    public ContentController(IntranetDbContext db, IOrganisationContext org) { _db = db; _org = org; }
    [HttpGet("recognitions")]
    public async Task<IActionResult> Recognitions(CancellationToken ct = default)
        => Ok(await _db.IntranetPosts.Where(p => p.OrganisationBusinessEntityID == _org.OrganisationBusinessEntityID
            && p.PostType == "Achievement" && p.Status == Domain.Enums.PostStatus.Published).ToListAsync(ct));
    [HttpGet("resources")]
    public async Task<IActionResult> Resources(CancellationToken ct = default)
        => Ok(await _db.IntranetPosts.Where(p => p.OrganisationBusinessEntityID == _org.OrganisationBusinessEntityID
            && (p.PostType == "Policy" || p.PostType == "KnowledgeArticle") && p.Status == Domain.Enums.PostStatus.Published).ToListAsync(ct));
    [HttpGet("new-joiners")]
    public async Task<IActionResult> NewJoiners(CancellationToken ct = default)
        => Ok(await _db.IntranetPosts.Include(p => p.NewJoinerProfile).Where(p => p.OrganisationBusinessEntityID == _org.OrganisationBusinessEntityID
            && p.PostType == "NewJoiner" && p.Status == Domain.Enums.PostStatus.Published).ToListAsync(ct));
    [HttpGet("announcements")]
    public async Task<IActionResult> Announcements(CancellationToken ct = default)
        => Ok(await _db.IntranetPosts.Where(p => p.OrganisationBusinessEntityID == _org.OrganisationBusinessEntityID
            && p.PostType == "Announcement" && p.Status == Domain.Enums.PostStatus.Published).ToListAsync(ct));
    [HttpGet("department-news")]
    public async Task<IActionResult> DepartmentNews(CancellationToken ct = default)
        => Ok(await _db.IntranetPosts.Where(p => p.OrganisationBusinessEntityID == _org.OrganisationBusinessEntityID
            && p.PostType == "DepartmentNews" && p.Status == Domain.Enums.PostStatus.Published).ToListAsync(ct));
}