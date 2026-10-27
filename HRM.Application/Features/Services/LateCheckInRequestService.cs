using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services;

public sealed record LateCheckInRow(int Id, int StateId, string Reference, string Staff, string Schedule, DateTime ScheduledStart,
    DateTime? ActualCheckIn, int LateMinutes, string Reason, string Status, string Stage, bool CanApprove, bool CanCancel, List<string> History,
    string Kind, DateTime? RequestedStart, DateTime? RequestedEnd);
