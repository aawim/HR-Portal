using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Certificates;

namespace HRM.Services.Contracts;

public abstract class CertificateServiceContracts
{
    public const string RequestTypeName = "Employment Certificate Request";
    public const string OnlineUrl = "/online/certificates";
    public const string AdminUrl = "/admin/hr/certificates";
    public const string VerifyUrl = "/verify/certificate";

    public interface IOperations
    {
        Task<List<CertificateRequestSummaryDto>> GetMyRequestsAsync();
        Task<List<IssuedCertificateDetailDto>> GetMyIssuedCertificatesAsync();
        Task<CertificateVerifiedEmploymentHistoryDto> GetVerifiedEmploymentHistoryAsync(int? individualId = null);
        Task<ServiceResult> SubmitCertificateRequestAsync(CertificateRequestInput input);
        Task<ServiceResult> CancelCertificateRequestAsync(int certificateRequestId);
        Task<List<CertificateRequestSummaryDto>> GetAdminPendingRequestsAsync(int? organisationId = null, string? statusFilter = null);
        Task<CertificateRequestDetailDto> GetRequestDetailAsync(int certificateRequestId);
        Task<ServiceResult> ReviewActionAsync(CertificateReviewActionInput input);
        Task<ServiceResult> RevokeCertificateAsync(CertificateRevokeInput input);
        Task<ServiceResult> ReissueCertificateAsync(CertificateReissueInput input);
        Task<CertificateVerificationResultDto> VerifyCertificateAsync(string tokenOrNumber);
        Task<CertificateSnapshotDto> GetCertificateSnapshotAsync(int issuedCertificateId);
        Task<byte[]?> GetIssuedCertificatePdfAsync(int issuedCertificateId);
        Task<ServiceResult> SaveIssuedCertificatePdfAsync(int issuedCertificateId, byte[] pdfBytes);
        Task<List<CertificateAuditLogDto>> GetAuditLogsAsync(int? certificateRequestId = null, int? issuedCertificateId = null);
    }
}
