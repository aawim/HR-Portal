using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services;

public sealed record WeeklyHourExceptionRow(int Id, int StateId, string Reference, string Staff, DateTime WeekStart,
    int Minutes, string Reason, string Status, bool CanApprove, bool CanCancel);
