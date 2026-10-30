using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.LeaveType;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public partial class LeaveTypeCatalogueService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService? logs = null, IBusinessAccessService? business = null,
    Microsoft.AspNetCore.DataProtection.IDataProtectionProvider? protection = null) : ILeaveTypeCatalogueService
{
    private async Task<(int Org, bool Super)> Scope(bool write = false)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
        if (write && !actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Leave.Configuration.Manage"))
            throw new UnauthorizedAccessException("Leave configuration management permission is required.");
        return (org, actor.IsSuperAdministrator);
    }
    private static System.Linq.Expressions.Expression<Func<LeaveType, LeaveTypeCatalogueDto>> Projection => x => new()
    {
        LeaveTypeId = x.LeaveTypeId, RequestTypeId = x.RequestTypeId, Code = x.Code, Name = x.Name, NameDhivehi = x.NameDhivehi,
        Description = x.Description, OrganisationId = x.OrganisationId, IsGlobal = x.IsGlobal,
        IsSystemType = x.IsSystemType, IsActive = x.IsActive
    };
    public async Task<List<LeaveRequestTypeOption>> GetRequestTypesAsync(CancellationToken cancellationToken = default)
    {
        await Scope(); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.RequestTypes.AsNoTracking().Where(x => x.RequestTypeId > 0 && !HRM.Services.Leave.LeaveRequestTypeLink.ExcludedIds.Contains(x.RequestTypeId))
            .OrderBy(x => x.TypeName).Select(x => new LeaveRequestTypeOption(x.RequestTypeId, x.TypeName)).ToListAsync(cancellationToken);
    }
    public async Task<List<LeaveTypeCatalogueDto>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        var a = await Scope(); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.LeaveTypes.AsNoTracking().Where(x => x.IsActive && (x.IsGlobal || x.OrganisationId == a.Org))
            .OrderBy(x => x.Name).Select(Projection).ToListAsync(cancellationToken);
    }
    public async Task<List<LeaveTypeCatalogueDto>> GetManagementAsync(CancellationToken cancellationToken = default)
    {
        var a = await Scope(); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.LeaveTypes.AsNoTracking().Where(x => x.IsGlobal || x.OrganisationId == a.Org)
            .OrderBy(x => x.Name).Select(Projection).ToListAsync(cancellationToken);
    }
    public async Task<LeaveTypeCatalogueDto?> GetByIdAsync(int leaveDefinitionId, CancellationToken cancellationToken = default)
    {
        var a = await Scope(); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.LeaveTypes.AsNoTracking().Where(x => x.LeaveTypeId == leaveDefinitionId && (x.IsGlobal || x.OrganisationId == a.Org))
            .Select(Projection).SingleOrDefaultAsync(cancellationToken);
    }
    public Task<ServiceResult<int>> CreateAsync(LeaveTypeCatalogueSaveRequest request, CancellationToken cancellationToken = default) => Save(request, false, cancellationToken);
    public Task<ServiceResult<int>> UpdateAsync(LeaveTypeCatalogueSaveRequest request, CancellationToken cancellationToken = default) => Save(request, true, cancellationToken);
    private async Task<ServiceResult<int>> Save(LeaveTypeCatalogueSaveRequest request, bool edit, CancellationToken ct)
    {
        var a = await Scope(true);
        var code = (request.Code ?? "").Trim().ToUpperInvariant(); var name = (request.Name ?? "").Trim();
        if (code.Length is 0 or > 50 || name.Length is 0 or > 250 || request.Description?.Length > 1000 || request.NameDhivehi?.Length > 250)
            return ServiceResult<int>.Failure("Enter a valid code and leave type name.");
        if (!a.Super && (request.IsGlobal || request.IsSystemType)) return ServiceResult<int>.Failure("Only Super Administrators can manage shared or system leave types.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (request.IsActive || request.RequestTypeId.HasValue)
        {
            var routingError = await HRM.Services.Leave.LeaveRequestTypeLink.ValidateAsync(db, request.RequestTypeId, ct);
            if (routingError != null) return ServiceResult<int>.Failure(routingError);
        }
        var row = edit ? await db.LeaveTypes.SingleOrDefaultAsync(x => x.LeaveTypeId == request.LeaveTypeId && (x.OrganisationId == a.Org || x.IsGlobal), ct) : new LeaveType { OrganisationId = a.Org, CreatedDate = DateTime.UtcNow };
        if (row == null) return ServiceResult<int>.Failure("Leave type was not found in this organisation.");
        if (!a.Super && (row.IsGlobal || row.IsPublic || row.IsSystemType)) return ServiceResult<int>.Failure("Only Super Administrators can manage shared or system leave types.");
        if (await db.LeaveTypes.AnyAsync(x => x.Code == code && x.LeaveTypeId != row.LeaveTypeId, ct)) return ServiceResult<int>.Failure("This leave type code already exists.");
        if (await db.LeaveTypes.AnyAsync(x => x.OrganisationId == row.OrganisationId && x.Name == name && x.LeaveTypeId != row.LeaveTypeId, ct)) return ServiceResult<int>.Failure("This leave type name already exists in the organisation.");
        var global = a.Super ? request.IsGlobal : row.IsGlobal;
        if (request.IsActive && await db.LeaveTypes.AnyAsync(x => x.IsActive && x.LeaveTypeId != row.LeaveTypeId &&
            x.Name.Trim().ToUpper() == name.ToUpper() && (global || x.IsGlobal || x.OrganisationId == row.OrganisationId), ct))
            return ServiceResult<int>.Failure("An active leave type with this name is already available. Configure its organisation policy instead of creating another type.");
        var oldRequestType = row.RequestTypeId;
        row.RequestTypeId = request.RequestTypeId; row.Code = code; row.Name = name; row.NameDhivehi = request.NameDhivehi?.Trim(); row.Description = request.Description?.Trim();
        row.IsActive = request.IsActive; row.UpdatedDate = DateTime.UtcNow;
        if (a.Super) { row.IsGlobal = request.IsGlobal; row.IsSystemType = request.IsSystemType; }
        var audit = await (logs ?? throw new InvalidOperationException("Audit service is unavailable.")).CreateAsync(db,
            edit ? HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_TYPE_UDPATE : HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_TYPE_CREATE,
            $"{(edit ? "Updated" : "Created")} unified leave type {row.LeaveTypeId}: {code}, {name}, organisation {row.OrganisationId}, request type {oldRequestType} -> {row.RequestTypeId}.");
        row.OperationLogId = audit.OperationLogId;
        if (!edit) db.LeaveTypes.Add(row);
        await db.SaveChangesAsync(ct);
        if (!edit) { db.LeaveTypeReasonTypes.Add(new LeaveTypeReasonType { LeaveTypeId = row.LeaveTypeId, ReasonTypeName = "General" }); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return ServiceResult<int>.Successful(row.LeaveTypeId, "Leave type saved.");
    }
}
