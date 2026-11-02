using System.Data;
using HRM.DTOs;
using HRM.DTOs.Items;
using HRM.DTOs.Organisation;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class ItemCatalogueService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs,
    IRequestService requests, HRM.Services.Interfaces.AppovalWF.IWorkflowEngine workflow,
    HRM.Services.Interfaces.AppovalWF.IWorkflowApproverResolver approvers,
    HRM.Services.Interfaces.AppovalWF.IWorkflowRequestContextResolver contexts)
: global::HRM.Services.Contracts.ItemCatalogueServiceContracts, global::HRM.Services.Contracts.ItemCatalogueServiceContracts.IOperations
{
    private static readonly string[] UnitStatuses = ["Available", "Issued", "Returned", "Damaged", "Lost", "Retired"];
    private static readonly string[] UnitConditions = ["Good", "Fair", "Damaged", "Unusable"];

    private async Task<(int Org, bool Super, bool Edit)> ScopeAsync(int? selected)
    {
        var actor = await access.RequireContextAsync();
        var super = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = super && selected is > 0 ? selected.Value : current;
        if (org <= 0 || !super && (selected.HasValue && selected != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation to manage items.");
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Organisations.AsNoTracking().AnyAsync(o => o.BusinessEntityID == org))
            throw new InvalidOperationException("Organisation not found.");
        var edit = super || await access.HasPermissionAsync("Admin");
        if (!edit && !await access.HasPermissionAsync("Employee.View") && !await access.HasPermissionAsync("Organisation.Edit") &&
            !await access.HasPermissionAsync("UserOrganisations.View"))
            throw new UnauthorizedAccessException("You cannot view the item catalogue.");
        return (org, super, edit);
    }

    public async Task<ItemCatalogueData> LoadAsync(int? organisationId = null)
    {
        var (org, super, edit) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var data = new ItemCatalogueData
        {
            OrganisationId = org,
            IsSuperAdmin = super,
            CanEdit = edit,
            OrganisationName = await db.Organisations.AsNoTracking().Where(o => o.BusinessEntityID == org)
                .Select(o => o.OrganisationName).SingleAsync(),
            Organisations = await db.Organisations.AsNoTracking().Where(o => super || o.BusinessEntityID == org)
                .OrderBy(o => o.OrganisationName).Select(o => new OrganisationSettingsOption(o.BusinessEntityID, o.OrganisationName)).ToListAsync(),
            Categories = await db.ItemCategories.AsNoTracking().Where(c => c.OrganisationId == org).OrderBy(c => c.Name)
                .Select(c => new ItemCategoryDto(c.ItemCategoryId, c.Name, c.Description, c.IsActive, c.Items.Count, c.Version)).ToListAsync(),
            Items = await db.Items.AsNoTracking().Where(i => i.OrganisationId == org).OrderBy(i => i.Category.Name).ThenBy(i => i.Name)
                .Select(i => new ItemDto(i.ItemId, i.ItemCategoryId, i.Category.Name, i.Name, i.Description, i.Kind, i.UnitOfMeasure,
                    i.Brand, i.Model, i.QuantityOnHand, i.ReorderLevel, i.Units.Count,
                    i.Units.Count(u => u.Status == "Available"), i.IsActive, i.Version)).ToListAsync()
        };
        return data;
    }

    public async Task<List<ItemUnitDto>> GetItemUnitsAsync(int itemId, int? organisationId = null)
    {
        var (org, _, _) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Items.AsNoTracking().AnyAsync(i => i.ItemId == itemId && i.OrganisationId == org))
            throw new InvalidOperationException("Select an item in your organisation.");
        return await db.ItemUnits.AsNoTracking().Where(u => u.ItemId == itemId && u.OrganisationId == org)
            .OrderBy(u => u.InventoryNumber)
            .Select(u => new ItemUnitDto(u.ItemUnitId, u.ItemId, u.InventoryNumber, u.SerialNumber, u.Status, u.Condition,
                u.IssuedToIndividualId,
                u.IssuedToIndividualId == null ? null : db.Individuals.Where(i => i.BusinessEntityId == u.IssuedToIndividualId)
                    .Select(i => i.FirstNameEnglish + " " + (i.LastNameEnglish ?? "")).FirstOrDefault(),
                u.IssuedDate, u.CollectedDate, u.Notes, u.Version))
            .ToListAsync();
    }

    public async Task<ServiceResult> SaveCategoryAsync(ItemCategoryInput model, bool delete = false)
    {
        var (org, _, edit) = await ScopeAsync(model.OrganisationId > 0 ? model.OrganisationId : null);
        if (!edit) throw new UnauthorizedAccessException("You cannot manage the item catalogue.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var category = model.ItemCategoryId > 0
            ? await db.ItemCategories.SingleOrDefaultAsync(c => c.ItemCategoryId == model.ItemCategoryId && c.OrganisationId == org)
            : null;
        if (model.ItemCategoryId > 0 && category is null) return ServiceResult.Fail("This category no longer exists.");
        if (category is not null && model.Version > 0 && category.Version != model.Version)
            return ServiceResult.Fail("This category changed. Refresh before saving.");
        if (delete)
        {
            if (category is null) return ServiceResult.Fail("This category no longer exists.");
            if (await db.Items.AnyAsync(i => i.ItemCategoryId == category.ItemCategoryId))
                return ServiceResult.Fail("Move or delete the items in this category before deleting it.");
            var removed = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
                $"Deleted item category {category.ItemCategoryId}: {category.Name}");
            category.OperationLogId = removed.OperationLogId;
            db.ItemCategories.Remove(category);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok("Category deleted.");
        }
        var name = (model.Name ?? "").Trim();
        var description = (model.Description ?? "").Trim();
        if (name.Length is 0 or > 200) return ServiceResult.Fail("Enter a category name of up to 200 characters.");
        if (description.Length > 1000) return ServiceResult.Fail("Category description must be 1000 characters or fewer.");
        var duplicate = await db.ItemCategories.AnyAsync(c => c.OrganisationId == org && c.ItemCategoryId != model.ItemCategoryId &&
            c.Name.ToLower() == name.ToLower());
        if (duplicate) return ServiceResult.Fail("This category name already exists.");
        var log = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
            $"{(category is null ? "Created" : "Updated")} item category: {name}");
        if (category is null)
        {
            category = new ItemCategory { OrganisationId = org, CreatedAtUtc = DateTime.UtcNow };
            db.ItemCategories.Add(category);
        }
        category.Name = name;
        category.Description = description;
        category.IsActive = model.IsActive;
        category.Version++;
        category.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return ServiceResult.Ok("Category saved.");
    }

    public async Task<ServiceResult> SaveItemAsync(ItemInput model, bool delete = false)
    {
        var (org, _, edit) = await ScopeAsync(model.OrganisationId > 0 ? model.OrganisationId : null);
        if (!edit) throw new UnauthorizedAccessException("You cannot manage the item catalogue.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = model.ItemId > 0
            ? await db.Items.SingleOrDefaultAsync(i => i.ItemId == model.ItemId && i.OrganisationId == org)
            : null;
        if (model.ItemId > 0 && item is null) return ServiceResult.Fail("This item no longer exists.");
        if (item is not null && model.Version > 0 && item.Version != model.Version)
            return ServiceResult.Fail("This item changed. Refresh before saving.");
        if (delete)
        {
            if (item is null) return ServiceResult.Fail("This item no longer exists.");
            if (await db.ItemUnits.AnyAsync(u => u.ItemId == item.ItemId))
                return ServiceResult.Fail("Delete or retire the inventory units for this item before deleting it.");
            if (await db.ItemStaffRequests.AnyAsync(r => r.ItemId == item.ItemId))
                return ServiceResult.Fail("Items with requisition or issue history cannot be deleted.");
            var removed = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
                $"Deleted item {item.ItemId}: {item.Name}");
            item.OperationLogId = removed.OperationLogId;
            db.Items.Remove(item);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok("Item deleted.");
        }
        var name = (model.Name ?? "").Trim();
        var description = (model.Description ?? "").Trim();
        if (name.Length is 0 or > 200) return ServiceResult.Fail("Enter an item name of up to 200 characters.");
        if (description.Length > 1000) return ServiceResult.Fail("Item description must be 1000 characters or fewer.");
        var kind = (model.Kind ?? "").Trim();
        if (kind is not ("Asset" or "Consumable")) return ServiceResult.Fail("Choose the item kind: Asset or Consumable.");
        var unit = (model.UnitOfMeasure ?? "").Trim();
        if (unit.Length is 0 or > 50) return ServiceResult.Fail("Enter a unit of measure of up to 50 characters.");
        var brand = string.IsNullOrWhiteSpace(model.Brand) ? null : model.Brand.Trim();
        var itemModel = string.IsNullOrWhiteSpace(model.Model) ? null : model.Model.Trim();
        if (brand?.Length > 120 || itemModel?.Length > 120) return ServiceResult.Fail("Brand and model must be 120 characters or fewer.");
        if (model.QuantityOnHand < 0 || model.ReorderLevel < 0) return ServiceResult.Fail("Stock and reorder levels cannot be negative.");
        if (!await db.ItemCategories.AnyAsync(c => c.ItemCategoryId == model.ItemCategoryId && c.OrganisationId == org))
            return ServiceResult.Fail("Select a category in your organisation.");
        var duplicate = await db.Items.AnyAsync(i => i.OrganisationId == org && i.ItemCategoryId == model.ItemCategoryId &&
            i.ItemId != model.ItemId && i.Name.ToLower() == name.ToLower());
        if (duplicate) return ServiceResult.Fail("This item name already exists in the selected category.");
        var log = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
            $"{(item is null ? "Created" : "Updated")} item: {name}");
        if (item is null)
        {
            item = new Item { OrganisationId = org, CreatedAtUtc = DateTime.UtcNow };
            db.Items.Add(item);
        }
        item.ItemCategoryId = model.ItemCategoryId;
        item.Name = name;
        item.Description = description;
        item.Kind = kind;
        item.UnitOfMeasure = unit;
        item.Brand = brand;
        item.Model = itemModel;
        item.QuantityOnHand = model.QuantityOnHand;
        item.ReorderLevel = model.ReorderLevel;
        item.IsActive = model.IsActive;
        item.Version++;
        item.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return ServiceResult.Ok("Item saved.");
    }

    public async Task<ServiceResult> SaveUnitAsync(ItemUnitInput model, bool delete = false)
    {
        var (org, _, edit) = await ScopeAsync(model.OrganisationId > 0 ? model.OrganisationId : null);
        if (!edit) throw new UnauthorizedAccessException("You cannot manage inventory units.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var unitRow = model.ItemUnitId > 0
            ? await db.ItemUnits.SingleOrDefaultAsync(u => u.ItemUnitId == model.ItemUnitId && u.OrganisationId == org)
            : null;
        if (model.ItemUnitId > 0 && unitRow is null) return ServiceResult.Fail("This inventory unit no longer exists.");
        if (unitRow is not null && model.Version > 0 && unitRow.Version != model.Version)
            return ServiceResult.Fail("This inventory unit changed. Refresh before saving.");
        if (delete)
        {
            if (unitRow is null) return ServiceResult.Fail("This inventory unit no longer exists.");
            if (unitRow.Status == "Issued") return ServiceResult.Fail("Collect or return this unit before deleting it.");
            if (await db.ItemStaffRequests.AnyAsync(r => r.ItemUnitId == unitRow.ItemUnitId))
                return ServiceResult.Fail("Inventory units with issue history cannot be deleted.");
            var removed = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
                $"Deleted inventory unit {unitRow.ItemUnitId}: {unitRow.InventoryNumber}");
            unitRow.OperationLogId = removed.OperationLogId;
            db.ItemUnits.Remove(unitRow);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok("Inventory unit deleted.");
        }
        var inventoryNumber = (model.InventoryNumber ?? "").Trim();
        var serialNumber = string.IsNullOrWhiteSpace(model.SerialNumber) ? null : model.SerialNumber.Trim();
        var notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        if (inventoryNumber.Length is 0 or > 100) return ServiceResult.Fail("Enter an inventory number of up to 100 characters.");
        if (serialNumber?.Length > 100) return ServiceResult.Fail("Serial number must be 100 characters or fewer.");
        if (notes?.Length > 1000) return ServiceResult.Fail("Notes must be 1000 characters or fewer.");
        var status = (model.Status ?? "").Trim();
        var condition = (model.Condition ?? "").Trim();
        if (!UnitStatuses.Contains(status)) return ServiceResult.Fail("Choose a valid unit status.");
        if (!UnitConditions.Contains(condition)) return ServiceResult.Fail("Choose a valid unit condition.");
        var item = await db.Items.SingleOrDefaultAsync(i => i.ItemId == model.ItemId && i.OrganisationId == org);
        if (item is null) return ServiceResult.Fail("Select an item in your organisation.");
        if (!item.Kind.Equals(Contracts.ItemCatalogueServiceContracts.AssetKind, StringComparison.Ordinal))
            return ServiceResult.Fail("Inventory units can only be added to asset items.");
        var duplicate = await db.ItemUnits.AnyAsync(u => u.OrganisationId == org && u.ItemUnitId != model.ItemUnitId &&
            u.InventoryNumber.ToLower() == inventoryNumber.ToLower());
        if (duplicate) return ServiceResult.Fail("This inventory number is already assigned.");
        if (model.IssuedToIndividualId is > 0 &&
            !await db.Individuals.AnyAsync(i => i.BusinessEntityId == model.IssuedToIndividualId))
            return ServiceResult.Fail("Select a valid staff member.");
        if (status == "Issued" || unitRow?.Status == "Issued" || model.IssuedToIndividualId is > 0)
            return ServiceResult.Fail("Issue and return inventory units through the approved staff item workflow.");
        var issuedTo = status == "Available" ? null : model.IssuedToIndividualId;
        var issuedDate = status == "Available" ? null : model.IssuedDate;
        var collectedDate = status == "Available" ? null : model.CollectedDate;
        if (status == "Issued" && (issuedTo is null || issuedDate is null))
            return ServiceResult.Fail("An issued unit needs the staff member and issue date.");
        if (status == "Issued") collectedDate = null;
        if (issuedDate.HasValue && collectedDate.HasValue && collectedDate < issuedDate)
            return ServiceResult.Fail("The collected date cannot be before the issue date.");
        var log = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
            $"{(unitRow is null ? "Created" : "Updated")} inventory unit: {inventoryNumber}");
        if (unitRow is null)
        {
            unitRow = new ItemUnit { OrganisationId = org, CreatedAtUtc = DateTime.UtcNow };
            db.ItemUnits.Add(unitRow);
        }
        unitRow.ItemId = model.ItemId;
        unitRow.InventoryNumber = inventoryNumber;
        unitRow.SerialNumber = serialNumber;
        unitRow.Status = status;
        unitRow.Condition = condition;
        unitRow.IssuedToIndividualId = issuedTo;
        unitRow.IssuedDate = issuedDate;
        unitRow.CollectedDate = collectedDate;
        unitRow.Notes = notes;
        unitRow.Version++;
        unitRow.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return ServiceResult.Ok("Inventory unit saved.");
    }
}
