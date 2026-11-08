// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/document-management")]
public sealed class DocumentManagementController(global::HRM.Services.Contracts.DocumentManagementServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-dashboard")]
    public async Task<ActionResult<global::HRM.DTOs.Documents.DmsDashboardSummaryDto>> GetDashboard([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetDashboardAsync(organisationId));
    }

    [HttpGet("get-categories")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsCategoryDto>>> GetCategories([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? targetDomain = null)
    {
        return Ok(await service.GetCategoriesAsync(organisationId, targetDomain));
    }

    [HttpGet("get-document-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentTypeDto>>> GetDocumentTypes([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Int32? categoryId = null)
    {
        return Ok(await service.GetDocumentTypesAsync(organisationId, categoryId));
    }

    [HttpPost("save-document-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveDocumentType([FromBody] DocumentManagementSaveDocumentTypeRequest request)
    {
        return Ok(await service.SaveDocumentTypeAsync(request.input));
    }

    public sealed class DocumentManagementSaveDocumentTypeRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentTypeDto input { get; set; } = default!;
    }

    [HttpGet("get-retention-policies")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsRetentionPolicyDto>>> GetRetentionPolicies([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetRetentionPoliciesAsync(organisationId));
    }

    [HttpPost("save-retention-policy")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveRetentionPolicy([FromBody] DocumentManagementSaveRetentionPolicyRequest request)
    {
        return Ok(await service.SaveRetentionPolicyAsync(request.input));
    }

    public sealed class DocumentManagementSaveRetentionPolicyRequest
    {
        public global::HRM.DTOs.Documents.DmsRetentionPolicyDto input { get; set; } = default!;
    }

    [HttpPost("query/search-documents")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentSummaryDto>>> SearchDocuments([FromBody] DocumentManagementSearchDocumentsRequest request)
    {
        return Ok(await service.SearchDocumentsAsync(request.filter));
    }

    public sealed class DocumentManagementSearchDocumentsRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentSearchFilterDto filter { get; set; } = default!;
    }

    [HttpGet("get-staff-documents")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentSummaryDto>>> GetStaffDocuments([FromQuery] global::System.Int32 individualId, [FromQuery] global::System.String? categoryCode = null)
    {
        return Ok(await service.GetStaffDocumentsAsync(individualId, categoryCode));
    }

    [HttpGet("get-organisation-library")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentSummaryDto>>> GetOrganisationLibrary([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Int32? departmentStructureId = null, [FromQuery] global::System.String? categoryCode = null)
    {
        return Ok(await service.GetOrganisationLibraryAsync(organisationId, departmentStructureId, categoryCode));
    }

    [HttpGet("get-linked-entity-documents")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentSummaryDto>>> GetLinkedEntityDocuments([FromQuery] global::System.String targetEntityType, [FromQuery] global::System.Int64 targetEntityId)
    {
        return Ok(await service.GetLinkedEntityDocumentsAsync(targetEntityType, targetEntityId));
    }

    [HttpGet("get-document-detail")]
    public async Task<ActionResult<global::HRM.DTOs.Documents.DmsDocumentDetailDto?>> GetDocumentDetail([FromQuery] global::System.Int64 documentId)
    {
        return Ok(await service.GetDocumentDetailAsync(documentId));
    }

    [HttpPost("upload-document")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UploadDocument([FromBody] DocumentManagementUploadDocumentRequest request)
    {
        return Ok(await service.UploadDocumentAsync(request.input));
    }

    public sealed class DocumentManagementUploadDocumentRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentUploadInput input { get; set; } = default!;
    }

    [HttpPost("upload-new-version")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UploadNewVersion([FromBody] DocumentManagementUploadNewVersionRequest request)
    {
        return Ok(await service.UploadNewVersionAsync(request.input));
    }

    public sealed class DocumentManagementUploadNewVersionRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentNewVersionInput input { get; set; } = default!;
    }

    [HttpPost("update-document-metadata")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateDocumentMetadata([FromBody] DocumentManagementUpdateDocumentMetadataRequest request)
    {
        return Ok(await service.UpdateDocumentMetadataAsync(request.input));
    }

    public sealed class DocumentManagementUpdateDocumentMetadataRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentMetadataUpdateInput input { get; set; } = default!;
    }

    [HttpPost("download-document-version")]
    public async Task<ActionResult<global::HRM.DTOs.Documents.DmsDocumentDownloadDto>> DownloadDocumentVersion([FromBody] DocumentManagementDownloadDocumentVersionRequest request)
    {
        return Ok(await service.DownloadDocumentVersionAsync(request.documentId, request.versionNumber, request.isPreview));
    }

    public sealed class DocumentManagementDownloadDocumentVersionRequest
    {
        public global::System.Int64 documentId { get; set; }
        public global::System.Int32? versionNumber { get; set; } = null;
        public global::System.Boolean isPreview { get; set; } = false;
    }

    [HttpPost("link-document-to-record")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> LinkDocumentToRecord([FromBody] DocumentManagementLinkDocumentToRecordRequest request)
    {
        return Ok(await service.LinkDocumentToRecordAsync(request.input));
    }

    public sealed class DocumentManagementLinkDocumentToRecordRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentLinkInput input { get; set; } = default!;
    }

    [HttpPost("unlink-document-from-record")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UnlinkDocumentFromRecord([FromBody] DocumentManagementUnlinkDocumentFromRecordRequest request)
    {
        return Ok(await service.UnlinkDocumentFromRecordAsync(request.documentLinkId));
    }

    public sealed class DocumentManagementUnlinkDocumentFromRecordRequest
    {
        public global::System.Int64 documentLinkId { get; set; }
    }

    [HttpPost("grant-or-revoke-permission")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> GrantOrRevokePermission([FromBody] DocumentManagementGrantOrRevokePermissionRequest request)
    {
        return Ok(await service.GrantOrRevokePermissionAsync(request.input));
    }

    public sealed class DocumentManagementGrantOrRevokePermissionRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentPermissionInput input { get; set; } = default!;
    }

    [HttpPost("execute-lifecycle-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ExecuteLifecycleAction([FromBody] DocumentManagementExecuteLifecycleActionRequest request)
    {
        return Ok(await service.ExecuteLifecycleActionAsync(request.input));
    }

    public sealed class DocumentManagementExecuteLifecycleActionRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentLifecycleActionInput input { get; set; } = default!;
    }

    [HttpPost("manage-legal-hold")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ManageLegalHold([FromBody] DocumentManagementManageLegalHoldRequest request)
    {
        return Ok(await service.ManageLegalHoldAsync(request.input));
    }

    public sealed class DocumentManagementManageLegalHoldRequest
    {
        public global::HRM.DTOs.Documents.DmsLegalHoldInput input { get; set; } = default!;
    }

    [HttpGet("get-legal-holds")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsLegalHoldDto>>> GetLegalHolds([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Boolean activeOnly = true)
    {
        return Ok(await service.GetLegalHoldsAsync(organisationId, activeOnly));
    }

    [HttpGet("get-templates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentTemplateDto>>> GetTemplates([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Boolean activeOnly = true)
    {
        return Ok(await service.GetTemplatesAsync(organisationId, activeOnly));
    }

    [HttpPost("save-template")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveTemplate([FromBody] DocumentManagementSaveTemplateRequest request)
    {
        return Ok(await service.SaveTemplateAsync(request.input));
    }

    public sealed class DocumentManagementSaveTemplateRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentTemplateDto input { get; set; } = default!;
    }

    [HttpGet("resolve-template-preview")]
    public async Task<ActionResult<global::HRM.DTOs.Documents.DmsTemplatePreviewDto>> ResolveTemplatePreview([FromQuery] global::System.Int32 documentTemplateId, [FromQuery] global::System.Int32? subjectIndividualId = null, [FromQuery] global::System.String? purpose = null, [FromQuery] global::System.String? addressedTo = null)
    {
        return Ok(await service.ResolveTemplatePreviewAsync(documentTemplateId, subjectIndividualId, purpose, addressedTo));
    }

    [HttpGet("get-my-document-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentRequestDto>>> GetMyDocumentRequests()
    {
        return Ok(await service.GetMyDocumentRequestsAsync());
    }

    [HttpGet("get-admin-document-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Documents.DmsDocumentRequestDto>>> GetAdminDocumentRequests([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? statusFilter = null)
    {
        return Ok(await service.GetAdminDocumentRequestsAsync(organisationId, statusFilter));
    }

    [HttpPost("submit-document-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitDocumentRequest([FromBody] DocumentManagementSubmitDocumentRequestRequest request)
    {
        return Ok(await service.SubmitDocumentRequestAsync(request.input));
    }

    public sealed class DocumentManagementSubmitDocumentRequestRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentRequestSubmitInput input { get; set; } = default!;
    }

    [HttpPost("process-document-request-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ProcessDocumentRequestAction([FromBody] DocumentManagementProcessDocumentRequestActionRequest request)
    {
        return Ok(await service.ProcessDocumentRequestActionAsync(request.input));
    }

    public sealed class DocumentManagementProcessDocumentRequestActionRequest
    {
        public global::HRM.DTOs.Documents.DmsDocumentRequestActionInput input { get; set; } = default!;
    }

}
