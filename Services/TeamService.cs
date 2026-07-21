using HRM.DTOs;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
 

namespace HRM.Services
{
    public class TeamService : ITeam
    {

        private readonly IDbContextFactory<HrmTeContext> _context;
        private readonly IUserAccessService _access;



        public TeamService(
            IDbContextFactory<HrmTeContext> factory,IUserAccessService userAccessService
            )
        {
            _context = factory;

            _access = userAccessService;

 
        }

        public Task<List<TeamDto>> GetMyTeamMembersAsync(int TeamId)
        {
            throw new NotImplementedException();
        }


    

           



        public async Task<List<TeamDto>> GetMyTeamsAsync()
        {
        
            var context = await _access.RequireContextAsync();
            using var db = await _context.CreateDbContextAsync();


            // 1. Get teams where current user is supervisor
            var myTeamIds = await db.TeamStaffs
                .Where(ts =>
                    ts.StaffId == context.IndividualId &&
                    ts.IsSuperVisor &&
                    ts.IsValid)
                .Select(ts => ts.TeamId)
                .Distinct()
                .ToListAsync();


            if (!myTeamIds.Any())
                return new List<TeamDto>();


            // 2. Get teams and their staff
            var teams = await db.Teams
                .Where(t =>
                    myTeamIds.Contains(t.TeamId) &&
                    t.IsValid == true)
                .Select(t => new TeamDto
                {
                    TeamId = t.TeamId,
                    Name = t.Name,
                   // NameDhivehi = t.NameDhivehi,

                    Staffs = t.TeamStaffs
                        .Where(ts => ts.IsValid)
                        .Select(ts => new TeamStaffDto
                        {
                            StaffId = ts.StaffId,

                            IsSuperVisor = ts.IsSuperVisor,

                            FullName =
                                ts.Staff.Individual.FirstNameEnglish
                                + " "
                                + ts.Staff.Individual.LastNameEnglish,

                             //=
                             //   ts.Staff.Individual.FirstNameDhivehi
                             //   + " "
                             //   + ts.Staff.Individual.LastNameDhivehi
                        })
                        .ToList()
                })
                .ToListAsync();


            return teams;



        }
    }
}
