using HRM.DTOs;
using HRM.DTOs.Items;

namespace HRM.Services.Contracts;

public abstract class ItemCatalogueServiceContracts
{
    public const string PageUrl = "/admin/hr/items";

    public const string AssetKind = "Asset";
    public const string ConsumableKind = "Consumable";
    public const string RequisitionTypeName = "Item Requisition Request";
    public const string IssueTypeName = "Staff Item Issue Request";

    public interface IOperations
    {
        Task<ItemCatalogueData> LoadAsync(int? organisationId = null);
        Task<List<ItemUnitDto>> GetItemUnitsAsync(int itemId, int? organisationId = null);
        Task<ServiceResult> SaveCategoryAsync(ItemCategoryInput model, bool delete = false);
        Task<ServiceResult> SaveItemAsync(ItemInput model, bool delete = false);
        Task<ServiceResult> SaveUnitAsync(ItemUnitInput model, bool delete = false);
        Task<List<ItemStaffRequestDto>> GetStaffRequestsAsync(int? organisationId = null, int? individualId = null);
        Task<List<ItemStaffRequestDto>> GetPendingItemApprovalsAsync();
        Task<List<string>> GetRequestedItemNamesAsync(int? individualId = null);
        Task<List<RequestableItemDto>> GetRequestableItemsAsync(int? organisationId = null);
        Task<bool> CanManageStaffItemsAsync();
        Task<ServiceResult> SubmitStaffRequestAsync(ItemStaffRequestInput model);
        Task<ServiceResult> ActStaffRequestAsync(int requestId, int expectedState, string action, string? comment);
    }
}
