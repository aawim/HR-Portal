namespace HRM.WorkPlanning.Abstractions;
public sealed record WorkforcePayrollExportLine(long LedgerEntryId,int IndividualId,int JobId,string TimeCategory,int ActualMinutes,int CreditedMinutes,DateTime StartAt,DateTime EndAt);
public sealed record WorkforcePayrollCorrection(int OrganisationId,long CorrectsLedgerEntryId,int ActualMinutes,int CreditedMinutes,string Reason,int OperationLogId);
public interface IWorkforcePayrollExportService{Task<IReadOnlyList<WorkforcePayrollExportLine>>PrepareAsync(int organisationId,string exportReference,CancellationToken ct=default);Task<long>CreateCorrectionAsync(WorkforcePayrollCorrection correction,CancellationToken ct=default);}
