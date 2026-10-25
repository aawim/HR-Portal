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
{
    private async Task<bool> GlobalAsync(UserContextDto user) => user.IsSuperAdministrator ||
        await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("UserOrganisations.Admin.View") || await access.HasPermissionAsync("UserOrganisations.Edit");
    private async Task RequireAsync(int id, params string[] keys)
    {
        var user = await access.RequireContextAsync();
        var allowed = user.IsSuperAdministrator || await access.HasPermissionAsync("Admin");
        foreach (var key in keys) allowed |= await access.HasPermissionAsync(key);
        if (!allowed) throw new UnauthorizedAccessException("You do not have permission to manage organisation records.");
        if (id > 0 && !await GlobalAsync(user) && !await access.HasOrganisationAccessAsync(id))
            throw new UnauthorizedAccessException("You cannot manage this organisation.");
    }
    public async Task<OrganisationSettingsData> LoadAsync()
    {
        await RequireAsync(0, "UserOrganisations.View", "UserOrganisations.Admin.View", "Organisation.Edit", "Organisation.Contact.View");
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var organisations = await db.Organisations.AsNoTracking().OrderBy(x => x.OrganisationName).Select(x => new OrganisationSettingsDto
        { Id = x.BusinessEntityID, Name = x.OrganisationName, DhivehiName = x.OrganisationNameDhivehi, RegistrationNumber = x.RegistrationNumber ?? "",
            RegistrationDate = x.RegistrationDate, TypeId = x.OrganisationTypeId, CountryId = x.CountryId, ParentId = x.ParentOrganisationBusinessEntityId }).ToListAsync();
        if (!await GlobalAsync(user))
        {
            var scoped = new List<OrganisationSettingsDto>();
            foreach (var item in organisations) if (await access.HasOrganisationAccessAsync(item.Id)) scoped.Add(item);
            organisations = scoped;
        }
        return new()
        {
            Organisations = organisations,
            Types = await db.OrganisationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.OrganisationTypeId, x.TypeName)).ToListAsync(),
            Countries = await db.Countries.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationSettingsOption(x.CountryId, x.NameEnglish)).ToListAsync(),
            AddressTypes = await db.AddressBaseTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.AddressBaseTypeId, x.TypeName)).ToListAsync(),
            AddressUsages = await db.BusinessEntityLocationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.BusinessEntityLocationTypeId, x.TypeName ?? "Address")).ToListAsync(),
            ContactTypes = await db.ContactInformationTypes.AsNoTracking().Select(x => new OrganisationSettingsOption(x.ContactInformationTypeId, x.TypeName)).ToListAsync(),
            Islands = await db.Islands.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationAreaOption(x.IslandId, x.AtollId, x.NameEnglish)).ToListAsync(),
            Wards = await db.Wards.AsNoTracking().OrderBy(x => x.NameEnglish).Select(x => new OrganisationAreaOption(x.WardId, x.IslandId, x.NameEnglish)).ToListAsync()
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
                .Select(x => new OrganisationContactDto { Id = x.BussinessEntityContactInformationId, TypeId = x.ContactInformationTypeId, Type = x.ContactInformationType.TypeName, Value = x.Value }).ToListAsync()
        };
    }
    public async Task<ServiceResult> SaveDetailsAsync(OrganisationSettingsDto model)
    {
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
        var rows = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == id)
            .OrderBy(x => x.Individual.FirstNameEnglish).ThenBy(x => x.JobId)
            .Select(x => new OrganisationStaffRow(x.JobId,
                x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? ""),
                x.Staff.EmployeeNumber, x.OrganisationStructure.Name, x.JobType.TypeName, x.JobState.StateName) { IndividualId = x.IndividualID })
            .ToListAsync();
        return rows.Select(row => row with { Name = string.Join(" ", row.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) }).ToList();
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
        if (await db.Jobs.AnyAsync(x => x.OrganisationID == id))
            return ServiceResult.Fail("Cannot delete this organisation because employees are attached, including employment history.");
        if (await db.OrganisationStructures.AnyAsync(x => x.OrganisationBusinessEntityID == id))
            return ServiceResult.Fail("Cannot delete this organisation because departments are attached.");
        if (await db.Organisations.AnyAsync(x => x.ParentOrganisationBusinessEntityId == id))
            return ServiceResult.Fail("Cannot delete this organisation because child organisations are attached.");
        // Prevent cascading removal of linked operational and configuration records.
        foreach (var collection in db.Entry(organisation).Collections)
            if (await collection.Query().Cast<object>().AnyAsync())
                return ServiceResult.Fail("Cannot delete this organisation because other records are attached. Remove those links first.");
        if (await db.Entry(organisation).Reference(x => x.OnlineSignInOrganisation).Query().AnyAsync())
            return ServiceResult.Fail("Cannot delete this organisation because sign-in configuration is attached.");
        db.Organisations.Remove(organisation);
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        { return ServiceResult.Fail("Cannot delete this organisation because other records are attached."); }
        return ServiceResult.Ok("Organisation deleted.");
    }
}
