using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class OrganisationSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs, IUserContextService? contexts = null)
:global::HRM.Services.Contracts.OrganisationSettingsServiceContracts,global::HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations{
    private async Task RequireAsync(int id, params string[] keys)
    {
        var user = await access.RequireContextAsync();
        var allowed = user.IsSuperAdministrator || await access.HasPermissionAsync("Admin");
        foreach (var key in keys) allowed |= await access.HasPermissionAsync(key);
        if (!allowed) throw new UnauthorizedAccessException("You do not have permission to manage organisation records.");
        if (!user.IsSuperAdministrator)
        {
            var current = await access.GetCurrentOrganisationIdAsync();
            if (current <= 0 || id > 0 && id != current || !await access.HasOrganisationAccessAsync(current))
                throw new UnauthorizedAccessException("You can access only your current organisation.");
        }
    }
    public async Task<OrganisationSettingsData> LoadAsync()
    {
        await RequireAsync(0, "UserOrganisations.View", "UserOrganisations.Admin.View", "Organisation.Edit", "Organisation.Contact.View");
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var currentOrganisation = user.IsSuperAdministrator ? 0 : await access.GetCurrentOrganisationIdAsync();
        var inactiveEntityStateIds = await db.BusinessEntityStates.AsNoTracking()
            .Where(s => s.StateName.Contains("Deleted") || s.StateName.Contains("Inactive") || s.StateName.Contains("Closed") || s.StateName.Contains("Disabled"))
            .Select(s => s.BusinessEntityStateId)
            .ToListAsync();
        var organisations = await db.Organisations.AsNoTracking()
            .Where(o => (user.IsSuperAdministrator || o.BusinessEntityID == currentOrganisation) &&
                        !db.BusinessEntities.Any(b => b.BusinessEntityID == o.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveEntityStateIds.Contains(b.BusinessEntityStateId.Value)))
            .OrderBy(x => x.OrganisationName).Select(x => new OrganisationSettingsDto
        { Id = x.BusinessEntityID, Name = x.OrganisationName, DhivehiName = x.OrganisationNameDhivehi, RegistrationNumber = x.RegistrationNumber ?? "",
            RegistrationDate = x.RegistrationDate, TypeId = x.OrganisationTypeId, CountryId = x.CountryId, ParentId = x.ParentOrganisationBusinessEntityId }).ToListAsync();
        return new()
        {
            Organisations = organisations,
            Types = await db.OrganisationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.OrganisationTypeId, x.TypeName)).ToListAsync(),
            Countries = await db.Countries.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationSettingsOption(x.CountryId, x.NameEnglish)).ToListAsync(),
            AddressTypes = await db.AddressBaseTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.AddressBaseTypeId, x.TypeName)).ToListAsync(),
            AddressUsages = await db.BusinessEntityLocationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.BusinessEntityLocationTypeId, x.TypeName ?? "Address")).ToListAsync(),
            ContactTypes = await db.ContactInformationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.ContactInformationTypeId, x.TypeName)).ToListAsync(),
            Islands = await db.Islands.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationAreaOption(x.IslandId, x.AtollId, x.NameEnglish)).ToListAsync(),
            Wards = await db.Wards.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationAreaOption(x.WardId, x.IslandId, x.NameEnglish)).ToListAsync(),
            DepartmentTypes = await db.OrganisationStructureTypes.AsNoTracking().OrderBy(x => x.TypeName).Select(x => new OrganisationSettingsOption(x.OrganisationStructureTypeId, x.TypeName)).ToListAsync()
        };
    }
    public async Task<OrganisationLinksDto> GetLinksAsync(int id)
    {
        await RequireAsync(id, "UserOrganisations.View", "UserOrganisations.Admin.View", "Organisation.Edit", "Organisation.Contact.View");
        await using var db = await factory.CreateDbContextAsync();
        var today = DateTime.Today;
        var now = DateTime.UtcNow;

        var activeJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == id && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= today)
            .OrderByDescending(j => j.JoinedDate).ThenByDescending(j => j.JobId)
            .Select(j => new
            {
                j.IndividualID,
                j.OrganisationStructureId,
                Name = (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim(),
                j.Staff.EmployeeNumber,
                JobType = j.JobType.TypeName
            })
            .ToListAsync();
        var activeStaffCount = activeJobs.Select(j => j.IndividualID).Distinct().Count();
        var staffCountByDept = activeJobs
            .GroupBy(j => j.OrganisationStructureId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.IndividualID).Distinct().Count());
        var departmentStaff = activeJobs
            .GroupBy(j => new { j.OrganisationStructureId, j.IndividualID })
            .Select(g => g.First())
            .Select(j => new OrganisationDepartmentStaffDto(
                j.IndividualID,
                j.OrganisationStructureId,
                string.Join(" ", j.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                j.EmployeeNumber,
                j.JobType))
            .OrderBy(s => s.Name)
            .ToList();

        var deptRows = await db.OrganisationStructures.AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == id && x.OrganisationStructureStateId == 3)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.OrganisationStructureId,
                x.Name,
                x.OrganisationStructureTypeId,
                TypeName = x.OrganisationStructureType.TypeName,
                x.OrganisationStructureStateId
            })
            .ToListAsync();
        var deptNameById = deptRows.ToDictionary(d => d.OrganisationStructureId, d => d.Name);

        var departments = deptRows.Select(x => new OrganisationDepartmentSummaryDto(
            x.OrganisationStructureId,
            x.Name,
            x.OrganisationStructureTypeId,
            x.TypeName,
            x.OrganisationStructureStateId,
            x.OrganisationStructureStateId == 3,
            staffCountByDept.GetValueOrDefault(x.OrganisationStructureId, 0)))
            .ToList();

        var teamRows = await db.Teams.AsNoTracking()
            .Where(t => t.OrganisationId == id && t.IsValid != false)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.TeamId,
                t.Name,
                t.OrganisationStructureId,
                StaffCount = db.TeamStaffs.Count(ts => ts.TeamId == t.TeamId && ts.IsValid && (ts.EndDate == null || ts.EndDate >= now))
            })
            .ToListAsync();
        var teams = teamRows.Select(t => new OrganisationTeamSummaryDto(
            t.TeamId,
            t.Name,
            t.OrganisationStructureId,
            t.OrganisationStructureId.HasValue ? deptNameById.GetValueOrDefault(t.OrganisationStructureId.Value) : null,
            t.StaffCount,
            true)).ToList();

        // Load both operational Groups (dbo.Groups) and HR StaffGroups (dbo.StaffGroups)
        var operationalGroups = await db.Groups.AsNoTracking()
            .Where(g => g.BusinessEntityOrganisationId == id)
            .OrderBy(g => g.Name)
            .Select(g => new OrganisationGroupSummaryDto(
                g.GroupId,
                g.Name,
                g.GroupType.Name,
                null,
                g.GroupStaffs.Count(),
                false,
                g.OrganisationStructureId,
                g.OrganisationStructure.Name,
                "Operational"))
            .ToListAsync();

        var staffGroups = await db.StaffGroups.AsNoTracking()
            .Where(x => x.OrganisationId == id && !x.IsDissolved)
            .OrderBy(x => x.Name)
            .Select(x => new OrganisationGroupSummaryDto(
                x.StaffGroupId,
                x.Name,
                x.Code,
                x.Description,
                x.Members.Count(m => m.IsActive),
                x.IsDissolved,
                null,
                null,
                "StaffGroup"))
            .ToListAsync();

        var teamStaffRows = await db.TeamStaffs.AsNoTracking()
            .Where(ts => ts.IsValid && (ts.EndDate == null || ts.EndDate >= now) && ts.Team.OrganisationId == id)
            .Select(ts => new
            {
                ts.TeamId,
                ts.Staff.IndividualId,
                Name = (ts.Staff.Individual.FirstNameEnglish + " " + (ts.Staff.Individual.MiddleNameEnglish ?? "") + " " + (ts.Staff.Individual.LastNameEnglish ?? "")).Trim(),
                ts.Staff.EmployeeNumber
            })
            .ToListAsync();
        var teamStaff = teamStaffRows
            .GroupBy(x => new { x.TeamId, x.IndividualId })
            .Select(g => g.First())
            .Select(x => new OrganisationMemberStaffDto(
                x.TeamId, "Team", x.IndividualId,
                string.Join(" ", x.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                x.EmployeeNumber))
            .OrderBy(x => x.Name)
            .ToList();

        var opGroupStaffRows = await db.GroupStaffs.AsNoTracking()
            .Where(gs => gs.Group.BusinessEntityOrganisationId == id)
            .Select(gs => new
            {
                gs.GroupId,
                IndividualId = gs.StaffIndividualId,
                Name = (gs.StaffIndividual.Individual.FirstNameEnglish + " " + (gs.StaffIndividual.Individual.MiddleNameEnglish ?? "") + " " + (gs.StaffIndividual.Individual.LastNameEnglish ?? "")).Trim(),
                gs.StaffIndividual.EmployeeNumber
            })
            .ToListAsync();
        var hrGroupStaffRows = await (
            from m in db.StaffGroupMembers.AsNoTracking()
            join s in db.Staffs.AsNoTracking() on m.IndividualId equals s.IndividualId
            where m.IsActive && m.StaffGroup.OrganisationId == id && !m.StaffGroup.IsDissolved
            select new
            {
                m.StaffGroupId,
                m.IndividualId,
                Name = (s.Individual.FirstNameEnglish + " " + (s.Individual.MiddleNameEnglish ?? "") + " " + (s.Individual.LastNameEnglish ?? "")).Trim(),
                s.EmployeeNumber
            }).ToListAsync();
        var groupStaff = opGroupStaffRows
            .GroupBy(x => new { x.GroupId, x.IndividualId })
            .Select(g => g.First())
            .Select(x => new OrganisationMemberStaffDto(
                x.GroupId, "Operational", x.IndividualId,
                string.Join(" ", x.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                x.EmployeeNumber))
            .Concat(hrGroupStaffRows
                .GroupBy(x => new { x.StaffGroupId, x.IndividualId })
                .Select(g => g.First())
                .Select(x => new OrganisationMemberStaffDto(
                    x.StaffGroupId, "StaffGroup", x.IndividualId,
                    string.Join(" ", x.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                    x.EmployeeNumber)))
            .OrderBy(x => x.Name)
            .ToList();

        var allGroups = operationalGroups.Concat(staffGroups).OrderBy(g => g.Name).ToList();

        return new()
        {
            ActiveStaffCount = activeStaffCount,
            Addresses = await db.BusinessEntityLocations.AsNoTracking().Where(x => x.BusinessEntityId == id && x.IsValid)
                .Select(x => new OrganisationAddressOption(x.LocationId, x.BusinessEntityLocationType.TypeName + ": " +
                    (x.Location.Address!.AddressInstance!.AddressBase.AddressLine1 ?? "") + " " +
                    (x.Location.Address.AddressInstance.AddressBase.StreetNameEnglish ?? "") + " " +
                    (x.Location.Address.AddressInstance.Floor ?? "") + " · " + x.Location.Country.NameEnglish, x.BusinessEntityLocationId)).ToListAsync(),
            Contacts = await db.BussinessEntityContactInformations.AsNoTracking().Where(x => x.BussinessEntityId == id && x.IsValid)
                .Select(x => new OrganisationContactDto { Id = x.BussinessEntityContactInformationId, TypeId = x.ContactInformationTypeId, Type = x.ContactInformationType.TypeName, Value = x.Value }).ToListAsync(),
            Departments = departments,
            DepartmentStaff = departmentStaff,
            Teams = teams,
            TeamStaff = teamStaff,
            Groups = allGroups,
            GroupStaff = groupStaff
        };
    }
    public async Task<ServiceResult> SaveDetailsAsync(OrganisationSettingsDto model)
    {
        if (model.Id == 0 && !(await access.RequireContextAsync()).IsSuperAdministrator)
            throw new UnauthorizedAccessException("Only a Super Administrator can create organisations.");
        await RequireAsync(model.Id, model.Id == 0 ? "UserOrganisations.Add" : "Organisation.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length > 250 || model.DhivehiName?.Length > 250 ||
            string.IsNullOrWhiteSpace(model.RegistrationNumber) || model.RegistrationNumber.Length > 50)
            return ServiceResult.Fail("Enter an organisation name and registration number (up to 50 characters).");
        if (model.RegistrationDate.HasValue && model.RegistrationDate.Value < new DateTime(1753, 1, 1))
            return ServiceResult.Fail("Enter a valid registration date (year 1753 or later).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            if (!await db.OrganisationTypes.AnyAsync(x => x.OrganisationTypeId == model.TypeId) || !await db.Countries.AnyAsync(x => x.CountryId == model.CountryId))
                return ServiceResult.Fail("Select an organisation type and country.");
            Organisation? existingEntity = model.Id > 0
                ? await db.Organisations.FirstOrDefaultAsync(x => x.BusinessEntityID == model.Id)
                : null;
            if (model.Id > 0 && existingEntity == null)
                return ServiceResult.Fail("Organisation was not found.");

            var trimmedReg = model.RegistrationNumber.Trim();
            var regChanged = existingEntity == null || !string.Equals(existingEntity.RegistrationNumber?.Trim(), trimmedReg, StringComparison.OrdinalIgnoreCase);
            if (regChanged)
            {
                var inactiveStateIds = await db.BusinessEntityStates.AsNoTracking()
                    .Where(s => s.StateName.Contains("Deleted") || s.StateName.Contains("Inactive") || s.StateName.Contains("Closed") || s.StateName.Contains("Disabled"))
                    .Select(s => s.BusinessEntityStateId)
                    .ToListAsync();
                var duplicateReg = await db.Organisations.AnyAsync(x =>
                    x.BusinessEntityID != model.Id &&
                    x.RegistrationNumber == trimmedReg &&
                    !db.BusinessEntities.Any(b => b.BusinessEntityID == x.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveStateIds.Contains(b.BusinessEntityStateId.Value)));
                if (duplicateReg)
                    return ServiceResult.Fail("An active organisation with this registration number already exists.");
            }
            var parentId = model.ParentId; var visited = new HashSet<int>();
            while (parentId.HasValue)
            {
                if (parentId == model.Id || !visited.Add(parentId.Value)) return ServiceResult.Fail("The parent organisation would create a hierarchy cycle.");
                var parent = await db.Organisations.AsNoTracking().Where(x => x.BusinessEntityID == parentId).Select(x => new { x.ParentOrganisationBusinessEntityId }).FirstOrDefaultAsync();
                if (parent == null) return ServiceResult.Fail("The parent organisation was not found.");
                parentId = parent.ParentOrganisationBusinessEntityId;
            }
            Organisation entity;
            if (model.Id == 0)
            {
                var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Created organisation {model.Name.Trim()}");
                var type = await db.BusinessEntityTypes.Where(x => x.TypeName == "Organisation").Select(x => x.BusinessEntityTypeId).FirstAsync();
                var business = new BusinessEntity { BusinessEntityTypeId = type, BusinessEntityStateId = 2, OperationLogId = log.OperationLogId, LastStateChangeDate = DateTime.Now };
                db.BusinessEntities.Add(business); await db.SaveChangesAsync();
                entity = new Organisation { BusinessEntityID = business.BusinessEntityID }; db.Organisations.Add(entity);
            }
            else
            {
                entity = existingEntity!;
            }
            entity.OrganisationName = model.Name.Trim();
            entity.OrganisationNameDhivehi = string.IsNullOrWhiteSpace(model.DhivehiName) ? null : model.DhivehiName.Trim();
            entity.RegistrationNumber = trimmedReg;
            entity.RegistrationDate = model.RegistrationDate;
            entity.OrganisationTypeId = model.TypeId;
            entity.CountryId = model.CountryId;
            entity.ParentOrganisationBusinessEntityId = model.ParentId;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            model.Id = entity.BusinessEntityID;
            if (contexts != null)
            {
                var affectedUserIds = await db.Users.AsNoTracking()
                    .Where(u => u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == entity.BusinessEntityID)
                    .Select(u => u.UserId)
                    .ToListAsync();
                foreach (var uid in affectedUserIds)
                    await contexts.InvalidateAsync(uid);
            }
            return ServiceResult.Ok("Organisation details saved.");
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail($"Could not save organisation details: {ex.InnerException?.Message ?? ex.Message}");
        }
    }
    public async Task<List<OrganisationSettingsOption>> SearchAddressesAsync(int organisationId, string query)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "Organisation.Contact.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return new();
        await using var db = await factory.CreateDbContextAsync(); query = query.Trim();
        return await db.AddressInstances.AsNoTracking().Where(x => (x.AddressBase.AddressLine1 ?? "").Contains(query) ||
            (x.AddressBase.StreetNameEnglish ?? "").Contains(query) || (x.Name ?? "").Contains(query))
            .OrderBy(x => x.AddressBase.AddressLine1).Take(30).Select(x => new OrganisationSettingsOption(x.LocationId,
                (x.AddressBase.AddressLine1 ?? "") + " " + (x.AddressBase.StreetNameEnglish ?? "") + " " + (x.Floor ?? ""))).ToListAsync();
    }

    public async Task<ServiceResult> SaveAddressAsync(int id, OrganisationAddressDto model)
    {
        await RequireAsync(id, "Organisation.Edit", "Organisation.Contact.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Organisations.AnyAsync(x => x.BusinessEntityID == id)) return ServiceResult.Fail("Save the organisation first.");
        if (!model.ExistingLocationId.HasValue && string.IsNullOrWhiteSpace(model.Line1)) return ServiceResult.Fail("Enter an address line.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.LOCATION_CREATE, $"Saved organisation {id} address link {model.LinkId}");
        var result = await BusinessEntityAddressWriter.SaveAsync(db, id, new AddressMutation {
            LinkId = model.LinkId, ExistingLocationId = model.ExistingLocationId, UsageId = model.UsageId,
            BaseTypeId = model.BaseTypeId, CountryId = model.CountryId, IslandId = model.IslandId, WardId = model.WardId,
            Line1 = model.Line1, Line2 = model.Line2, Street = model.Street, Floor = model.Floor, PostCode = model.PostCode, IsCurrent = true
        }, audit.OperationLogId);
        if (!result.Success) { await tx.RollbackAsync(); return result; }
        await tx.CommitAsync(); return result;
    }
    public async Task<OrganisationAddressDto> GetAddressAsync(int id, int linkId)
    {
        await RequireAsync(id, "Organisation.Edit", "Organisation.Contact.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        return await db.BusinessEntityLocations.AsNoTracking()
            .Where(x => x.BusinessEntityId == id && x.BusinessEntityLocationId == linkId && x.IsValid && x.Location.Address!.AddressInstance != null)
            .Select(x => new OrganisationAddressDto { LinkId = x.BusinessEntityLocationId, UsageId = x.BusinessEntityLocationTypeId,
                CountryId = x.Location.CountryId, IslandId = x.Location.Address!.IslandId,
                BaseTypeId = x.Location.Address.AddressInstance!.AddressBase.AddressBaseTypeId,
                WardId = x.Location.Address.AddressInstance.AddressBase.WardId,
                Line1 = x.Location.Address.AddressInstance.AddressBase.AddressLine1 ?? "",
                Line2 = x.Location.Address.AddressInstance.AddressBase.AddressLine2,
                Street = x.Location.Address.AddressInstance.AddressBase.StreetNameEnglish,
                Floor = x.Location.Address.AddressInstance.Floor, PostCode = x.Location.Address.AddressInstance.AddressBase.PostCode })
            .SingleOrDefaultAsync() ?? throw new InvalidOperationException("Address was not found for this organisation.");
    }

    public async Task<ServiceResult> DeleteAddressAsync(int id, int linkId)
    {
        await RequireAsync(id, "Organisation.Edit", "Organisation.Contact.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await BusinessEntityAddressWriter.UnlinkAsync(db, id, linkId);
        if (!result.Success) return result;
        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE, $"Removed organisation {id} address link {linkId}");
        await tx.CommitAsync(); return result;
    }
    public async Task<ServiceResult> SaveContactAsync(int id, OrganisationContactDto model)
    {
        await RequireAsync(id, "Organisation.Contact.Edit", "Organisation.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(model.Value) || model.Value.Length > 250) return ServiceResult.Fail("Enter a contact value of up to 250 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Organisations.AnyAsync(x => x.BusinessEntityID == id)) return ServiceResult.Fail("Save the organisation first.");
        var type = await db.ContactInformationTypes.Where(x => x.ContactInformationTypeId == model.TypeId).Select(x => x.TypeName).SingleOrDefaultAsync();
        if (type == null) return ServiceResult.Fail("Select a contact type.");
        if (type.Contains("email", StringComparison.OrdinalIgnoreCase) && !new EmailAddressAttribute().IsValid(model.Value.Trim())) return ServiceResult.Fail("Enter a valid email address.");
        if (await db.BussinessEntityContactInformations.AnyAsync(x => x.BussinessEntityId == id && x.ContactInformationTypeId == model.TypeId && x.Value == model.Value.Trim() && x.IsValid && x.BussinessEntityContactInformationId != model.Id)) return ServiceResult.Fail("This contact is already recorded.");
        var entity = model.Id == 0 ? new BussinessEntityContactInformation { BussinessEntityId = id } :
            await db.BussinessEntityContactInformations.SingleOrDefaultAsync(x => x.BussinessEntityId == id && x.BussinessEntityContactInformationId == model.Id);
        if (entity == null) return ServiceResult.Fail("Contact was not found for this organisation.");
        if (model.Id == 0) db.BussinessEntityContactInformations.Add(entity);
        entity.ContactInformationTypeId = model.TypeId; entity.Value = model.Value.Trim(); entity.IsValid = true;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.BussinessEntityContactInformationId;
        return ServiceResult.Ok("Contact saved.");
    }
    public async Task<List<OrganisationStaffRow>> GetStaffAsync(int id)
    {
        await RequireAsync(id, "UserOrganisations.View", "UserOrganisations.Admin.View", "Organisation.Edit", "Organisation.Contact.View");
        await using var db = await factory.CreateDbContextAsync();
        var today = FormerStaffPolicy.Today;
        var currentJobIds = (await FormerStaffPolicy.CurrentJobs(db)
            .Where(j => j.OrganisationID == id)
            .Select(j => j.JobId)
            .ToListAsync()).ToHashSet();

        var jobs = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == id)
            .Select(x => new
            {
                x.JobId,
                x.IndividualID,
                Name = x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? ""),
                x.Staff.EmployeeNumber,
                Department = x.OrganisationStructure.Name,
                JobType = x.JobType.TypeName,
                StateName = x.JobState.StateName,
                x.JoinedDate,
                x.TerminatedDate
            })
            .ToListAsync();

        var deduplicated = jobs
            .GroupBy(x => x.IndividualID)
            .Select(group =>
            {
                var currentJob = group
                    .Where(x => currentJobIds.Contains(x.JobId))
                    .OrderByDescending(x => x.JoinedDate)
                    .ThenByDescending(x => x.JobId)
                    .FirstOrDefault();

                if (currentJob is not null)
                {
                    return new OrganisationStaffRow(
                        currentJob.JobId,
                        string.Join(" ", currentJob.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                        currentJob.EmployeeNumber,
                        currentJob.Department,
                        currentJob.JobType,
                        currentJob.StateName)
                    { IndividualId = currentJob.IndividualID };
                }

                var latestJob = group
                    .OrderByDescending(x => x.TerminatedDate ?? x.JoinedDate)
                    .ThenByDescending(x => x.JobId)
                    .First();

                var status = latestJob.TerminatedDate.HasValue && latestJob.TerminatedDate.Value <= today &&
                    (latestJob.StateName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                     latestJob.StateName.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    ? "Terminated"
                    : latestJob.StateName;

                return new OrganisationStaffRow(
                    latestJob.JobId,
                    string.Join(" ", latestJob.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                    latestJob.EmployeeNumber,
                    latestJob.Department,
                    latestJob.JobType,
                    status)
                { IndividualId = latestJob.IndividualID };
            })
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.JobId)
            .ToList();

        return deduplicated;
    }
    public async Task<ServiceResult> DeleteContactAsync(int id, int contactId)
    {
        await RequireAsync(id, "Organisation.Contact.Edit", "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var contact = await db.BussinessEntityContactInformations.SingleOrDefaultAsync(x => x.BussinessEntityId == id && x.BussinessEntityContactInformationId == contactId && x.IsValid);
        if (contact == null) return ServiceResult.Fail("Contact was not found for this organisation.");
        contact.IsValid = false;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return ServiceResult.Ok("Contact deleted.");
    }
    public async Task<ServiceResult> DeleteOrganisationAsync(int id)
    {
        await RequireAsync(id, "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var organisation = await db.Organisations.SingleOrDefaultAsync(x => x.BusinessEntityID == id);
        if (organisation == null) return ServiceResult.Fail("Organisation was not found.");
        var entity = await db.BusinessEntities.SingleOrDefaultAsync(x => x.BusinessEntityID == id);
        if (entity == null) return ServiceResult.Fail("Organisation entity was not found.");

        var today = DateTime.UtcNow.AddHours(5).Date;
        var states = await db.BusinessEntityStates.AsNoTracking().ToListAsync();
        var inactiveEntityStateIds = states
            .Where(s => s.StateName.Contains("Deleted", StringComparison.OrdinalIgnoreCase) ||
                        s.StateName.Contains("Inactive", StringComparison.OrdinalIgnoreCase) ||
                        s.StateName.Contains("Closed", StringComparison.OrdinalIgnoreCase) ||
                        s.StateName.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.BusinessEntityStateId)
            .ToList();
        var inactiveDeptStateIds = await db.OrganisationStructureStates.AsNoTracking()
            .Where(s => s.StateName.Contains("Deleted") || s.StateName.Contains("Inactive") || s.StateName.Contains("Closed") || s.StateName.Contains("Archived") || s.StateName.Contains("Terminated"))
            .Select(s => s.OrganisationStructureStateId)
            .ToListAsync();

        var activeStaffCount = await db.Jobs.CountAsync(j => j.OrganisationID == id && j.JobStateId == 4 && (!j.TerminatedDate.HasValue || j.TerminatedDate > today));
        var activeDeptCount = await db.OrganisationStructures.CountAsync(s => s.OrganisationBusinessEntityID == id && !inactiveDeptStateIds.Contains(s.OrganisationStructureStateId));
        var activeTeamCount = await db.Teams.CountAsync(t => t.OrganisationId == id && t.IsValid != false);
        var activeGroupCount = await db.StaffGroups.CountAsync(g => g.OrganisationId == id && !g.IsDissolved)
            + await db.Groups.CountAsync(g => g.BusinessEntityOrganisationId == id);
        var activeChildOrgCount = await db.Organisations.CountAsync(o => o.ParentOrganisationBusinessEntityId == id &&
            !db.BusinessEntities.Any(b => b.BusinessEntityID == o.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveEntityStateIds.Contains(b.BusinessEntityStateId.Value)));
        var activeUserCount = await db.Users.CountAsync(u => u.IsActive && u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == id);

        var blockers = new List<string>();
        if (activeStaffCount > 0) blockers.Add($"{activeStaffCount} active staff");
        if (activeDeptCount > 0) blockers.Add($"{activeDeptCount} department{(activeDeptCount == 1 ? "" : "s")}");
        if (activeTeamCount > 0) blockers.Add($"{activeTeamCount} team{(activeTeamCount == 1 ? "" : "s")}");
        if (activeGroupCount > 0) blockers.Add($"{activeGroupCount} group{(activeGroupCount == 1 ? "" : "s")}");
        if (activeChildOrgCount > 0) blockers.Add($"{activeChildOrgCount} child organisation{(activeChildOrgCount == 1 ? "" : "s")}");
        if (activeUserCount > 0) blockers.Add($"{activeUserCount} active user account{(activeUserCount == 1 ? "" : "s")}");
        if (blockers.Count > 0)
            return ServiceResult.Fail($"Cannot delete organisation while dependencies exist: {string.Join(", ", blockers)}. Clear or reassign all dependencies first.");

        var inactiveState = states.FirstOrDefault(s =>
            s.StateName.Contains("Deleted", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Inactive", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Closed", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            ?? states.FirstOrDefault(s => s.BusinessEntityStateId != 2);
        if (inactiveState == null) return ServiceResult.Fail("No inactive organisation state is configured.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Soft-deleted organisation {organisation.OrganisationName} (#{id})");
        entity.BusinessEntityStateId = inactiveState.BusinessEntityStateId;
        entity.LastStateChangeDate = DateTime.Now;
        entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Organisation soft-deleted.");
    }

    public async Task<ServiceResult> SaveDepartmentAsync(int organisationId, int departmentId, string name, int typeId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 250)
            return ServiceResult.Fail("Enter a department name (up to 250 characters).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.OrganisationStructureTypes.AnyAsync(x => x.OrganisationStructureTypeId == typeId))
            return ServiceResult.Fail("Select a valid department type.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Saved department '{name.Trim()}' for organisation #{organisationId}");
        if (departmentId == 0)
        {
            db.OrganisationStructures.Add(new OrganisationStructure
            {
                OrganisationBusinessEntityID = organisationId,
                Name = name.Trim(),
                OrganisationStructureTypeId = typeId,
                OrganisationStructureStateId = 3,
                OperationLogId = log.OperationLogId
            });
        }
        else
        {
            var dept = await db.OrganisationStructures.SingleOrDefaultAsync(x => x.OrganisationStructureId == departmentId && x.OrganisationBusinessEntityID == organisationId);
            if (dept == null) return ServiceResult.Fail("Department was not found in this organisation.");
            dept.Name = name.Trim();
            dept.OrganisationStructureTypeId = typeId;
            dept.OperationLogId = log.OperationLogId;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Department saved.");
    }

    public async Task<ServiceResult> SoftDeleteDepartmentAsync(int organisationId, int departmentId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var dept = await db.OrganisationStructures.SingleOrDefaultAsync(x => x.OrganisationStructureId == departmentId && x.OrganisationBusinessEntityID == organisationId);
        if (dept == null) return ServiceResult.Fail("Department was not found in this organisation.");
        var states = await db.OrganisationStructureStates.AsNoTracking().ToListAsync();
        var inactiveState = states.FirstOrDefault(s =>
            s.StateName.Contains("Deleted", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Inactive", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Closed", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Archived", StringComparison.OrdinalIgnoreCase) ||
            s.StateName.Contains("Terminated", StringComparison.OrdinalIgnoreCase))
            ?? states.FirstOrDefault(s => s.OrganisationStructureStateId != 3);
        if (inactiveState == null) return ServiceResult.Fail("No inactive department state is configured.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Soft-deleted department '{dept.Name}' (#{departmentId})");
        dept.OrganisationStructureStateId = inactiveState.OrganisationStructureStateId;
        dept.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Department soft-deleted.");
    }

    public async Task<ServiceResult> SaveGroupSummaryAsync(int organisationId, int groupId, string name, string? code, string? description)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return ServiceResult.Fail("Enter a group name (up to 200 characters).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var staffGroup = await db.StaffGroups.FirstOrDefaultAsync(x => x.StaffGroupId == groupId && x.OrganisationId == organisationId && !x.IsDissolved);
        if (staffGroup != null)
        {
            staffGroup.Name = name.Trim();
            staffGroup.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
            staffGroup.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            staffGroup.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Group saved.");
        }
        var opGroup = await db.Groups.FirstOrDefaultAsync(x => x.GroupId == groupId && x.BusinessEntityOrganisationId == organisationId);
        if (opGroup == null) return ServiceResult.Fail("Group was not found in this organisation.");
        opGroup.Name = name.Trim();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Group saved.");
    }

    public async Task<ServiceResult> SoftDeleteGroupAsync(int organisationId, int groupId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var staffGroup = await db.StaffGroups.FirstOrDefaultAsync(x => x.StaffGroupId == groupId && x.OrganisationId == organisationId && !x.IsDissolved);
        if (staffGroup != null)
        {
            staffGroup.IsDissolved = true;
            staffGroup.DissolvedAtUtc = DateTime.UtcNow;
            staffGroup.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Group soft-deleted.");
        }
        var opGroup = await db.Groups.Include(g => g.GroupStaffs).FirstOrDefaultAsync(x => x.GroupId == groupId && x.BusinessEntityOrganisationId == organisationId);
        if (opGroup == null) return ServiceResult.Fail("Group was not found in this organisation.");
        if (opGroup.GroupStaffs.Any())
            return ServiceResult.Fail("Cannot delete an operational group while staff members are assigned to it.");
        db.Groups.Remove(opGroup);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Operational group removed.");
    }

    public async Task<ServiceResult> SaveTeamSummaryAsync(int organisationId, int teamId, string name, int? departmentId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return ServiceResult.Fail("Enter a team name (up to 200 characters).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (departmentId.HasValue && departmentId.Value > 0 &&
            !await db.OrganisationStructures.AnyAsync(s => s.OrganisationStructureId == departmentId.Value && s.OrganisationBusinessEntityID == organisationId && s.OrganisationStructureStateId == 3))
            return ServiceResult.Fail("Select a valid department in this organisation.");
        if (teamId == 0)
        {
            var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Created team '{name.Trim()}' for organisation #{organisationId}");
            db.Teams.Add(new Team
            {
                OrganisationId = organisationId,
                Name = name.Trim(),
                OrganisationStructureId = departmentId is > 0 ? departmentId.Value : null,
                IsValid = true,
                OperationLogId = log.OperationLogId
            });
        }
        else
        {
            var team = await db.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId && t.OrganisationId == organisationId && t.IsValid != false);
            if (team == null) return ServiceResult.Fail("Team was not found in this organisation.");
            team.Name = name.Trim();
            team.OrganisationStructureId = departmentId is > 0 ? departmentId.Value : null;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Team saved.");
    }

    public async Task<ServiceResult> SoftDeleteTeamAsync(int organisationId, int teamId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var team = await db.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId && t.OrganisationId == organisationId && t.IsValid != false);
        if (team == null) return ServiceResult.Fail("Team was not found in this organisation.");
        team.IsValid = false;
        team.EndDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Team soft-deleted.");
    }
}
