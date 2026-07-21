using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Staff;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRM.Services
{
    public class StaffService : IStaffService
    {
        private readonly HrmTeContext _dbContext;

        public StaffService(HrmTeContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<IndividualResultDto>> SearchStaffAsync(string searchTerm, string searchType, int? organisationId)
        {
            var query = from ind in _dbContext.Individuals
                        join id in _dbContext.Idcards on ind.BusinessEntityId equals id.BusinessEntityId into idGroup
                        from idCard in idGroup.DefaultIfEmpty()
                        join p in _dbContext.Passports on ind.BusinessEntityId equals p.BusinessEntityId into pGroup
                        from passport in pGroup.DefaultIfEmpty()
                        join j in _dbContext.Jobs.Where(x => x.TerminatedDate == null || x.TerminatedDate > DateTime.Today)
                             on ind.BusinessEntityId equals j.IndividualID into jobGroup
                        from activeJob in jobGroup.DefaultIfEmpty()
                        join org in _dbContext.Organisations on activeJob.OrganisationID equals org.BusinessEntityID into orgGroup
                        where activeJob.JobStateId == SharedConfig.JobStates.APPROVED
                        from organisation in orgGroup.DefaultIfEmpty()
                        select new { ind, idCard, passport, organisation };
                       

            // 1. Apply Organisation Filter
            if (organisationId.HasValue)
            {
                query = query.Where(q => q.organisation != null && q.organisation.BusinessEntityID == organisationId.Value);
            }

            // 2. Apply Text Filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim();
                if (searchType == "ID")
                {
                    query = query.Where(q => q.idCard.IdcardNumber == term);
                }
                else if (searchType == "Passport")
                {
                    query = query.Where(q => q.passport.PassportNumber == term);
                }
                else // Name Search
                {
                    query = query.Where(q => (q.ind.FirstNameEnglish + (q.ind.LastNameEnglish != null ? " " + q.ind.LastNameEnglish : "")).Contains(term));
                }
            }

            return await query
                .Take(50)
                .Select(q => new IndividualResultDto
                {
                    BusinessEntityId = q.ind.BusinessEntityId,
                    FullName = (q.ind.FirstNameEnglish + (q.ind.LastNameEnglish != null ? " " + q.ind.LastNameEnglish : "")).Trim(),
                    IDNumber = q.idCard != null ? q.idCard.IdcardNumber : string.Empty,
                    PassportNumber = q.passport != null ? q.passport.PassportNumber : string.Empty,
                    Organisation = q.organisation != null ? q.organisation.OrganisationName : string.Empty,
                    Address = q.ind.CountryId.ToString(),
                    Nationality = q.ind.CountryId.ToString()
                })
                .ToListAsync();
            }

    }
}
