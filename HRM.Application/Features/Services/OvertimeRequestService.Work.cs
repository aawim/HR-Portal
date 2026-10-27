using System.Data;
using HRM.DTOs;
using HRM.Models;
using HRM.Components.Shared;
namespace HRM.Services;

public sealed record OvertimeClockOption(int Id, DateTime Time);

public sealed record OvertimeTaskOption(int Id, int RequestId, string Reference, string Task, DateTime From, DateTime To);

public sealed record OvertimeWorkRow(int Id, int StateId, string Reference, string Staff, string Kind,
    DateTime From, DateTime To, string Reason, List<string> Tasks, string Status, bool CanApprove, bool CanCancel,
    DateTime? Start, DateTime? End);
