using HRM.DTOs;
using HRM.Api.Infrastructure;
using HRM.DTOs.Organisation;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    public class OrganisationController : HrmApiController
    {
        private readonly IOrganisationService _organisationService;
        private readonly HRM.Services.Interfaces.IUserAccessService _access;

        public OrganisationController(IOrganisationService organisationService, HRM.Services.Interfaces.IUserAccessService access)
        {
            _organisationService = organisationService;
            _access = access;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<OrganisationResultDto>>> Search([FromQuery] string term, [FromQuery] string type = "Name")
        {
            var actor = await _access.RequireContextAsync();
            if (!actor.IsSuperAdministrator) return Forbid();
            if (string.IsNullOrWhiteSpace(term) || term.Length > 200) return BadRequest("Enter a search term of up to 200 characters.");
            var results = await _organisationService.SearchOrganisationsAsync(term, type);
            return Ok(results);
        }

        [HttpGet("{id}/tree")]
        public async Task<ActionResult<List<OrgNodeDto>>> GetTree(int id)
        {
            await _access.RequireContextAsync();
            if (!await _access.HasOrganisationAccessAsync(id)) return Forbid();
            var tree = await _organisationService.GetOrganisationTreeAsync(id);
            return Ok(tree);
        }
    }
}
