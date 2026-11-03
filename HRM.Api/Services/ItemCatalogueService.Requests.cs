using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Items;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class ItemCatalogueService
{
    private WorkflowRequestProcessing ItemProcessing => new(approvers, contexts);
    private static WorkflowRequestSubject Subject(ItemStaffRequest r) => new(r.Request, r.JobId, r.IndividualId, r.OrganisationId);

    private async Task<(int Org, int Person, int User, bool Manage)> RequestScopeAsync(int? selected = null)
    {
        var actor = await access.RequireContextAsync();
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = actor.IsSuperAdministrator && selected is > 0 ? selected.Value : current;
        if (org <= 0 || !actor.IsSuperAdministrator && (selected.HasValue && selected != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Select your current organisation.");
        return (org, actor.IndividualId, actor.UserId, actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin"));
    }

    public async Task<List<RequestableItemDto>> GetRequestableItemsAsync(int? organisationId = null)
    {
        var actor = await RequestScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        return await db.Items.AsNoTracking().Where(i => i.OrganisationId == actor.Org && i.IsActive && i.Category.IsActive)
            .OrderBy(i => i.Name).Select(i => new RequestableItemDto(i.ItemId, i.OrganisationId, i.Name, i.Kind, i.UnitOfMeasure,
                i.Kind == "Asset" ? i.Units.Count(u => u.Status == "Available") : i.QuantityOnHand)).ToListAsync();
    }

    public async Task<bool> CanManageStaffItemsAsync() => (await RequestScopeAsync()).Manage;

    public async Task<List<string>> GetRequestedItemNamesAsync(int? individualId = null)
    {
        var actor = await RequestScopeAsync();
        var person = individualId.GetValueOrDefault(actor.Person);
        if (person != actor.Person && !actor.Manage) throw new UnauthorizedAccessException("You can view only your requested items.");
        await using var db = await factory.CreateDbContextAsync();
        return (await db.ItemStaffRequestLines.AsNoTracking().Where(l => l.Request.OrganisationId == actor.Org &&
                l.Request.IndividualId == person && l.Request.Kind == "Requisition")
            .Select(l => l.ItemName).ToListAsync()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task<List<ItemStaffRequestDto>> GetStaffRequestsAsync(int? organisationId = null, int? individualId = null)
        => LoadStaffRequestsAsync(organisationId, individualId, false);

    public Task<List<ItemStaffRequestDto>> GetPendingItemApprovalsAsync()
        => LoadStaffRequestsAsync(null, null, true);

    private async Task<List<ItemStaffRequestDto>> LoadStaffRequestsAsync(int? organisationId, int? individualId, bool approvalsOnly)
    {
        var actor = await RequestScopeAsync(organisationId);
        var person = individualId.GetValueOrDefault(approvalsOnly || actor.Manage ? 0 : actor.Person);
        if (person > 0 && person != actor.Person && !actor.Manage)
            throw new UnauthorizedAccessException("You can view only your item requests.");
        await using var db = await factory.CreateDbContextAsync();
        var rows = await db.ItemStaffRequests.AsNoTracking().Include(r => r.Request).ThenInclude(r => r.RequestState)
            .Include(r => r.Lines)
            .Where(r => r.OrganisationId == actor.Org && (person == 0 || r.IndividualId == person) &&
                (approvalsOnly
                    ? db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.User && a.Decision == "Pending" &&
                        a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)
                    : actor.Manage || r.IndividualId == actor.Person))
            .OrderByDescending(r => r.CreatedAtUtc).Take(200).ToListAsync();
        var result = new List<ItemStaffRequestDto>(rows.Count);
        foreach (var row in rows)
        {
            var instance = await WorkflowRequestProcessing.Instance(db, row.RequestId);
            var creator = await db.WfRequestActions.AsNoTracking().Where(a => a.WorkflowRequestInstance.RequestId == row.RequestId && a.ActionCode == "START")
                .Select(a => (int?)a.PerformedByUserId).FirstOrDefaultAsync();
            var name = await db.Individuals.Where(i => i.BusinessEntityId == row.IndividualId)
                .Select(i => i.FirstNameEnglish + " " + (i.LastNameEnglish ?? "")).SingleAsync();
            var canApprove = creator != actor.User && await ItemProcessing.CanApprove(db, Subject(row), instance, actor.User, actor.Person);
            if (approvalsOnly && !canApprove) continue;
            result.Add(new(row.RequestId, row.Request.RequestStateId, row.Request.ReferenceNumber ?? $"IM-{row.RequestId}",
                row.IndividualId, name, row.Purpose, row.Kind,
                row.Reason, row.Request.RequestState.StateName.Trim(), row.CreatedAtUtc, row.IssuedAtUtc, row.ReturnedAtUtc,
                canApprove,
                row.IndividualId == actor.Person && instance?.WorkflowStatus == "InProgress",
                row.Lines.OrderBy(l => l.ItemStaffRequestLineId)
                    .Select(l => new ItemStaffRequestLineDto(
                        l.ItemStaffRequestLineId,
                        l.ItemId,
                        l.ItemUnitId,
                        l.ItemName,
                        l.InventoryNumber,
                        l.SerialNumber,
                        l.Condition,
                        l.ExpectedReturnDate,
                        l.Remarks,
                        l.Quantity,
                        l.IssuedAtUtc)).ToList()));
        }
        return result;
    }

    public async Task<ServiceResult> SubmitStaffRequestAsync(ItemStaffRequestInput model)
    {
        var actor = await RequestScopeAsync(model.OrganisationId > 0 ? model.OrganisationId : null);
        var person = model.Kind == "Issue" && actor.Manage ? model.IndividualId : actor.Person;
        if (model.Kind is not ("Issue" or "Requisition") || model.Kind == "Issue" && !actor.Manage)
            return ServiceResult.Fail("Only an administrator can submit a direct issue request.");
        if (person <= 0 || model.Purpose is not ("Common" or "Job related") ||
            string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 1000 ||
            model.Lines is null || model.Lines.Count is < 1 or > 30 ||
            model.Lines.Any(l => string.IsNullOrWhiteSpace(l.ItemName) || l.ItemName.Trim().Length > 200 || l.Quantity is < 1 or > 1000))
            return ServiceResult.Fail("Enter 1–30 item lines, each with a name and positive quantity, plus a reason of up to 1,000 characters.");
        var lines = model.Lines.Select(l => new ItemStaffRequestLine
        {
            ItemId = l.ItemId,
            ItemUnitId = l.ItemUnitId,
            ItemName = l.ItemName.Trim(),
            InventoryNumber = string.IsNullOrWhiteSpace(l.InventoryNumber) ? null : l.InventoryNumber.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(l.SerialNumber) ? null : l.SerialNumber.Trim(),
            Condition = string.IsNullOrWhiteSpace(l.Condition) ? "Good" : l.Condition.Trim(),
            ExpectedReturnDate = l.ExpectedReturnDate,
            Remarks = string.IsNullOrWhiteSpace(l.Remarks) ? null : l.Remarks.Trim(),
            Quantity = l.Quantity
        }).ToList();
        var duplicates = lines.GroupBy(l => (l.ItemName.ToLowerInvariant(), (l.InventoryNumber ?? "").ToLowerInvariant(), (l.SerialNumber ?? "").ToLowerInvariant()));
        if (duplicates.Any(g => g.Count() > 1))
            return ServiceResult.Fail("Combine duplicate item lines into one line or specify distinct inventory/serial numbers.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var today = DateTime.UtcNow.AddHours(5).Date;
            var job = await db.Jobs.Where(j => j.OrganisationID == actor.Org && j.IndividualID == person && j.JobStateId == 4 &&
                j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate > today))
                .OrderByDescending(j => j.JoinedDate).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("The staff member needs active approved employment in this organisation.");
            var typeName = model.Kind == "Issue" ? Contracts.ItemCatalogueServiceContracts.IssueTypeName : Contracts.ItemCatalogueServiceContracts.RequisitionTypeName;
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == typeName)
                ?? throw new InvalidOperationException("Apply item request database setup and configure its approval flow.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,
                $"{model.Kind} {lines.Count} item lines for staff {person} in organisation {actor.Org}.");
            var request = await requests.CreateAsync(db, new CreateRequestDto { RequestTypeId = type.RequestTypeId,
                ApplicantBusinessEntityId = person, OrganisationBusinessEntityId = actor.Org,
                EffectiveDate = today, OperationLogId = audit.OperationLogId });
            db.ItemStaffRequests.Add(new ItemStaffRequest { Request = request, RequestId = request.RequestId,
                OrganisationId = actor.Org, IndividualId = person, JobId = job.JobId,
                Quantity = lines.Sum(l => l.Quantity), Purpose = model.Purpose, Kind = model.Kind,
                Reason = model.Reason.Trim(), CreatedAtUtc = DateTime.UtcNow, Lines = lines });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, job.JobId);
            await tx.CommitAsync();
            return ServiceResult.Ok($"{model.Kind} submitted for approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
    }

    public async Task<ServiceResult> ActStaffRequestAsync(int requestId, int expectedState, string action, string? comment)
    {
        var actor = await RequestScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var row = await db.ItemStaffRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
                .Include(r => r.Lines).SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationId == actor.Org)
                ?? throw new UnauthorizedAccessException("Item request unavailable in this organisation.");
            if (action is "Return" or "MarkReturned")
            {
                if (!actor.Manage) throw new UnauthorizedAccessException("Only an administrator can record an item return.");
                row.ReturnedAtUtc = DateTime.UtcNow;
                foreach (var line in row.Lines)
                {
                    if (line.ItemUnitId.HasValue)
                    {
                        var unit = await db.ItemUnits.SingleOrDefaultAsync(u => u.ItemUnitId == line.ItemUnitId.Value && u.OrganisationId == actor.Org);
                        if (unit != null)
                        {
                            unit.Status = "Returned";
                            unit.CollectedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
                            unit.IssuedToIndividualId = null;
                            unit.Version++;
                        }
                    }
                }
                if (row.ItemUnitId.HasValue)
                {
                    var unit = await db.ItemUnits.SingleOrDefaultAsync(u => u.ItemUnitId == row.ItemUnitId.Value && u.OrganisationId == actor.Org);
                    if (unit != null)
                    {
                        unit.Status = "Returned";
                        unit.CollectedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
                        unit.IssuedToIndividualId = null;
                        unit.Version++;
                    }
                }
                await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                    $"Recorded item return for staff {row.IndividualId}; request {row.RequestId}.");
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult.Ok("Items successfully marked as returned.");
            }
            if (action is "Approve" or "Reject" && await db.WfRequestActions.AnyAsync(a =>
                a.WorkflowRequestInstance.RequestId == requestId && a.ActionCode == "START" && a.PerformedByUserId == actor.User))
                throw new UnauthorizedAccessException("You cannot decide an item request that you submitted.");
            await ItemProcessing.ExecuteAsync(db, Subject(row), expectedState, action, comment, actor.User, actor.Person,
                () => ValidateIssueAsync(db, row), async () =>
                {
                    await ValidateIssueAsync(db, row);
                    // Preserve fulfilment for requests submitted before item lines replaced catalogue selection.
                    if (row.ItemId.HasValue)
                    {
                        var item = await db.Items.SingleAsync(i => i.ItemId == row.ItemId.Value && i.OrganisationId == actor.Org);
                        if (item.Kind == "Asset")
                        {
                            var unit = await db.ItemUnits.Where(u => u.ItemId == item.ItemId && u.Status == "Available")
                                .OrderBy(u => u.ItemUnitId).FirstAsync();
                            unit.Status = "Issued"; unit.IssuedToIndividualId = row.IndividualId;
                            unit.IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5)); unit.Version++;
                            row.ItemUnitId = unit.ItemUnitId;
                        }
                        else { item.QuantityOnHand -= row.Quantity; item.Version++; }
                    }
                    foreach (var line in row.Lines)
                    {
                        if (line.ItemUnitId.HasValue)
                        {
                            var unit = await db.ItemUnits.SingleOrDefaultAsync(u => u.ItemUnitId == line.ItemUnitId.Value && u.OrganisationId == actor.Org);
                            if (unit != null)
                            {
                                unit.Status = "Issued";
                                unit.IssuedToIndividualId = row.IndividualId;
                                unit.IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
                                unit.Version++;
                            }
                        }
                        else if (line.ItemId.HasValue)
                        {
                            var item = await db.Items.SingleOrDefaultAsync(i => i.ItemId == line.ItemId.Value && i.OrganisationId == actor.Org);
                            if (item != null)
                            {
                                if (item.Kind == "Asset")
                                {
                                    var unit = await db.ItemUnits.Where(u => u.ItemId == item.ItemId && u.Status == "Available")
                                        .OrderBy(u => u.ItemUnitId).FirstOrDefaultAsync();
                                    if (unit != null)
                                    {
                                        unit.Status = "Issued";
                                        unit.IssuedToIndividualId = row.IndividualId;
                                        unit.IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
                                        unit.Version++;
                                        line.ItemUnitId = unit.ItemUnitId;
                                        if (string.IsNullOrWhiteSpace(line.InventoryNumber)) line.InventoryNumber = unit.InventoryNumber;
                                        if (string.IsNullOrWhiteSpace(line.SerialNumber)) line.SerialNumber = unit.SerialNumber;
                                    }
                                }
                                else
                                {
                                    item.QuantityOnHand = Math.Max(0, item.QuantityOnHand - line.Quantity);
                                    item.Version++;
                                }
                            }
                        }
                    }
                    row.IssuedAtUtc = DateTime.UtcNow;
                    foreach (var line in row.Lines) line.IssuedAtUtc = row.IssuedAtUtc;
                    await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                        $"Approved and issued {row.Lines.Count} item lines to staff {row.IndividualId}; request {row.RequestId}.");
                });
            await tx.CommitAsync();
            return ServiceResult.Ok(row.IssuedAtUtc.HasValue ? "Approved and issued to staff." : "Item request action saved.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("Stock or the request changed. Refresh and try again."); }
    }

    private static async Task ValidateIssueAsync(HrmTeContext db, ItemStaffRequest row)
    {
        if (row.Lines.Count == 0) throw new InvalidOperationException("The request has no item lines.");
        var today = DateTime.UtcNow.AddHours(5).Date;
        if (!await db.Jobs.AnyAsync(j => j.JobId == row.JobId && j.OrganisationID == row.OrganisationId &&
            j.IndividualID == row.IndividualId && j.JobStateId == 4 && j.JoinedDate <= today &&
            (!j.TerminatedDate.HasValue || j.TerminatedDate > today)))
            throw new InvalidOperationException("The staff member no longer has active approved employment.");
        if (row.ItemId.HasValue)
        {
            var item = await db.Items.SingleOrDefaultAsync(i => i.ItemId == row.ItemId.Value && i.OrganisationId == row.OrganisationId);
            if (item is null || !item.IsActive) throw new InvalidOperationException("The legacy catalogue item is no longer active.");
            var available = item.Kind == "Asset" ? await db.ItemUnits.CountAsync(u => u.ItemId == item.ItemId && u.Status == "Available") : item.QuantityOnHand;
            if (available < row.Quantity) throw new InvalidOperationException("Legacy request stock is no longer available.");
        }
    }
}
