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
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.ItemStaffRequestDto>>> GetStaffRequests([FromQuery] int? organisationId = null, [FromQuery] int? individualId = null)
        => Ok(await service.GetStaffRequestsAsync(organisationId, individualId));

    [HttpGet("get-pending-item-approvals")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.ItemStaffRequestDto>>> GetPendingItemApprovals()
        => Ok(await service.GetPendingItemApprovalsAsync());

    [HttpGet("get-requested-item-names")]
    public async Task<ActionResult<global::System.Collections.Generic.List<string>>> GetRequestedItemNames([FromQuery] int? individualId = null)
        => Ok(await service.GetRequestedItemNamesAsync(individualId));

    [HttpGet("get-requestable-items")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Items.RequestableItemDto>>> GetRequestableItems([FromQuery] int? organisationId = null)
        => Ok(await service.GetRequestableItemsAsync(organisationId));

    [HttpGet("can-manage-staff-items")]
    public async Task<ActionResult<bool>> CanManageStaffItems() => Ok(await service.CanManageStaffItemsAsync());

    [HttpPost("submit-staff-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitStaffRequest([FromBody] ItemStaffSubmitRequest request)
        => Ok(await service.SubmitStaffRequestAsync(request.model));

    public sealed class ItemStaffSubmitRequest
    {
        public global::HRM.DTOs.Items.ItemStaffRequestInput model { get; set; } = default!;
    }

    [HttpPost("act-staff-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ActStaffRequest([FromBody] ItemStaffActionRequest model)
        => Ok(await service.ActStaffRequestAsync(model.requestId, model.expectedState, model.action, model.comment));

    [HttpGet("get-staff-clearance-report")]
    public async Task<ActionResult<global::HRM.DTOs.Items.ItemStaffClearanceReportDto>> GetStaffClearanceReport([FromQuery] int individualId, [FromQuery] int? organisationId = null)
        => Ok(await service.GetStaffClearanceReportAsync(individualId, organisationId));

    public sealed class ItemStaffActionRequest
    {
        public int requestId { get; set; }
        public int expectedState { get; set; }
        public string action { get; set; } = "";
        public string? comment { get; set; }
    }

}
