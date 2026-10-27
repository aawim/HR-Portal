using System.Data;
using System.Text.Json;
using HRM.Models;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Components.Shared;
namespace HRM.Services;
public sealed record OvertimeRow(int Id,int StateId,string Reference,string Staff,DateTime Date,int Minutes,string Status,string Reason,bool CanApprove,bool CanCancel,bool CanPay,string? PaymentReference)
{ public string Source { get; init; } = "Legacy"; public DateTime? Start { get; init; } public DateTime? End { get; init; } public string? Task { get; init; }
    public bool CanReviseApproved {get;init;} public bool CanResubmit {get;init;}
    public bool PayrollProcessed {get;init;} public DateTime? PayrollMonth {get;init;} public string? PayrollReference {get;init;}
}
