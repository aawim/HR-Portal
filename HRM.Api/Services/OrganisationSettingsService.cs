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

public partial class OrganisationSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
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
        return new()
        {
            Addresses = await db.BusinessEntityLocations.AsNoTracking().Where(x => x.BusinessEntityId == id && x.IsValid)
                .Select(x => new OrganisationAddressOption(x.LocationId, x.BusinessEntityLocationType.TypeName + ": " +
                    (x.Location.Address!.AddressInstance!.AddressBase.AddressLine1 ?? "") + " " +
                    (x.Location.Address.AddressInstance.AddressBase.StreetNameEnglish ?? "") + " " +
                    (x.Location.Address.AddressInstance.Floor ?? "") + " · " + x.Location.Country.NameEnglish, x.BusinessEntityLocationId)).ToListAsync(),
            Contacts = await db.BussinessEntityContactInformations.AsNoTracking().Where(x => x.BussinessEntityId == id && x.IsValid)
                .Select(x => new OrganisationContactDto { Id = x.BussinessEntityContactInformationId, TypeId = x.ContactInformationTypeId, Type = x.ContactInformationType.TypeName, Value = x.Value }).ToListAsync(),
            Departments = await db.OrganisationStructures.AsNoTracking()
                .Where(x => x.OrganisationBusinessEntityID == id && x.OrganisationStructureStateId == 3)
                .OrderBy(x => x.Name)
                .Select(x => new OrganisationDepartmentSummaryDto(
                    x.OrganisationStructureId,
                    x.Name,
                    x.OrganisationStructureTypeId,
                    x.OrganisationStructureType.TypeName,
                    x.OrganisationStructureStateId,
                    x.OrganisationStructureStateId == 3))
                .ToListAsync(),
            Groups = await db.StaffGroups.AsNoTracking()
                .Where(x => x.OrganisationId == id && !x.IsDissolved)
                .OrderBy(x => x.Name)
                .Select(x => new OrganisationGroupSummaryDto(
                    x.StaffGroupId,
                    x.Name,
                    x.Code,
                    x.Description,
                    x.Members.Count(m => m.IsActive),
                    x.IsDissolved))
                .ToListAsync()
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
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.OrganisationTypes.AnyAsync(x => x.OrganisationTypeId == model.TypeId) || !await db.Countries.AnyAsync(x => x.CountryId == model.CountryId))
            return ServiceResult.Fail("Select an organisation type and country.");
        if (await db.Organisations.AnyAsync(x => x.BusinessEntityID != model.Id && x.RegistrationNumber == model.RegistrationNumber.Trim()))
            return ServiceResult.Fail("An organisation with this registration number already exists. Select it from the list.");
        var parentId = model.ParentId; var visited = new HashSet<int>();
        while (parentId.HasValue)
        {
            if (parentId == model.Id || !visited.Add(parentId.Value)) return ServiceResult.Fail("The parent organisation would create a hierarchy cycle.");
            var parent = await db.Organisations.AsNoTracking().Where(x => x.BusinessEntityID == parentId).Select(x => new { x.ParentOrganisationBusinessEntityId }).SingleOrDefaultAsync();
            if (parent == null) return ServiceResult.Fail("The parent organisation was not found.");
            parentId = parent.ParentOrganisationBusinessEntityId;
        }
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE, $"Saved organisation {model.Name}");
        Organisation? entity;
        if (model.Id == 0)
        {
            var type = await db.BusinessEntityTypes.Where(x => x.TypeName == "Organisation").Select(x => x.BusinessEntityTypeId).SingleAsync();
            var business = new BusinessEntity { BusinessEntityTypeId = type, BusinessEntityStateId = 2, OperationLogId = log.OperationLogId, LastStateChangeDate = DateTime.Now };
            db.BusinessEntities.Add(business); await db.SaveChangesAsync();
            entity = new Organisation { BusinessEntityID = business.BusinessEntityID }; db.Organisations.Add(entity);
        }
        else entity = await db.Organisations.SingleOrDefaultAsync(x => x.BusinessEntityID == model.Id);
        if (entity == null) return ServiceResult.Fail("Organisation was not found.");
        entity.OrganisationName = model.Name.Trim(); entity.OrganisationNameDhivehi = model.DhivehiName?.Trim(); entity.RegistrationNumber = model.RegistrationNumber.Trim();
        entity.RegistrationDate = model.RegistrationDate; entity.OrganisationTypeId = model.TypeId; entity.CountryId = model.CountryId; entity.ParentOrganisationBusinessEntityId = model.ParentId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.BusinessEntityID;
        return ServiceResult.Ok("Organisation details saved. You can now add addresses and contacts.");
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
        var states = await db.BusinessEntityStates.AsNoTracking().ToListAsync();
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
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var group = await db.StaffGroups.SingleOrDefaultAsync(x => x.StaffGroupId == groupId && x.OrganisationId == organisationId && !x.IsDissolved);
        if (group == null) return ServiceResult.Fail("Group was not found in this organisation.");
        group.Name = name.Trim();
        group.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        group.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        group.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Group saved.");
    }

    public async Task<ServiceResult> SoftDeleteGroupAsync(int organisationId, int groupId)
    {
        await RequireAsync(organisationId, "Organisation.Edit", "UserOrganisations.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var group = await db.StaffGroups.SingleOrDefaultAsync(x => x.StaffGroupId == groupId && x.OrganisationId == organisationId && !x.IsDissolved);
        if (group == null) return ServiceResult.Fail("Group was not found in this organisation.");
        group.IsDissolved = true;
        group.DissolvedAtUtc = DateTime.UtcNow;
        group.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok("Group soft-deleted.");
    }
}
