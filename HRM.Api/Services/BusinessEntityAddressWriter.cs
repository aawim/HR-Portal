using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

internal sealed class AddressMutation
{
    public int LinkId { get; init; }
    public int? ExistingLocationId { get; init; }
    public int UsageId { get; init; }
    public int BaseTypeId { get; init; }
    public int? InstanceTypeId { get; init; }
    public int CountryId { get; init; }
    public int? IslandId { get; init; }
    public int? WardId { get; init; }
    public string? Name { get; init; }
    public string? Line1 { get; init; }
    public string? Line2 { get; init; }
    public string? Street { get; init; }
    public string? Floor { get; init; }
    public string? PostCode { get; init; }
    public bool IsCurrent { get; init; }
    public bool ClearOtherCurrent { get; init; }
}

// Entity-specific callers authorise the owner and supply their transaction and audit record.
internal static class BusinessEntityAddressWriter
{
    internal static async Task<ServiceResult> SaveAsync(HrmTeContext db, int owner, AddressMutation value, int auditId)
    {
        if (owner <= 0 || !await db.Individuals.AnyAsync(i => i.BusinessEntityId == owner) && !await db.Organisations.AnyAsync(o => o.BusinessEntityID == owner))
            return ServiceResult.Fail("The address owner was not found.");
        var original = value.LinkId == 0 ? null : await db.BusinessEntityLocations.SingleOrDefaultAsync(l => l.BusinessEntityId == owner && l.BusinessEntityLocationId == value.LinkId && l.IsValid);
        if (value.LinkId != 0 && original == null) return ServiceResult.Fail("Address was not found for this owner.");
        if (!await db.BusinessEntityLocationTypes.AnyAsync(t => t.BusinessEntityLocationTypeId == value.UsageId)) return ServiceResult.Fail("Select a valid address usage.");
        int locationId;
        if (value.ExistingLocationId is int existing)
        {
            if (!await db.AddressInstances.AnyAsync(a => a.LocationId == existing)) return ServiceResult.Fail("Select an existing address.");
            locationId = existing;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(value.Name) && string.IsNullOrWhiteSpace(value.Line1) || value.Name?.Length > 250 || value.Line1?.Length > 250 || value.Line2?.Length > 250 || value.Street?.Length > 250 || value.Floor?.Length > 250 || value.PostCode?.Length > 20)
                return ServiceResult.Fail("Enter an address name or address line, and keep the fields within their allowed lengths.");
            if (!await db.Countries.AnyAsync(c => c.CountryId == value.CountryId) || !await db.AddressBaseTypes.AnyAsync(t => t.AddressBaseTypeId == value.BaseTypeId))
                return ServiceResult.Fail("Select a valid address type and country.");
            if (value.InstanceTypeId.HasValue && !await db.AddressInstanceTypes.AnyAsync(t => t.AddressInstanceTypeId == value.InstanceTypeId) ||
                value.IslandId.HasValue && !await db.Islands.AnyAsync(i => i.IslandId == value.IslandId) ||
                value.WardId.HasValue && !await db.Wards.AnyAsync(w => w.WardId == value.WardId && w.IslandId == value.IslandId))
                return ServiceResult.Fail("Select a valid address instance type, island and ward.");
            var locationType = await db.LocationTypes.Where(t => t.TypeName == "Address").Select(t => t.LocationTypeId).SingleOrDefaultAsync();
            if (locationType == 0) return ServiceResult.Fail("The Address location type is not configured.");
            var basis = new AddressBasis { AddressBaseTypeId = value.BaseTypeId, AddressLine1 = value.Line1?.Trim(), AddressLine2 = value.Line2?.Trim(),
                StreetNameEnglish = value.Street?.Trim(), PostCode = value.PostCode?.Trim(), WardId = value.WardId, IsCurrent = value.IsCurrent };
            var location = new Location { LocationTypeId = locationType, CountryId = value.CountryId, OperationLogId = auditId };
            db.AddressBases.Add(basis); db.Locations.Add(location); await db.SaveChangesAsync();
            db.Addresses.Add(new Address { LocationId = location.LocationId, IslandId = value.IslandId });
            db.AddressInstances.Add(new AddressInstance { LocationId = location.LocationId, AddressBaseId = basis.AddressBaseId,
                AddressInstanceTypeId = value.InstanceTypeId, Name = value.Name?.Trim(), Floor = value.Floor?.Trim() });
            await db.SaveChangesAsync(); locationId = location.LocationId;
            if (value.IsCurrent && value.ClearOtherCurrent)
            {
                var older = await db.BusinessEntityLocations.Where(l => l.BusinessEntityId == owner && l.IsValid && l.LocationId != locationId &&
                    l.Location.Address != null && l.Location.Address.AddressInstance != null)
                    .Select(l => l.Location.Address!.AddressInstance!.AddressBase).Distinct().ToListAsync();
                foreach (var old in older)
                    if (!await db.BusinessEntityLocations.AnyAsync(l => l.BusinessEntityId != owner && l.IsValid && l.Location.Address != null &&
                        l.Location.Address.AddressInstance != null && l.Location.Address.AddressInstance.AddressBaseId == old.AddressBaseId)) old.IsCurrent = false;
            }
        }
        if (original != null) original.IsValid = false;
        var link = await db.BusinessEntityLocations.SingleOrDefaultAsync(l => l.BusinessEntityId == owner && l.LocationId == locationId && l.BusinessEntityLocationTypeId == value.UsageId);
        if (link == null) db.BusinessEntityLocations.Add(new() { BusinessEntityId = owner, LocationId = locationId, BusinessEntityLocationTypeId = value.UsageId, IsValid = true });
        else link.IsValid = true;
        await db.SaveChangesAsync(); return ServiceResult.Ok(value.LinkId == 0 ? "Address added." : "Address updated.");
    }
    internal static async Task<ServiceResult> UnlinkAsync(HrmTeContext db, int owner, int linkId)
    {
        var link = await db.BusinessEntityLocations.SingleOrDefaultAsync(l => l.BusinessEntityId == owner && l.BusinessEntityLocationId == linkId && l.IsValid);
        if (link == null) return ServiceResult.Fail("Address was not found for this owner.");
        link.IsValid = false; await db.SaveChangesAsync(); return ServiceResult.Ok("Address removed.");
    }
}
