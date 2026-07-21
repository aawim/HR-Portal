using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganisationController : ControllerBase
    {
        private readonly IOrganisationService _organisationService;

        public OrganisationController(IOrganisationService organisationService)
        {
            _organisationService = organisationService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<OrganisationResultDto>>> Search([FromQuery] string term, [FromQuery] string type = "Name")
        {
            var results = await _organisationService.SearchOrganisationsAsync(term, type);
            return Ok(results);
        }

        [HttpGet("{id}/tree")]
        public async Task<ActionResult<List<OrgNodeDto>>> GetTree(int id)
        {
            var tree = await _organisationService.GetOrganisationTreeAsync(id);
            return Ok(tree);
        }
    }
}