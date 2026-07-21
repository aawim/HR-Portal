using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRM.Models; // Update with your actual namespace
//using HRM.Data;   // Update with your actual DbContext namespace

[Route("api/[controller]")]
[ApiController]
public class LeaveTypesController : ControllerBase
{
    private readonly HrmTeContext _context;

    public LeaveTypesController(HrmTeContext context)
    {
        _context = context;
    }


    // GET: api/LeaveTypes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveType>>> GetLeaveTypes([FromQuery] int? organisationId = null)
    {
        var query = _context.LeaveTypes.AsQueryable();

        // If an organization is selected (and is greater than 0), filter the query
        if (organisationId.HasValue && organisationId.Value > 0)
        {
            query = query.Where(lt => lt.OrganisationId == organisationId.Value);
        }

        return await query.ToListAsync();
    }



    // POST: api/LeaveTypes
    [HttpPost]
    public async Task<ActionResult<LeaveType>> CreateLeaveType(LeaveType leaveType)
    {
        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLeaveTypes), new { id = leaveType.LeaveTypeId }, leaveType);
    }

    // PUT: api/LeaveTypes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLeaveType(int id, LeaveType leaveType)
    {
        if (id != leaveType.LeaveTypeId) return BadRequest();

        _context.Entry(leaveType).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/LeaveTypes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLeaveType(int id)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null) return NotFound();

        _context.LeaveTypes.Remove(leaveType);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}