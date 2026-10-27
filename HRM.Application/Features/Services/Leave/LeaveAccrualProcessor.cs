using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
namespace HRM.Services.Leave;

public sealed record AccrualPreview(int JobId, DateTime? DueDate, decimal Grant, decimal PreviousBalance, decimal NewBalance, string Message);
