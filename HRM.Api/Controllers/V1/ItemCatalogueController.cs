// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/item-catalogue")]
public sealed class ItemCatalogueController(global::HRM.Services.Contracts.ItemCatalogueServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Items.ItemCatalogueData>> Load([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadAsync(organisationId));
    }

    [HttpGet("get-item-units")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.ItemUnitDto>>> GetItemUnits([FromQuery] global::System.Int32 itemId, [FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetItemUnitsAsync(itemId, organisationId));
    }

    [HttpPost("save-category")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveCategory([FromBody] ItemCatalogueSaveCategoryRequest request)
    {
        return Ok(await service.SaveCategoryAsync(request.model, request.delete));
    }

    public sealed class ItemCatalogueSaveCategoryRequest
    {
        public global::HRM.DTOs.Items.ItemCategoryInput model { get; set; } = default!;
        public global::System.Boolean delete { get; set; } = false;
    }

    [HttpPost("save-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveItem([FromBody] ItemCatalogueSaveItemRequest request)
    {
        return Ok(await service.SaveItemAsync(request.model, request.delete));
    }

    public sealed class ItemCatalogueSaveItemRequest
    {
        public global::HRM.DTOs.Items.ItemInput model { get; set; } = default!;
        public global::System.Boolean delete { get; set; } = false;
    }

    [HttpPost("save-unit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveUnit([FromBody] ItemCatalogueSaveUnitRequest request)
    {
        return Ok(await service.SaveUnitAsync(request.model, request.delete));
    }

    public sealed class ItemCatalogueSaveUnitRequest
    {
        public global::HRM.DTOs.Items.ItemUnitInput model { get; set; } = default!;
        public global::System.Boolean delete { get; set; } = false;
    }

    [HttpGet("get-staff-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.ItemStaffRequestDto>>> GetStaffRequests([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Int32? individualId = null)
    {
        return Ok(await service.GetStaffRequestsAsync(organisationId, individualId));
    }

    [HttpGet("get-pending-item-approvals")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.ItemStaffRequestDto>>> GetPendingItemApprovals()
    {
        return Ok(await service.GetPendingItemApprovalsAsync());
    }

    [HttpGet("get-requested-item-names")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::System.String>>> GetRequestedItemNames([FromQuery] global::System.Int32? individualId = null)
    {
        return Ok(await service.GetRequestedItemNamesAsync(individualId));
    }

    [HttpGet("get-requestable-items")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.RequestableItemDto>>> GetRequestableItems([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetRequestableItemsAsync(organisationId));
    }

    [HttpGet("can-manage-staff-items")]
    public async Task<ActionResult<global::System.Boolean>> CanManageStaffItems()
    {
        return Ok(await service.CanManageStaffItemsAsync());
    }

    [HttpPost("submit-staff-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitStaffRequest([FromBody] ItemCatalogueSubmitStaffRequestRequest request)
    {
        return Ok(await service.SubmitStaffRequestAsync(request.model));
    }

    public sealed class ItemCatalogueSubmitStaffRequestRequest
    {
        public global::HRM.DTOs.Items.ItemStaffRequestInput model { get; set; } = default!;
    }

    [HttpPost("act-staff-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ActStaffRequest([FromBody] ItemCatalogueActStaffRequestRequest request)
    {
        return Ok(await service.ActStaffRequestAsync(request.requestId, request.expectedState, request.action, request.comment));
    }

    public sealed class ItemCatalogueActStaffRequestRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

    [HttpGet("get-staff-clearance-report")]
    public async Task<ActionResult<global::HRM.DTOs.Items.ItemStaffClearanceReportDto>> GetStaffClearanceReport([FromQuery] global::System.Int32 individualId, [FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetStaffClearanceReportAsync(individualId, organisationId));
    }

}
