using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave;

public sealed record CarryForwardReview(int RuleId, int RuleVersion, int JobId, int BalanceId, string Staff,
    string LeaveType, DateTime RenewalDate, decimal Balance, decimal SuggestedDays)
{
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd => RenewalDate.AddDays(-1);
    public decimal ClosingGrant { get; init; }
    public decimal UnusedBalance => Balance + ClosingGrant;
    public decimal AnnualEntitlement { get; init; }
    public decimal RenewalGrant { get; init; }
    public decimal ScheduledRenewalGrant { get; init; }
    public decimal? MaximumBalance { get; init; }
    public decimal RenewalGrantFor(decimal carriedDays) => MaximumBalance.HasValue
        ? Math.Max(0, Math.Min(ScheduledRenewalGrant, MaximumBalance.Value - carriedDays)) : ScheduledRenewalGrant;
    public string AccrualDescription { get; init; } = "";
}
