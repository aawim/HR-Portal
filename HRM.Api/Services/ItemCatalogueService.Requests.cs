using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Assets;
using HRM.DTOs.Items;
using HRM.Enums;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>
/// Staff item custody lifecycle. Deliberately independent of the configurable approval workflow:
/// HR (Admin permission) issues/approves and checks returns, the staff member accepts and asks for returns,
/// replacements or upgrades. Every transition is written to IM_ItemStaffRequestEvents.
/// </summary>
public sealed partial class ItemCatalogueService
{
    private const string JobRelated = "Job related";
    private static readonly string[] ReturnConditions = ["Good", "Fair", "Damaged", "Unusable", "Lost"];

    private static class St
    {
        public const string Submitted = "Submitted", PendingAcceptance = "PendingAcceptance", InUse = "InUse",
            Received = "Received", ReturnRequested = "ReturnRequested", Returned = "Returned",
            Rejected = "Rejected", Cancelled = "Cancelled", Declined = "Declined";
    }

    private async Task<(int Org, int Person, int User, bool Manage)> RequestScopeAsync(int? selected = null)
    {
        var actor = await access.RequireContextAsync();
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = actor.IsSuperAdministrator && selected is > 0 ? selected.Value : current;
        if (org <= 0 || !actor.IsSuperAdministrator && (selected.HasValue && selected != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Select your current organisation.");
        var manage = actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin || actor.IsSupervisor || await access.HasPermissionAsync("Admin");
        return (org, actor.IndividualId, actor.UserId, manage);
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
                l.Request.IndividualId == person)
            .Select(l => l.ItemName).ToListAsync()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Task<List<ItemStaffRequestDto>> GetStaffRequestsAsync(int? organisationId = null, int? individualId = null)
        => LoadStaffRequestsAsync(organisationId, individualId, false);

    public Task<List<ItemStaffRequestDto>> GetPendingItemApprovalsAsync()
        => LoadStaffRequestsAsync(null, null, true);

    private async Task<List<ItemStaffRequestDto>> LoadStaffRequestsAsync(int? organisationId, int? individualId, bool hrQueueOnly)
    {
        var actor = await RequestScopeAsync(organisationId);
        if (hrQueueOnly && !actor.Manage) return [];
        var person = individualId.GetValueOrDefault(hrQueueOnly || actor.Manage ? 0 : actor.Person);
        if (person > 0 && person != actor.Person && !actor.Manage)
            throw new UnauthorizedAccessException("You can view only your item records.");
        await using var db = await factory.CreateDbContextAsync();
        var rows = await db.ItemStaffRequests.AsNoTracking().Include(r => r.Request).Include(r => r.Lines)
            .Include(r => r.Events).Include(r => r.ReplacesRequest).ThenInclude(r => r!.Request)
            .Where(r => r.OrganisationId == actor.Org && (person == 0 || r.IndividualId == person) &&
                (hrQueueOnly ? r.Status == St.Submitted || r.Status == St.ReturnRequested : actor.Manage || r.IndividualId == actor.Person))
            .OrderByDescending(r => r.CreatedAtUtc).Take(300).ToListAsync();

        var people = rows.Select(r => r.IndividualId).Concat(rows.SelectMany(r => r.Events).Select(e => e.ActorIndividualId)).Distinct().ToList();
        var names = await db.Individuals.AsNoTracking().Where(i => people.Contains(i.BusinessEntityId))
            .Select(i => new { i.BusinessEntityId, Name = i.FirstNameEnglish + " " + (i.LastNameEnglish ?? "") })
            .ToDictionaryAsync(i => i.BusinessEntityId, i => i.Name.Trim());

        return rows.Select(row =>
        {
            var actions = AllowedActions(row, actor.Person, actor.Manage);
            return new ItemStaffRequestDto(row.RequestId, row.Request.RequestStateId, row.Request.ReferenceNumber ?? $"IM-{row.RequestId}",
                row.IndividualId, names.GetValueOrDefault(row.IndividualId, $"Staff {row.IndividualId}"), row.Purpose, row.Kind,
                row.Reason, row.Status, row.CreatedAtUtc, row.IssuedAtUtc, row.ReturnedAtUtc,
                actions.Contains("Approve"), actions.Contains("Cancel"),
                row.Lines.OrderBy(l => l.ItemStaffRequestLineId).Select(l => new ItemStaffRequestLineDto(l.ItemStaffRequestLineId,
                    l.ItemId, l.ItemUnitId, l.ItemName, l.InventoryNumber, l.SerialNumber, l.Condition, l.ExpectedReturnDate,
                    l.Remarks, l.Quantity, l.IssuedAtUtc)).ToList())
            {
                ReplacesRequestId = row.ReplacesRequestId,
                ReplacesReference = row.ReplacesRequest == null ? null : row.ReplacesRequest.Request.ReferenceNumber ?? $"IM-{row.ReplacesRequestId}",
                AcceptedAtUtc = row.AcceptedAtUtc,
                ReturnRequestedAtUtc = row.ReturnRequestedAtUtc,
                ReturnCondition = row.ReturnCondition,
                ReturnNotes = row.ReturnNotes,
                Actions = actions,
                History = row.Events.OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.ItemStaffRequestEventId)
                    .Select(e => new ItemStaffRequestEventDto(e.Action, e.FromStatus, e.ToStatus,
                        names.GetValueOrDefault(e.ActorIndividualId, e.ActorUserId == 0 ? "System" : $"User {e.ActorUserId}"),
                        e.Condition, e.Comment, e.CreatedAtUtc)).ToList()
            };
        }).ToList();
    }

    public async Task<ItemStaffClearanceReportDto> GetStaffClearanceReportAsync(int individualId, int? organisationId = null)
    {
        var actor = await RequestScopeAsync(organisationId);
        if (individualId != actor.Person && !actor.Manage)
            throw new UnauthorizedAccessException("You can only generate your own clearance report.");

        await using var db = await factory.CreateDbContextAsync();

        var individual = await db.Individuals.AsNoTracking().FirstOrDefaultAsync(i => i.BusinessEntityId == individualId)
            ?? throw new KeyNotFoundException($"Staff member #{individualId} not found.");

        var staffName = (individual.FirstNameEnglish + " " + (individual.LastNameEnglish ?? "")).Trim();

        var job = await db.Jobs.AsNoTracking()
            .Include(j => j.Staff)
            .Include(j => j.JobPositions).ThenInclude(p => p.Position)
            .Where(j => j.IndividualID == individualId && j.OrganisationID == actor.Org)
            .OrderByDescending(j => j.JoinedDate)
            .FirstOrDefaultAsync();

        var positionName = job?.JobPositions.OrderByDescending(p => p.FromDate).Select(p => p.Position.Name).FirstOrDefault() ?? "Staff Member";
        var staffNumber = job?.Staff?.EmployeeNumber ?? job?.Sapnumber ?? $"ID-{individualId}";
        var orgName = await db.Organisations.Where(o => o.BusinessEntityID == actor.Org).Select(o => o.OrganisationName).FirstOrDefaultAsync() ?? "Organisation";

        var allRequests = await LoadStaffRequestsAsync(actor.Org, individualId, false);

        var activeAssets = allRequests
            .Where(r => r.Purpose == JobRelated && r.Status is St.InUse or St.PendingAcceptance or St.ReturnRequested)
            .ToList();

        var returnedAssets = allRequests
            .Where(r => r.Purpose == JobRelated && r.Status == St.Returned)
            .ToList();

        var consumables = allRequests
            .Where(r => r.Purpose != JobRelated)
            .ToList();

        var fullHistory = allRequests
            .SelectMany(r => r.History)
            .OrderByDescending(h => h.CreatedAtUtc)
            .ToList();

        return new ItemStaffClearanceReportDto(
            individualId,
            staffName,
            staffNumber,
            positionName,
            "General",
            orgName,
            DateTime.UtcNow,
            activeAssets,
            returnedAssets,
            consumables,
            fullHistory
        );
    }

    /// <summary>Pure state machine: which actions the actor may perform on a record.</summary>
    internal static List<string> AllowedActions(ItemStaffRequest r, int person, bool manage)
    {
        var owner = r.IndividualId == person;
        var hr = manage && !owner; // HR never acts on their own record.
        var list = new List<string>();
        switch (r.Status)
        {
            case St.Submitted:
                if (hr) list.AddRange(["Approve", "Reject"]);
                if (owner) list.Add("Cancel");
                break;
            case St.PendingAcceptance:
                if (owner) list.AddRange(["Accept", "Decline"]);
                break;
            case St.InUse:
                if (owner)
                {
                    list.Add("RequestReturn");
                    list.AddRange(["RequestReplacement", "RequestUpgrade"]);
                }
                if (hr)
                {
                    list.Add("RequestReturn");
                    list.Add("ConfirmReturn");
                }
                break;
            case St.ReturnRequested:
                if (hr) list.Add("ConfirmReturn");
                break;
        }
        return list;
    }

    public async Task<ServiceResult> SubmitStaffRequestAsync(ItemStaffRequestInput model)
    {
        var actor = await RequestScopeAsync(model.OrganisationId > 0 ? model.OrganisationId : null);
        if (model.Kind is not ("Issue" or "Requisition" or "Replacement" or "Upgrade"))
            return ServiceResult.Fail("Unknown item request type.");

        int person;
        if (model.Kind == "Issue")
        {
            // HR assigns items to another staff member; the staff member then accepts them.
            if (!actor.Manage) return ServiceResult.Fail("Only HR can assign items to staff.");
            if (model.IndividualId <= 0)
                return ServiceResult.Fail("Select a staff member to assign items to.");
            if (model.IndividualId == actor.Person)
                return ServiceResult.Fail("Staff raise item requests for themselves. HR assigns job-related items from the staff workspace.");
            person = model.IndividualId;
            if (string.IsNullOrWhiteSpace(model.Purpose))
                model.Purpose = JobRelated;
        }
        else
        {
            // Requisitions, replacements and upgrades can be raised by staff for themselves,
            // or by HR on behalf of a staff member.
            if (model.IndividualId > 0 && model.IndividualId != actor.Person)
            {
                if (!actor.Manage)
                    return ServiceResult.Fail("Staff raise item requests for themselves. HR assigns job-related items from the staff workspace.");
                person = model.IndividualId;
            }
            else
            {
                person = actor.Person;
            }
            if (model.Kind is "Replacement" or "Upgrade") model.Purpose = JobRelated;
        }

        if (person <= 0 || model.Purpose is not ("Common" or JobRelated) ||
            string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 1000 ||
            model.Lines is null || model.Lines.Count is < 1 or > 30 ||
            model.Lines.Any(l => string.IsNullOrWhiteSpace(l.ItemName) || l.ItemName.Trim().Length > 200 || l.Quantity is < 1 or > 1000))
            return ServiceResult.Fail("Enter 1–30 item lines, each with a name and positive quantity, plus a reason of up to 1,000 characters.");
        var lines = model.Lines.Select(l => new ItemStaffRequestLine
        {
            ItemId = l.ItemId,
            ItemUnitId = model.Kind == "Issue" ? l.ItemUnitId : null,
            ItemName = l.ItemName.Trim(),
            InventoryNumber = string.IsNullOrWhiteSpace(l.InventoryNumber) ? null : l.InventoryNumber.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(l.SerialNumber) ? null : l.SerialNumber.Trim(),
            Condition = string.IsNullOrWhiteSpace(l.Condition) ? "Good" : l.Condition.Trim(),
            ExpectedReturnDate = l.ExpectedReturnDate,
            Remarks = string.IsNullOrWhiteSpace(l.Remarks) ? null : l.Remarks.Trim(),
            Quantity = l.Quantity
        }).ToList();
        if (lines.GroupBy(l => (l.ItemName.ToLowerInvariant(), (l.InventoryNumber ?? "").ToLowerInvariant(), (l.SerialNumber ?? "").ToLowerInvariant())).Any(g => g.Count() > 1))
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

            if (model.Kind is "Replacement" or "Upgrade")
            {
                var original = await db.ItemStaffRequests.SingleOrDefaultAsync(r => r.RequestId == model.ReplacesRequestId &&
                    r.OrganisationId == actor.Org && r.IndividualId == person)
                    ?? throw new InvalidOperationException("Select the job-related item you want to replace or upgrade.");
                if (original.Status != St.InUse || original.Purpose != JobRelated)
                    throw new InvalidOperationException("Only accepted job-related items in use can be replaced or upgraded.");
                if (await db.ItemStaffRequests.AnyAsync(r => r.ReplacesRequestId == original.RequestId &&
                    (r.Status == St.Submitted || r.Status == St.PendingAcceptance)))
                    throw new InvalidOperationException("A replacement or upgrade for this item is already in progress.");
            }
            else model.ReplacesRequestId = null;

            var typeName = model.Kind == "Issue" ? Contracts.ItemCatalogueServiceContracts.IssueTypeName : Contracts.ItemCatalogueServiceContracts.RequisitionTypeName;
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == typeName)
                ?? throw new InvalidOperationException("Apply the item request database setup first.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,
                $"{model.Kind} {lines.Count} item lines for staff {person} in organisation {actor.Org}.");
            var request = await requests.CreateAsync(db, new CreateRequestDto { RequestTypeId = type.RequestTypeId,
                ApplicantBusinessEntityId = person, OrganisationBusinessEntityId = actor.Org,
                EffectiveDate = today, OperationLogId = audit.OperationLogId });
            var row = new ItemStaffRequest { Request = request, RequestId = request.RequestId,
                OrganisationId = actor.Org, IndividualId = person, JobId = job.JobId,
                Quantity = lines.Sum(l => l.Quantity), Purpose = model.Purpose, Kind = model.Kind,
                ReplacesRequestId = model.ReplacesRequestId,
                Reason = model.Reason.Trim(), CreatedAtUtc = DateTime.UtcNow, Lines = lines, Status = St.Submitted };
            db.ItemStaffRequests.Add(row);
            AddEvent(row, model.Kind == "Issue" ? "Assigned" : "Submitted", null, St.Submitted, actor, null, model.Reason.Trim());
            if (model.Kind == "Issue")
            {
                // HR assignment is pre-approved: stock is reserved now and the staff member must accept it.
                await IssueStockAsync(db, row);
                Move(row, "Issued", St.PendingAcceptance, actor, null, null);
                await SyncJobRelatedItemToAssetSystemAsync(db, row, actor.User);
                await NotifyStaffAsync(db, row, $"New equipment/items ({string.Join(", ", lines.Select(l => $"{l.Quantity}x {l.ItemName}"))}) were assigned to you. Please confirm receipt.", actor.User);
            }
            else
            {
                // Requisitions, replacements and upgrades submitted to HR.
                // Strict rule: No notification to sender!
                await NotifyApproversAsync(db, row, $"{model.Kind} request ({string.Join(", ", lines.Select(l => $"{l.Quantity}x {l.ItemName}"))}) submitted by staff #{person}.", actor.User, actor.Person);
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok(model.Kind == "Issue"
                ? "Items assigned. The staff member must accept them."
                : $"{model.Kind} submitted to HR.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            var inner = ex.InnerException?.Message ?? ex.Message;
            return ServiceResult.Fail($"Database update failed: {inner}");
        }
    }

    /// <param name="expectedState">Unused; kept for contract compatibility. Concurrency is guarded by the status check.</param>
    /// <param name="action">An action code from <see cref="ItemStaffRequestDto.Actions"/>. ConfirmReturn carries the condition: "ConfirmReturn:Damaged".</param>
    public async Task<ServiceResult> ActStaffRequestAsync(int requestId, int expectedState, string action, string? comment)
    {
        var actor = await RequestScopeAsync();
        var parts = (action ?? "").Split(':', 2, StringSplitOptions.TrimEntries);
        var code = parts[0] == "MarkReturned" || parts[0] == "Return" ? "ConfirmReturn" : parts[0];
        var condition = parts.Length > 1 ? parts[1] : null;
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment?.Length > 1000) return ServiceResult.Fail("Comments are limited to 1,000 characters.");

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var row = await db.ItemStaffRequests.Include(r => r.Lines).Include(r => r.Request)
                .SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationId == actor.Org)
                ?? throw new UnauthorizedAccessException("Item record unavailable in this organisation.");
            if (!AllowedActions(row, actor.Person, actor.Manage).Contains(code))
                throw new InvalidOperationException($"'{code}' is not allowed while the record is {row.Status}. Refresh and try again.");

