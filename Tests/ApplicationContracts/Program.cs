using System.Reflection;
using System.Text.Json;
using HRM.Application.Contracts;
using HRM.Application.Contracts.Pages;
using HRM.Application.Transport;
using HRM.DTOs.Contracts;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var assembly = typeof(ApplicationContractCatalog).Assembly;
Check(assembly.GetReferencedAssemblies().All(a => !a.Name!.StartsWith("Microsoft.AspNetCore") && !a.Name.StartsWith("Microsoft.EntityFrameworkCore") && a.Name is not ("HRM.Api" or "HRM.UI")), "Application depends on a host or EF.");
var visited = new HashSet<Type>();
void CheckPayload(Type type, string path)
{
    if (type == typeof(CancellationToken) || type == typeof(void) || !visited.Add(type)) return;
    Check(type.Namespace?.StartsWith("HRM.Models") != true, $"Entity exposed at {path}: {type}");
    Check(type != typeof(object) && type != typeof(Type) && !typeof(Delegate).IsAssignableFrom(type) &&
        !typeof(IQueryable).IsAssignableFrom(type), $"Unbounded or executable payload at {path}: {type}");
    Check(!type.IsPointer && !type.IsByRef && !type.ContainsGenericParameters, $"Open payload at {path}: {type}");
    if (type.IsArray) { CheckPayload(type.GetElementType()!, path + "[]"); return; }
    if (type.IsGenericType) foreach (var argument in type.GenericTypeArguments) CheckPayload(argument, path + "<>" );
    if (type.Assembly != assembly) return;
    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) CheckPayload(property.PropertyType, path + "." + property.Name);
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) CheckPayload(field.FieldType, path + "." + field.Name);
}
Check(ApplicationContractCatalog.All.Distinct().Count() == ApplicationContractCatalog.All.Count, "Duplicate contract registration.");
foreach (var contract in assembly.GetTypes().Where(t => t.IsInterface && (t.DeclaringType?.Name.EndsWith("Contracts") == true || t.Namespace == "HRM.Application.Contracts.Pages")))
    Check(ApplicationContractCatalog.All.Contains(contract), "New remote contract missing from the reviewed catalog: " + contract.FullName);
var operations = 0;
foreach (var contract in ApplicationContractCatalog.All)
{
    _ = new ApiOperationDescriptor(contract);
    foreach (var method in contract.GetMethods())
    {
        operations++;
        CheckPayload(method.ReturnType, contract.Name + "." + method.Name + " result");
        foreach (var parameter in method.GetParameters()) CheckPayload(parameter.ParameterType, contract.Name + "." + method.Name + "(" + parameter.Name + ")");
    }
}
Console.WriteLine($"PASS {ApplicationContractCatalog.All.Count} remote contracts / {operations} operations: async, DTO-only, bounded payloads and no host/EF references");

T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, ApiOperationKeys.Json), ApiOperationKeys.Json)!;
var balance = RoundTrip(new HRM.DTOs.Leave.JobLeaveTypeDto { JobLeaveTypeId = 8, JobId = 12, LeaveTypeId = 4, LeaveTypeName = "Annual Leave", RemainingDays = 17.5m, EffectiveToDate = new DateTime(2027, 1, 1) });
Check(balance.RemainingDays == 17.5m && balance.LeaveTypeName == "Annual Leave" && balance.JobLeaveTypeId == 8, "Leave identity or fractional days changed in transit.");
var application = RoundTrip(new OutOfOfficeApplication { RequestTypeId = 4, Date = new(2026, 10, 27), ExpectedOut = new(8, 30), ExpectedIn = new(9, 15), ExpectedStateId = 7, Remarks = "Appointment" });
Check(application.Date == new DateOnly(2026, 10, 27) && application.ExpectedOut == new TimeOnly(8, 30) && application.ExpectedStateId == 7, "Local date/time or concurrency state changed in transit.");
var rule = RoundTrip(new JobTypeOvertimeRuleDto { OrganisationId = 3, JobTypeId = 2, IsAllowed = true, MaximumWeeklyHours = 8.5m, Version = 9 });
Check(rule.MaximumWeeklyHours == 8.5m && rule.Version == 9 && rule.OrganisationId == 3, "Policy identity/version changed in transit.");
var eventRow = RoundTrip(new AttendanceLogDto { AttendanceLogId = 7, Date = new(2026, 10, 27, 8, 31, 1), AttendanceLogResolutions = [new AttendanceLogResolutionDto { AttendanceClockTypeId = 1, WorkAssignmentId = 42, IsValid = true }] });
Check(eventRow.AttendanceLogResolutions.Single().WorkAssignmentId == 42 && eventRow.Date.Minute == 31, "Attendance resolution data was lost.");
var workflow = RoundTrip(new LeaveRequestWorkflowSummary(8, 12, "LV-12", 7, "Pending HR Approval", true, false, null, true, 100, "Active", "HR", false));
Check(workflow.IsProcessing && !workflow.IsFinal && workflow.HasHandover && workflow.CurrentStage == "HR", "Workflow state flags were lost.");
Console.WriteLine("PASS DTO JSON round trips: leave fractions, local dates/times, policy versions, clock resolutions and workflow state flags");

var report = RoundTrip(new ExportReportDto("Staff history", "Read only", [new("Balances", ["Leave", "Days"],
    [[new(ExportCellKind.Text, Text: "Annual Leave"), new(ExportCellKind.Number, Number: 17.5m)]])]));
Check(report.Tables.Single().Rows.Single()[1].Number == 17.5m, "Typed report cells lost numeric values.");
Check(new ScopeScheduleAssignmentDto().WorkingDaysMask == 31 && new JobTypeLeaveAccrualRuleDto().RepeatEveryMonths == 12 && new JobTypeOvertimeRuleDto().Version == 1,
    "Contract defaults differ from existing configuration defaults.");
Console.WriteLine("PASS Reviewed catalog completeness, scalar report cells and existing configuration defaults");
