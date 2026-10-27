using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.Services;

public sealed record OperationalGroupOption(int Id, string Name);

public sealed record OperationalGroupRow(int Id, string Name, int TypeId, string Type, int StructureId, string Structure, int AuditId);

public sealed record OperationalGroupsData(int OrganisationId, string Organisation, bool IsSuperAdmin, bool CanManage,
    List<OperationalGroupOption> Organisations, List<OperationalGroupOption> Types, List<OperationalGroupOption> Structures, List<OperationalGroupRow> Groups);
