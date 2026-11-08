using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Documents;

namespace HRM.Services.Contracts;

public abstract class DocumentManagementServiceContracts
{
    public const string RequestTypeName = "Official Document Request";
    public const string OnlineUrl = "/online/documents";
    public const string AdminUrl = "/admin/hr/documents";

    public interface IOperations
    {
        // Dashboard & Configuration Catalogues
        Task<DmsDashboardSummaryDto> GetDashboardAsync(int? organisationId = null);
        Task<List<DmsCategoryDto>> GetCategoriesAsync(int? organisationId = null, string? targetDomain = null);
        Task<List<DmsDocumentTypeDto>> GetDocumentTypesAsync(int? organisationId = null, int? categoryId = null);
        Task<ServiceResult> SaveDocumentTypeAsync(DmsDocumentTypeDto input);
        Task<List<DmsRetentionPolicyDto>> GetRetentionPoliciesAsync(int? organisationId = null);
        Task<ServiceResult> SaveRetentionPolicyAsync(DmsRetentionPolicyDto input);

        // Central Repository Search, Registration, Upload, Download & Preview
        Task<List<DmsDocumentSummaryDto>> SearchDocumentsAsync(DmsDocumentSearchFilterDto filter);
        Task<List<DmsDocumentSummaryDto>> GetStaffDocumentsAsync(int individualId, string? categoryCode = null);
        Task<List<DmsDocumentSummaryDto>> GetOrganisationLibraryAsync(int? organisationId = null, int? departmentStructureId = null, string? categoryCode = null);
        Task<List<DmsDocumentSummaryDto>> GetLinkedEntityDocumentsAsync(string targetEntityType, long targetEntityId);
        Task<DmsDocumentDetailDto?> GetDocumentDetailAsync(long documentId);
        Task<ServiceResult> UploadDocumentAsync(DmsDocumentUploadInput input);
        Task<ServiceResult> UploadNewVersionAsync(DmsDocumentNewVersionInput input);
        Task<ServiceResult> UpdateDocumentMetadataAsync(DmsDocumentMetadataUpdateInput input);
        Task<DmsDocumentDownloadDto> DownloadDocumentVersionAsync(long documentId, int? versionNumber = null, bool isPreview = false);

        // Multi-Record Linking, Security & Lifecycle Governance
        Task<ServiceResult> LinkDocumentToRecordAsync(DmsDocumentLinkInput input);
        Task<ServiceResult> UnlinkDocumentFromRecordAsync(long documentLinkId);
        Task<ServiceResult> GrantOrRevokePermissionAsync(DmsDocumentPermissionInput input);
        Task<ServiceResult> ExecuteLifecycleActionAsync(DmsDocumentLifecycleActionInput input);
        Task<ServiceResult> ManageLegalHoldAsync(DmsLegalHoldInput input);
        Task<List<DmsLegalHoldDto>> GetLegalHoldsAsync(int? organisationId = null, bool activeOnly = true);

        // Templates, Verified HRM Placeholder Resolution & Official Document Requests
        Task<List<DmsDocumentTemplateDto>> GetTemplatesAsync(int? organisationId = null, bool activeOnly = true);
        Task<ServiceResult> SaveTemplateAsync(DmsDocumentTemplateDto input);
        Task<DmsTemplatePreviewDto> ResolveTemplatePreviewAsync(int documentTemplateId, int? subjectIndividualId = null, string? purpose = null, string? addressedTo = null);
        Task<List<DmsDocumentRequestDto>> GetMyDocumentRequestsAsync();
        Task<List<DmsDocumentRequestDto>> GetAdminDocumentRequestsAsync(int? organisationId = null, string? statusFilter = null);
        Task<ServiceResult> SubmitDocumentRequestAsync(DmsDocumentRequestSubmitInput input);
        Task<ServiceResult> ProcessDocumentRequestActionAsync(DmsDocumentRequestActionInput input);
    }
}
