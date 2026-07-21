using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
 
namespace HRM.Services
{
    public class OrganisationService : IOrganisationService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public OrganisationService(IDbContextFactory<HrmTeContext> dbFactory)
        {
              _dbFactory = dbFactory;
        }


        public async Task<UserOrganisationDto?> GetOrganisationAsync(int businessEntityId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();


            return await db.Organisations
                .AsNoTracking()
                .Where(x =>
                    x.BusinessEntityID == businessEntityId)
                .Select(x => new UserOrganisationDto
                {
                    OrganisationBusinessEntityId = x.BusinessEntityID,

                    OrganisationName = x.OrganisationName,

                    OrganisationNameDhivehi = x.OrganisationNameDhivehi,

                    ParentOrganisationBusinessEntityId = x.ParentOrganisationBusinessEntityId
                })
                .FirstOrDefaultAsync();
        }



        public async Task<UserOrganisationDto?> GetUserOrganisationAsync(int userId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();


            return await (
                from userOrg in db.UserOrganisations

                join org in db.Organisations
                    on userOrg.BusinessEntityID
                    equals org.BusinessEntityID


                join user in db.Users
                    on userOrg.UserOrganisationID
                    equals user.UserOrganisationId
                    into users


                where users.Any(x =>
                    x.UserId == userId)


                select new UserOrganisationDto
                {
                    OrganisationBusinessEntityId = org.BusinessEntityID,

                    OrganisationName = org.OrganisationName,

                    OrganisationNameDhivehi = org.OrganisationNameDhivehi,

                    ParentOrganisationBusinessEntityId = org.ParentOrganisationBusinessEntityId
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }







        public async Task<List<OrganisationResultDto>> SearchOrganisationsAsync(string searchTerm, string searchType)
        {

            await using var _dbContext = await _dbFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<OrganisationResultDto>();

            var query = from org in _dbContext.Organisations
                        join ot in _dbContext.OrganisationTypes on org.OrganisationTypeId equals ot.OrganisationTypeId into otGroup
                        from orgType in otGroup.DefaultIfEmpty()
                        join c in _dbContext.Countries on org.CountryId equals c.CountryId into cGroup
                        from country in cGroup.DefaultIfEmpty()
                        select new { org, orgType, country };

            string term = searchTerm.Trim();

            if (searchType == "RegNo")
            {
                query = query.Where(q => q.org.RegistrationNumber == term);
            }
            else // Name Search
            {
                // THE FIX: Added null checks before .Contains()
                query = query.Where(q =>
                    (q.org.OrganisationName != null && q.org.OrganisationName.Contains(term)) ||
                    (q.org.OrganisationNameDhivehi != null && q.org.OrganisationNameDhivehi.Contains(term))
                );
            }

            return await query
                .Take(50)
                .Select(q => new OrganisationResultDto
                {
                    BusinessEntityID = q.org.BusinessEntityID,
                    OrganisationName = q.org.OrganisationName ?? string.Empty,
                    OrganisationNameDhivehi = q.org.OrganisationNameDhivehi ?? string.Empty,
                    RegistrationNumber = q.org.RegistrationNumber ?? string.Empty,
                    RegistrationDate = q.org.RegistrationDate,
                    Type = q.orgType != null ? q.orgType.TypeName : string.Empty,
                    Country = q.country != null ? q.country.NameEnglish : string.Empty
                })
                .ToListAsync();
        }

        //public async Task<List<OrganisationResultDto>> SearchOrganisationsAsync(string searchTerm, string searchType)
        //{
        //    if (string.IsNullOrWhiteSpace(searchTerm)) return new List<OrganisationResultDto>();

        //    var query = from org in _dbContext.Organisations
        //                join ot in _dbContext.OrganisationTypes on org.OrganisationTypeId equals ot.OrganisationTypeId into otGroup
        //                from orgType in otGroup.DefaultIfEmpty()
        //                join c in _dbContext.Countries on org.CountryId equals c.CountryId into cGroup
        //                from country in cGroup.DefaultIfEmpty()
        //                select new { org, orgType, country };

        //    string term = searchTerm.Trim();

        //    if (searchType == "RegNo")
        //    {
        //        query = query.Where(q => q.org.RegistrationNumber == term);
        //    }
        //    else // Name Search
        //    {
        //        query = query.Where(q => q.org.OrganisationName.Contains(term) || q.org.OrganisationNameDhivehi.Contains(term));
        //    }

        //    return await query
        //        .Take(50)
        //        .Select(q => new OrganisationResultDto
        //        {
        //            BusinessEntityID = q.org.BusinessEntityID,
        //            OrganisationName = q.org.OrganisationName ?? string.Empty,
        //            OrganisationNameDhivehi = q.org.OrganisationNameDhivehi ?? string.Empty,
        //            RegistrationNumber = q.org.RegistrationNumber ?? string.Empty,
        //            RegistrationDate = q.org.RegistrationDate,
        //            Type = q.orgType != null ? q.orgType.TypeName : string.Empty,
        //            Country = q.country != null ? q.country.NameEnglish : string.Empty
        //        })
        //        .ToListAsync();
        //}


        public async Task<OrganisationResultDto?> GetOrganisationDetailsAsync(int organisationId)
        {

            await using var _dbContext = await _dbFactory.CreateDbContextAsync();


            var orgDto = new OrganisationResultDto { BusinessEntityID = organisationId };

            orgDto.Teams = await _dbContext.Teams
                .Where(t => t.OrganisationId == organisationId)
                .Select(t => new TeamDto
                {
                    TeamId = t.TeamId,
                    Name = t.Name ?? string.Empty,
                    NameDhivehi = t.NameDhivehi ?? string.Empty,
                    FromDate = t.StartDate,
                    ToDate = t.EndDate,
                    // FIX 1: Safely handle null booleans
                    IsValid = t.IsValid ?? false,
                }).ToListAsync();

            orgDto.Groups = await _dbContext.Groups
                .Where(g => g.BusinessEntityOrganisationId == organisationId)
                .Select(g => new GroupDto
                {
                    GroupId = g.GroupId,
                    Name = g.Name ?? string.Empty,
                    NameDhivehi = g.Name ?? string.Empty, // Note: You mapped g.Name here instead of g.NameDhivehi in your original code

                    // FIX 2: Safely handle null navigation properties
                    Type = g.GroupType != null ? g.GroupType.Name : string.Empty,
                    Division = g.OrganisationStructure != null ? g.OrganisationStructure.Name : string.Empty,
                }).ToListAsync();

            return orgDto;
        }


        //public async Task<OrganisationResultDto?> GetOrganisationDetailsAsync(int organisationId)
        //{
        //    // First, get the base org (You might want to fetch this from DB, but assuming you pass it from Search)
        //    // For now, let's just fetch the nested lists you needed:
        //    var orgDto = new OrganisationResultDto { BusinessEntityID = organisationId };

        //    orgDto.Teams = await _dbContext.Teams
        //        .Where(t => t.OrganisationId == organisationId)
        //        .Select(t => new TeamDto
        //        {
        //            TeamId = t.TeamId,
        //            Name = t.Name ?? string.Empty,
        //            NameDhivehi = t.NameDhivehi ?? string.Empty,
        //            FromDate = t.StartDate,
        //            ToDate = t.EndDate,
        //            IsValid = (bool) t.IsValid,
        //        }).ToListAsync();

        //    orgDto.Groups = await _dbContext.Groups
        //        .Where(g => g.BusinessEntityOrganisationId == organisationId)
        //        .Select(g => new GroupDto
        //        {
        //            GroupId = g.GroupId,
        //            Name = g.Name ?? string.Empty,
        //            NameDhivehi = g.Name ?? string.Empty,
        //            Type = g.GroupType.Name,
        //            Division = g.OrganisationStructure.Name,
        //        }).ToListAsync();

        //    return orgDto;
        //}

        public async Task<List<OrgNodeDto>> GetOrganisationTreeAsync(int organisationId)
        {

            await using var _dbContext = await _dbFactory.CreateDbContextAsync();


            // 1. Fetch structures AND their parent relations in a SINGLE efficient SQL query
            var query = from s in _dbContext.OrganisationStructures
                        where s.OrganisationBusinessEntityID == organisationId && s.OrganisationStructureStateId == 3

                        // LEFT JOIN to get the current parent relation securely on the database side
                        join r in _dbContext.OrganisationStructureRelations.Where(rel => rel.IsCurrent)
                            on s.OrganisationStructureId equals r.OrganisationStructureId into relGroup
                        from relation in relGroup.DefaultIfEmpty()

                        select new OrgNodeDto
                        {
                            StructureId = s.OrganisationStructureId,
                            Name = s.Name,
                            TypeId = s.OrganisationStructureTypeId,
                            // Safely extract the ParentId if a relationship exists
                            ParentId = relation != null ? relation.ParentOrganisationStructureId : null
                        };

            // Execute the single query and bring exactly what we need into memory
            var allNodes = await query.ToListAsync();

            // 2. Build the tree in memory using your highly efficient ToLookup pattern
            var lookup = allNodes.ToLookup(node => node.ParentId);
            foreach (var node in allNodes)
            {
                node.Children = lookup[node.StructureId].ToList();
            }

            // 3. Find the actual roots (nodes whose parent is null OR parent isn't in this specific tree)
            var actualRoots = allNodes
                .Where(n => n.ParentId == null || !allNodes.Any(p => p.StructureId == n.ParentId))
                .ToList();

            // 4. Return with the synthetic Ministry root
            return new List<OrgNodeDto>
        {
        new OrgNodeDto
        {
            StructureId = -1,
            ParentId = null,
            TypeId = 99,
            Name = "The Ministry",
            Children = actualRoots
        }
         };
        }

        //public async Task<List<OrgNodeDto>> GetOrganisationTreeAsync(int organisationId)
        //{
        //    var structures = await _dbContext.OrganisationStructures
        //        .Where(o => o.OrganisationBusinessEntityID == organisationId && o.OrganisationStructureStateId == 3)
        //        .ToListAsync();

        //    var relations = await _dbContext.OrganisationStructureRelations
        //        .Where(r => r.IsCurrent)
        //        .ToListAsync();

        //    var allNodes = structures.Select(s => new OrgNodeDto
        //    {
        //        StructureId = s.OrganisationStructureId,
        //        Name = s.Name,
        //        TypeId = s.OrganisationStructureTypeId,
        //        ParentId = relations.FirstOrDefault(r => r.OrganisationStructureId == s.OrganisationStructureId)?.ParentOrganisationStructureId
        //    }).ToList();

        //    var lookup = allNodes.ToLookup(node => node.ParentId);
        //    foreach (var node in allNodes)
        //    {
        //        node.Children = lookup[node.StructureId].ToList();
        //    }

        //    var actualRoots = allNodes.Where(n => n.ParentId == null || !allNodes.Any(p => p.StructureId == n.ParentId)).ToList();

        //    // Return with the synthetic Ministry root
        //    return new List<OrgNodeDto>
        //    {
        //        new OrgNodeDto { StructureId = -1, ParentId = null, TypeId = 99, Name = "The Ministry", Children = actualRoots }
        //    };
        //}
        // --- Helper Lookups ---
        public async Task<List<DropdownItem>> GetCountriesAsync()
        {

            await using var _dbContext = await _dbFactory.CreateDbContextAsync();
             return  
                await _dbContext.Countries.Select(c => new DropdownItem { Id = c.CountryId, Name = c.NameEnglish }).ToListAsync();


        }
            

        public async Task<List<DropdownItem>> GetOrganisationTypesAsync()
        {
            await using var _dbContext = await _dbFactory.CreateDbContextAsync();
            return await _dbContext.OrganisationTypes.Select(ot => new DropdownItem { Id = ot.OrganisationTypeId, Name = ot.TypeName }).ToListAsync();


        }
           
 
        public async Task<List<DropdownItem>> GetAllOrganisationsAsync()
        {

            await using var _dbContext = await _dbFactory.CreateDbContextAsync();


            return await _dbContext.Organisations
                .Select(o => new DropdownItem
                {
                    Id = o.BusinessEntityID,
                    Name = o.OrganisationName ?? "Unknown"
                })
                .OrderBy(o => o.Name)
                .ToListAsync();
        }
    }
   
}
