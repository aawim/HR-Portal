// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/certificate")]
public sealed class CertificateController(global::HRM.Services.Contracts.CertificateServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-my-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Certificates.CertificateRequestSummaryDto>>> GetMyRequests()
    {
        return Ok(await service.GetMyRequestsAsync());
    }

    [HttpGet("get-my-issued-certificates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Certificates.IssuedCertificateDetailDto>>> GetMyIssuedCertificates()
    {
        return Ok(await service.GetMyIssuedCertificatesAsync());
    }

    [HttpGet("get-verified-employment-history")]
    public async Task<ActionResult<global::HRM.DTOs.Certificates.CertificateVerifiedEmploymentHistoryDto>> GetVerifiedEmploymentHistory([FromQuery] global::System.Int32? individualId = null)
    {
        return Ok(await service.GetVerifiedEmploymentHistoryAsync(individualId));
    }

    [HttpPost("submit-certificate-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitCertificateRequest([FromBody] CertificateSubmitCertificateRequestRequest request)
    {
        return Ok(await service.SubmitCertificateRequestAsync(request.input));
    }

    public sealed class CertificateSubmitCertificateRequestRequest
    {
        public global::HRM.DTOs.Certificates.CertificateRequestInput input { get; set; } = default!;
    }

    [HttpGet("cancel-certificate-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CancelCertificateRequest([FromQuery] global::System.Int32 certificateRequestId)
    {
        return Ok(await service.CancelCertificateRequestAsync(certificateRequestId));
    }

    [HttpGet("get-admin-pending-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Certificates.CertificateRequestSummaryDto>>> GetAdminPendingRequests([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? statusFilter = null)
    {
        return Ok(await service.GetAdminPendingRequestsAsync(organisationId, statusFilter));
    }

    [HttpGet("get-request-detail")]
    public async Task<ActionResult<global::HRM.DTOs.Certificates.CertificateRequestDetailDto>> GetRequestDetail([FromQuery] global::System.Int32 certificateRequestId)
    {
        return Ok(await service.GetRequestDetailAsync(certificateRequestId));
    }

    [HttpPost("review-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReviewAction([FromBody] CertificateReviewActionRequest request)
    {
        return Ok(await service.ReviewActionAsync(request.input));
    }

    public sealed class CertificateReviewActionRequest
    {
        public global::HRM.DTOs.Certificates.CertificateReviewActionInput input { get; set; } = default!;
    }

    [HttpPost("revoke-certificate")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RevokeCertificate([FromBody] CertificateRevokeCertificateRequest request)
    {
        return Ok(await service.RevokeCertificateAsync(request.input));
    }

    public sealed class CertificateRevokeCertificateRequest
    {
        public global::HRM.DTOs.Certificates.CertificateRevokeInput input { get; set; } = default!;
    }

    [HttpPost("reissue-certificate")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReissueCertificate([FromBody] CertificateReissueCertificateRequest request)
    {
        return Ok(await service.ReissueCertificateAsync(request.input));
    }

    public sealed class CertificateReissueCertificateRequest
    {
        public global::HRM.DTOs.Certificates.CertificateReissueInput input { get; set; } = default!;
    }

    [HttpPost("verify-certificate")]
    public async Task<ActionResult<global::HRM.DTOs.Certificates.CertificateVerificationResultDto>> VerifyCertificate([FromBody] CertificateVerifyCertificateRequest request)
    {
        return Ok(await service.VerifyCertificateAsync(request.tokenOrNumber));
    }

    public sealed class CertificateVerifyCertificateRequest
    {
        public global::System.String tokenOrNumber { get; set; } = default!;
    }

    [HttpGet("get-certificate-snapshot")]
    public async Task<ActionResult<global::HRM.DTOs.Certificates.CertificateSnapshotDto>> GetCertificateSnapshot([FromQuery] global::System.Int32 issuedCertificateId)
    {
        return Ok(await service.GetCertificateSnapshotAsync(issuedCertificateId));
    }

    [HttpGet("get-issued-certificate-pdf")]
    public async Task<ActionResult<global::System.Byte[]?>> GetIssuedCertificatePdf([FromQuery] global::System.Int32 issuedCertificateId)
    {
        return Ok(await service.GetIssuedCertificatePdfAsync(issuedCertificateId));
    }

    [HttpPost("save-issued-certificate-pdf")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveIssuedCertificatePdf([FromBody] CertificateSaveIssuedCertificatePdfRequest request)
    {
        return Ok(await service.SaveIssuedCertificatePdfAsync(request.issuedCertificateId, request.pdfBytes));
    }

    public sealed class CertificateSaveIssuedCertificatePdfRequest
    {
        public global::System.Int32 issuedCertificateId { get; set; }
        public global::System.Byte[] pdfBytes { get; set; } = default!;
    }

    [HttpGet("get-audit-logs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Certificates.CertificateAuditLogDto>>> GetAuditLogs([FromQuery] global::System.Int32? certificateRequestId = null, [FromQuery] global::System.Int32? issuedCertificateId = null)
    {
        return Ok(await service.GetAuditLogsAsync(certificateRequestId, issuedCertificateId));
    }

}