            string message;
            switch (code)
            {
                case "Approve":
                    await ValidateEmploymentAsync(db, row);
                    await IssueStockAsync(db, row);
                    Move(row, "Approved", St.PendingAcceptance, actor, null, comment);
                    message = "Approved and issued. Waiting for the staff member to accept.";
                    await NotifyStaffAsync(db, row, $"Your item request #{row.RequestId} has been approved and issued. Please confirm receipt.", actor.User);
                    break;
                case "Reject":
                    if (comment is null) throw new InvalidOperationException("Enter a reason for rejecting.");
                    Move(row, "Rejected", St.Rejected, actor, null, comment);
                    message = "Request rejected.";
                    await NotifyStaffAsync(db, row, $"Your item request #{row.RequestId} was rejected. Reason: {comment}", actor.User);
                    break;
                case "Cancel":
                    Move(row, "Cancelled", St.Cancelled, actor, null, comment);
                    message = "Request cancelled.";
                    await NotifyApproversAsync(db, row, $"Item request #{row.RequestId} was cancelled by the applicant.", actor.User, actor.Person);
                    break;
                case "Accept":
                    row.AcceptedAtUtc = DateTime.UtcNow;
                    Move(row, "Accepted", row.Purpose == JobRelated ? St.InUse : St.Received, actor, null, comment);
                    if (row.ReplacesRequestId.HasValue)
                    {
                        // The replaced item must now be handed back and checked by HR.
                        var old = await db.ItemStaffRequests.SingleAsync(r => r.RequestId == row.ReplacesRequestId.Value);
                        if (old.Status == St.InUse)
                        {
                            old.ReturnRequestedAtUtc = DateTime.UtcNow;
                            Move(old, "ReturnRequested", St.ReturnRequested, actor, null,
                                $"{row.Kind} accepted under {row.RequestId}; old item to be returned.");
                        }
                    }
                    message = "Items accepted.";
                    await NotifyApproversAsync(db, row, $"Staff #{row.IndividualId} accepted items under request #{row.RequestId}.", actor.User, actor.Person);
                    break;
                case "Decline":
                    if (comment is null) throw new InvalidOperationException("Enter a reason for declining.");
                    await RestoreStockAsync(db, row, "Good");
                    Move(row, "Declined", St.Declined, actor, null, comment);
                    message = "Items declined and returned to stock.";
                    await NotifyApproversAsync(db, row, $"Staff #{row.IndividualId} declined items under request #{row.RequestId}. Reason: {comment}", actor.User, actor.Person);
                    break;
                case "RequestReturn":
                    row.ReturnRequestedAtUtc = DateTime.UtcNow;
                    Move(row, "ReturnRequested", St.ReturnRequested, actor, null, comment);
                    message = "Return submitted. HR will check the items.";
                    await NotifyApproversAsync(db, row, $"Staff #{row.IndividualId} requested return for items under request #{row.RequestId}.", actor.User, actor.Person);
                    break;
                case "ConfirmReturn":
                    if (condition is null || !ReturnConditions.Contains(condition))
                        throw new InvalidOperationException("Select the condition of the returned items.");
                    if (condition != "Good" && comment is null)
                        throw new InvalidOperationException("Describe the damage or loss for the record.");
                    await RestoreStockAsync(db, row, condition);
                    row.ReturnedAtUtc = DateTime.UtcNow;
                    row.ReturnCondition = condition;
                    row.ReturnNotes = comment;
                    Move(row, "ReturnConfirmed", St.Returned, actor, condition, comment);
                    message = condition == "Good" ? "Return confirmed." : $"Return confirmed and recorded as {condition}.";
                    await NotifyStaffAsync(db, row, $"The return of items for request #{row.RequestId} was confirmed by HR ({condition}).", actor.User);
                    break;
                default:
                    throw new InvalidOperationException("Use the request form to ask for a replacement or upgrade.");
            }
            await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                $"Item record {row.RequestId} for staff {row.IndividualId}: {code}{(condition is null ? "" : $" ({condition})")}.");
            await SyncJobRelatedItemToAssetSystemAsync(db, row, actor.User);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok(message);
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            var inner = ex.InnerException?.Message ?? ex.Message;
            return ServiceResult.Fail($"Database update failed: {inner}");
        }
    }

    private static void AddEvent(ItemStaffRequest row, string action, string? from, string to,
        (int Org, int Person, int User, bool Manage) actor, string? condition, string? comment)
        => row.Events.Add(new ItemStaffRequestEvent { Action = action, FromStatus = from, ToStatus = to,
            ActorUserId = actor.User, ActorIndividualId = actor.Person, Condition = condition, Comment = comment,
            CreatedAtUtc = DateTime.UtcNow });

    private static void Move(ItemStaffRequest row, string action, string to,
        (int Org, int Person, int User, bool Manage) actor, string? condition, string? comment)
    {
        AddEvent(row, action, row.Status, to, actor, condition, comment);
        row.Status = to;
    }

    private static async Task ValidateEmploymentAsync(HrmTeContext db, ItemStaffRequest row)
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        if (!await db.Jobs.AnyAsync(j => j.JobId == row.JobId && j.OrganisationID == row.OrganisationId &&
            j.IndividualID == row.IndividualId && j.JobStateId == 4 && j.JoinedDate <= today &&
            (!j.TerminatedDate.HasValue || j.TerminatedDate > today)))
            throw new InvalidOperationException("The staff member no longer has active approved employment.");
    }

    /// <summary>Takes catalogue stock / asset units for each line. Free-text lines are recorded without stock.</summary>
    private static async Task IssueStockAsync(HrmTeContext db, ItemStaffRequest row)
    {
        if (row.Lines.Count == 0) throw new InvalidOperationException("The request has no item lines.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        foreach (var line in row.Lines)
        {
            if (line.ItemUnitId.HasValue)
            {
                var unit = await db.ItemUnits.SingleOrDefaultAsync(u => u.ItemUnitId == line.ItemUnitId.Value && u.OrganisationId == row.OrganisationId)
                    ?? throw new InvalidOperationException($"Asset unit for {line.ItemName} was not found.");
                if (unit.Status != "Available") throw new InvalidOperationException($"{line.ItemName} #{unit.InventoryNumber} is no longer available.");
                Take(unit, line, row.IndividualId, today);
            }
            else if (line.ItemId.HasValue)
            {
                var item = await db.Items.SingleOrDefaultAsync(i => i.ItemId == line.ItemId.Value && i.OrganisationId == row.OrganisationId)
                    ?? throw new InvalidOperationException($"Catalogue item {line.ItemName} was not found.");
                if (item.Kind == "Asset")
                {
                    // Assets are tracked per unit so each one can be accepted, returned and checked individually.
                    if (line.Quantity != 1) throw new InvalidOperationException($"Add {item.Name} as one line per unit.");
                    var unit = await db.ItemUnits.Where(u => u.ItemId == item.ItemId && u.Status == "Available")
                        .OrderBy(u => u.ItemUnitId).FirstOrDefaultAsync()
                        ?? throw new InvalidOperationException($"No available units of {item.Name}.");
                    Take(unit, line, row.IndividualId, today);
                }
                else
                {
                    if (item.QuantityOnHand < line.Quantity) throw new InvalidOperationException($"Only {item.QuantityOnHand} {item.Name} in stock.");
                    item.QuantityOnHand -= line.Quantity;
                    item.Version++;
                }
            }
            line.IssuedAtUtc = DateTime.UtcNow;
        }
        row.IssuedAtUtc = DateTime.UtcNow;
    }

    private static void Take(ItemUnit unit, ItemStaffRequestLine? line, int person, DateOnly today)
    {
        unit.Status = "Issued";
        unit.IssuedToIndividualId = person;
        unit.IssuedDate = today;
        unit.CollectedDate = null;
        unit.Version++;
        if (line is null) return;
        line.ItemUnitId = unit.ItemUnitId;
        if (string.IsNullOrWhiteSpace(line.InventoryNumber)) line.InventoryNumber = unit.InventoryNumber;
        if (string.IsNullOrWhiteSpace(line.SerialNumber)) line.SerialNumber = unit.SerialNumber;
    }

    /// <summary>Puts asset units back (or marks them damaged/lost); consumables return to stock only when in good order.</summary>
    private static async Task RestoreStockAsync(HrmTeContext db, ItemStaffRequest row, string condition)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        var unitIds = row.Lines.Where(l => l.ItemUnitId.HasValue).Select(l => l.ItemUnitId!.Value).ToList();
        if (row.ItemUnitId.HasValue) unitIds.Add(row.ItemUnitId.Value);
        var units = await db.ItemUnits.Where(u => u.OrganisationId == row.OrganisationId && u.IssuedToIndividualId == row.IndividualId &&
            u.Status == "Issued" && unitIds.Contains(u.ItemUnitId)).ToListAsync();
        foreach (var unit in units)
        {
            (unit.Status, unit.Condition) = condition switch
            {
                "Damaged" => ("Damaged", "Damaged"),
                "Unusable" => ("Damaged", "Unusable"),
                "Lost" => ("Lost", unit.Condition),
                _ => ("Available", condition)
            };
            unit.IssuedToIndividualId = null;
            unit.CollectedDate = today;
            unit.Version++;
        }
        if (condition is not ("Good" or "Fair")) return;
        foreach (var line in row.Lines.Where(l => l.ItemId.HasValue && !l.ItemUnitId.HasValue && l.IssuedAtUtc.HasValue))
        {
            var item = await db.Items.SingleOrDefaultAsync(i => i.ItemId == line.ItemId!.Value && i.OrganisationId == row.OrganisationId);
            if (item is { Kind: "Consumable" }) { item.QuantityOnHand += line.Quantity; item.Version++; }
        }
    }

    /// <summary>
    /// Synchronizes Job-Related items from IM_ItemStaffRequests into the AM_ serialized asset management tables
    /// (AM_AssetItems, AM_AssetIssuances, AM_AssetIssuanceItems, AM_AssetCustodies, AM_AssetDocuments, AM_AssetReturns).
    /// This ensures seamless interoperability with the /online/my-assets portal.
    /// </summary>
    private static async Task SyncJobRelatedItemToAssetSystemAsync(HrmTeContext db, ItemStaffRequest row, int userId)
    {
        if (row.Purpose != JobRelated) return;

        var nowUtc = DateTime.UtcNow;
        var todayDate = nowUtc.AddHours(5).Date;

        // Find or create AM_AssetIssuance
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.RequestId == row.RequestId);

        if (issuance == null)
        {
            var ind = await db.Individuals.AsNoTracking().FirstOrDefaultAsync(i => i.BusinessEntityId == row.IndividualId);
            var activeJob = await db.Jobs.AsNoTracking()
                .Include(j => j.Organisation)
                .Include(j => j.OrganisationStructure)
                .Include(j => j.JobPositions.Where(p => p.JobPositionStateId == 4 && (p.ToDate == null || p.ToDate >= todayDate)))
                    .ThenInclude(p => p.Position)
                .FirstOrDefaultAsync(j => j.JobId == row.JobId);

            var fullName = ind != null ? string.Join(" ", new[] { ind.FirstNameEnglish, ind.MiddleNameEnglish, ind.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))) : "Staff";
            var staffNumber = activeJob?.Sapnumber ?? activeJob?.JobId.ToString() ?? row.IndividualId.ToString();
            var positionTitle = activeJob?.JobPositions.FirstOrDefault()?.Position.Name ?? "Staff";
            var deptName = activeJob?.OrganisationStructure?.Name ?? "";
            var orgName = activeJob?.Organisation.OrganisationName ?? "Organisation";

            issuance = new AssetIssuance
            {
                RequestId = row.RequestId,
                ReferenceNumber = $"ISS-JOB-{row.RequestId}",
                IndividualId = row.IndividualId,
                JobId = row.JobId,
                OrganisationId = row.OrganisationId,
                StructureId = activeJob?.OrganisationStructureId,
                Reason = string.IsNullOrWhiteSpace(row.Reason) ? "Job related" : row.Reason.Trim(),
                Status = AssetIssuanceStatuses.Draft,
                CreatedByUserId = userId > 0 ? userId : 1,
                IssueDate = todayDate,
                EmployeeNameSnapshot = fullName,
                StaffNumberSnapshot = staffNumber,
                PositionTitleSnapshot = positionTitle,
                DepartmentNameSnapshot = deptName,
                OrganisationNameSnapshot = orgName,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };
            db.AssetIssuances.Add(issuance);
            await db.SaveChangesAsync();
        }

        // Default category & condition
        var laptopCategory = await db.AssetCategories.FirstOrDefaultAsync(c => c.Code == "LAPTOP");
        var defaultCategoryId = laptopCategory?.AssetCategoryId ?? (await db.AssetCategories.Select(c => c.AssetCategoryId).FirstOrDefaultAsync());
        if (defaultCategoryId <= 0) defaultCategoryId = 1;

        var goodCondition = await db.AssetConditions.FirstOrDefaultAsync(c => c.Code == "GOOD");
        var newCondition = await db.AssetConditions.FirstOrDefaultAsync(c => c.Code == "NEW");
        var defaultConditionId = goodCondition?.AssetConditionId ?? 2;

        foreach (var line in row.Lines)
        {
            var invCode = !string.IsNullOrWhiteSpace(line.InventoryNumber) ? line.InventoryNumber.Trim() : $"JOB-{row.RequestId}-L{line.ItemStaffRequestLineId}";
            var serial = !string.IsNullOrWhiteSpace(line.SerialNumber) ? line.SerialNumber.Trim() : null;

            var asset = await db.AssetItems.FirstOrDefaultAsync(a => a.AssetCode == invCode && a.OwnerOrganisationId == row.OrganisationId);
            if (asset == null)
            {
                var condId = (line.Condition != null && line.Condition.Contains("New", StringComparison.OrdinalIgnoreCase))
                    ? (newCondition?.AssetConditionId ?? 1)
                    : defaultConditionId;

                asset = new AssetItem
                {
                    CategoryId = defaultCategoryId,
                    AssetCode = invCode,
                    SerialNumber = serial,
                    Name = line.ItemName.Trim(),
                    Brand = "Standard",
                    Model = "Equipment",
                    OwnerOrganisationId = row.OrganisationId,
                    OwnerStructureId = issuance.StructureId,
                    Status = AssetInventoryStatuses.Available,
                    CurrentConditionId = condId,
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };
                db.AssetItems.Add(asset);
                await db.SaveChangesAsync();
            }

            var issuanceItem = issuance.Lines.FirstOrDefault(l => l.AssetItemId == asset.AssetItemId);
            if (issuanceItem == null)
            {
                issuanceItem = new AssetIssuanceItem
                {
                    IssuanceId = issuance.AssetIssuanceId,
                    AssetItemId = asset.AssetItemId,
                    ConditionAtIssueId = asset.CurrentConditionId,
                    HandedOverAtUtc = nowUtc,
                    HandedOverByUserId = userId > 0 ? userId : 1,
                    Status = AssetIssuanceLineStatuses.Proposed,
                    IsActiveReservation = true,
                    Version = 1,
                    ExpectedReturnDate = line.ExpectedReturnDate.HasValue ? line.ExpectedReturnDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    AssetCodeSnapshot = asset.AssetCode,
                    SerialNumberSnapshot = asset.SerialNumber,
                    CategoryNameSnapshot = laptopCategory?.Name ?? "General Assets",
                    ItemNameSnapshot = asset.Name,
                    BrandSnapshot = asset.Brand,
                    ModelSnapshot = asset.Model,
                    ConditionNameSnapshot = "Good",
                    IsSecurityItemSnapshot = false
                };
                issuance.Lines.Add(issuanceItem);
                db.AssetIssuanceItems.Add(issuanceItem);
                await db.SaveChangesAsync();
            }

            // Sync state based on row.Status
            if (row.Status == St.PendingAcceptance)
            {
                issuance.Status = AssetIssuanceStatuses.PendingStaffAcknowledgement;
                issuanceItem.Status = AssetIssuanceLineStatuses.HandedOver;
                asset.Status = AssetInventoryStatuses.PendingHandover;
            }
            else if (row.Status == St.InUse)
            {
                issuance.Status = AssetIssuanceStatuses.Completed;
                issuanceItem.Status = AssetIssuanceLineStatuses.HRApproved;
                issuanceItem.IsActiveReservation = false;
                asset.Status = AssetInventoryStatuses.Assigned;

                // Ensure active custody exists
                var custody = await db.AssetCustodies.FirstOrDefaultAsync(c => c.IssuanceItemId == issuanceItem.AssetIssuanceItemId);
                if (custody == null)
                {
                    custody = new AssetCustody
                    {
                        AssetItemId = asset.AssetItemId,
                        IndividualId = row.IndividualId,
                        JobId = row.JobId,
                        OrganisationId = row.OrganisationId,
                        StructureId = issuance.StructureId,
                        IssuanceItemId = issuanceItem.AssetIssuanceItemId,
                        CustodyFromUtc = row.AcceptedAtUtc ?? nowUtc,
                        CustodyToUtc = null,
                        ExpectedReturnDate = line.ExpectedReturnDate.HasValue ? line.ExpectedReturnDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                        Status = AssetCustodyStatuses.Active,
                        IdempotencyKey = $"CUST-SYNC-{row.RequestId}-{issuanceItem.AssetIssuanceItemId}"
                    };
                    db.AssetCustodies.Add(custody);
                }
                else if (custody.Status != AssetCustodyStatuses.Active)
                {
                    custody.Status = AssetCustodyStatuses.Active;
                    custody.CustodyToUtc = null;
                }

                // Ensure handover document exists
                var hasDoc = await db.AssetDocuments.AnyAsync(d => d.AssetIssuanceId == issuance.AssetIssuanceId && d.DocumentVariant == AssetDocumentVariants.StandardHandoverRecord);
                if (!hasDoc)
                {
                    var doc = new AssetDocument
                    {
                        AssetIssuanceId = issuance.AssetIssuanceId,
                        DocumentNumber = $"DOC-{issuance.ReferenceNumber}-V1",
                        DocumentVariant = AssetDocumentVariants.StandardHandoverRecord,
                        DocumentVersion = 1,
                        SnapshotJson = "{}",
                        Sha256Hash = "SYNCED",
                        ContentType = "application/pdf",
                        GeneratedByUserId = userId > 0 ? userId : 1,
                        GeneratedAtUtc = nowUtc
                    };
                    db.AssetDocuments.Add(doc);
                }
            }
            else if (row.Status == St.ReturnRequested)
            {
                var custody = await db.AssetCustodies.FirstOrDefaultAsync(c => c.IssuanceItemId == issuanceItem.AssetIssuanceItemId && c.Status == AssetCustodyStatuses.Active);
                if (custody != null)
                {
                    custody.Status = AssetCustodyStatuses.ReturnPending;
                    asset.Status = AssetInventoryStatuses.ReturnPending;

                    var ret = await db.AssetReturns.FirstOrDefaultAsync(r => r.ReferenceNumber == $"RET-{row.RequestId}");
                    if (ret == null)
                    {
                        ret = new AssetReturn
                        {
                            ReferenceNumber = $"RET-{row.RequestId}",
                            OrganisationId = row.OrganisationId,
                            IndividualId = row.IndividualId,
                            JobId = row.JobId,
                            ReturnType = AssetReturnTypes.StandardReturn,
                            RequestedByUserId = userId > 0 ? userId : 1,
                            ReturnDate = nowUtc,
                            Status = AssetReturnStatuses.ReturnPending,
                            Notes = row.ReturnNotes
                        };
                        db.AssetReturns.Add(ret);
                        await db.SaveChangesAsync();

                        var retItem = new AssetReturnItem
                        {
                            AssetReturnId = ret.AssetReturnId,
                            AssetCustodyId = custody.AssetCustodyId,
                            AssetItemId = asset.AssetItemId,
                            Status = AssetReturnStatuses.ReturnPending,
                            InspectionResult = AssetInspectionResults.Pending
                        };
                        db.AssetReturnItems.Add(retItem);
                    }
                }
            }
            else if (row.Status == St.Returned)
            {
                var custody = await db.AssetCustodies.FirstOrDefaultAsync(c => c.IssuanceItemId == issuanceItem.AssetIssuanceItemId);
                if (custody != null)
                {
                    custody.Status = AssetCustodyStatuses.Returned;
                    custody.CustodyToUtc = row.ReturnedAtUtc ?? nowUtc;
                    custody.ClosedReason = $"Returned via Request #{row.RequestId} (Condition: {row.ReturnCondition ?? "Good"})";
                    custody.ClosedByUserId = userId > 0 ? userId : 1;
                }
                asset.Status = AssetInventoryStatuses.Available;
            }
            else if (row.Status is St.Declined or St.Cancelled or St.Rejected)
            {
                issuance.Status = AssetIssuanceStatuses.Cancelled;
                issuanceItem.Status = AssetIssuanceLineStatuses.Closed;
                issuanceItem.IsActiveReservation = false;
                asset.Status = AssetInventoryStatuses.Available;
            }

            asset.UpdatedAtUtc = nowUtc;
        }

        issuance.UpdatedAtUtc = nowUtc;
    }


    private static async Task NotifyApproversAsync(HrmTeContext db, ItemStaffRequest row, string message, int senderUserId, int senderPersonId)
    {
        var approverUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.LocalUserStateId == 1 &&
                        u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == row.OrganisationId &&
                        u.UserId != senderUserId && u.BusinessEntityID != senderPersonId &&
                        (u.AdminPortalAccessEnabled ||
                         u.UserRoles.Any(r => r.IsActive && (r.Role.Name.Contains("Admin") || r.Role.Name.Contains("HR"))) ||
                         u.UserAssignedUserGroups.Any(g => g.UserGroup.GroupName.Contains("Admin") || g.UserGroup.GroupName.Contains("HR"))))
            .Select(u => new { u.UserId, PersonId = u.BusinessEntityID })
            .Distinct()
            .ToListAsync();

        if (approverUsers.Count == 0)
        {
            approverUsers = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.LocalUserStateId == 1 &&
                            u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == row.OrganisationId &&
                            u.UserId != senderUserId && u.BusinessEntityID != senderPersonId &&
                            u.AdminPortalAccessEnabled)
                .Select(u => new { u.UserId, PersonId = u.BusinessEntityID })
                .Distinct()
                .ToListAsync();
        }

        var refNumber = row.Request?.ReferenceNumber ?? $"IM-{row.RequestId}";
        var truncatedMessage = message.Length > 500 ? message[..500] : message;

        foreach (var approver in approverUsers)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = approver.UserId,
                IndividualId = approver.PersonId,
                OrganisationId = row.OrganisationId,
                Kind = "ItemRequest",
                Message = truncatedMessage,
                Url = $"/admin/my/workspace?tab=requests&requestId={row.RequestId}",
                CreatedAtUtc = DateTime.UtcNow
            });

            db.RequestNotifications.Add(new RequestNotification
            {
                UserId = approver.UserId,
                RequestId = row.RequestId,
                NotificationKind = "ActionRequired",
                FromStateId = 1,
                ToStateId = 1,
                ReferenceNumber = refNumber,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static async Task NotifyStaffAsync(HrmTeContext db, ItemStaffRequest row, string message, int senderUserId)
    {
        var staffUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.BusinessEntityID == row.IndividualId &&
                        u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == row.OrganisationId &&
                        u.UserId != senderUserId)
            .Select(u => u.UserId)
            .ToListAsync();

        var refNumber = row.Request?.ReferenceNumber ?? $"IM-{row.RequestId}";
        var truncatedMessage = message.Length > 500 ? message[..500] : message;

        foreach (var userId in staffUsers)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = userId,
                IndividualId = row.IndividualId,
                OrganisationId = row.OrganisationId,
                Kind = "ItemRequest",
                Message = truncatedMessage,
                Url = $"/online/item-requests?requestId={row.RequestId}",
                CreatedAtUtc = DateTime.UtcNow
            });

            db.RequestNotifications.Add(new RequestNotification
            {
                UserId = userId,
                RequestId = row.RequestId,
                NotificationKind = "StateChanged",
                FromStateId = 1,
                ToStateId = 1,
                ReferenceNumber = refNumber,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }
}