using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models.Archives;
using HRM.DTOs;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;



public sealed partial class OrganisationResourceSettingsService
{
    private static string DocumentVersion(DocumentType t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
        t.TypeName, t.TypeNameDhivehi, t.Description, t.IsActive, t.IsImage, t.ShouldBeColor, t.ImageMinimumResolution, t.OrganisationId, t.IsSystemType, t.ServiceTypeId }))));

    public async Task<OrganisationDocumentTypesData> LoadDocumentTypesAsync()
    {
        var (org, _, edit) = await Scope(null);
        await using var db = await factory.CreateDbContextAsync();
        var types = await db.DocumentTypes.AsNoTracking().Where(t => t.OrganisationId == org || t.OrganisationId == null).OrderBy(t => t.TypeName).ToListAsync();
        return new(org, await db.Organisations.Where(o => o.BusinessEntityID == org).Select(o => o.OrganisationName).SingleAsync(), edit,
            types.Select(t => new OrganisationDocumentType { Id=t.DocumentTypeId, Name=t.TypeName, DhivehiName=t.TypeNameDhivehi,
                Description=t.Description, IsActive=t.IsActive, IsImage=t.IsImage, ShouldBeColor=t.ShouldBeColor,
                ImageMinimumResolution=t.ImageMinimumResolution, IsShared=t.OrganisationId==null, IsSystem=t.IsSystemType, Version=DocumentVersion(t) }).ToList());
    }
    public async Task<ServiceResult> SaveDocumentTypeAsync(OrganisationDocumentType model, bool delete = false)
    {
        var (org, _, edit) = await Scope(null);
        if (!edit) throw new UnauthorizedAccessException("You cannot manage this organisation's document types.");
        if (!delete && (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length>250 || model.DhivehiName?.Length>250 || model.Description?.Length>2000 ||
            model.IsImage && model.ImageMinimumResolution is <=0)) return ServiceResult.Fail("Enter a name of up to 250 characters, description of up to 2,000 characters and a positive image resolution when specified.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        DocumentType entity;
        if (model.Id == 0 && !delete) entity = new() { OrganisationId=org };
        else
        {
            entity = await db.DocumentTypes.SingleOrDefaultAsync(t => t.DocumentTypeId==model.Id && t.OrganisationId==org && !t.IsSystemType)
                ?? throw new UnauthorizedAccessException("This document type is shared, protected or unavailable in your organisation.");
            if (model.Version != DocumentVersion(entity)) return ServiceResult.Fail("This document type changed. Refresh before saving.");
        }
        if (delete)
        {
            if (await db.Documents.AnyAsync(d=>d.DocumentTypeId==entity.DocumentTypeId) || await db.RequestTypeSpecificDocumentTypes.AnyAsync(d=>d.DocumentTypeId==entity.DocumentTypeId))
                return ServiceResult.Fail("This type is in use. Disable it to preserve existing documents and request requirements.");
            db.DocumentTypes.Remove(entity);
        }
        else
        {
            var name=model.Name.Trim();
            if (await db.DocumentTypes.AnyAsync(t=>t.DocumentTypeId!=model.Id && (t.OrganisationId==org || t.OrganisationId==null) && t.TypeName.Trim().ToUpper()==name.ToUpper()))
                return ServiceResult.Fail("A document type with this name already exists.");
            entity.TypeName=name;entity.TypeNameDhivehi=model.DhivehiName?.Trim();entity.Description=model.Description?.Trim();
            entity.IsActive=model.IsActive;entity.IsImage=model.IsImage;entity.ShouldBeColor=model.IsImage&&model.ShouldBeColor;
            entity.ImageMinimumResolution=model.IsImage?model.ImageMinimumResolution:null;
            if (model.Id==0) db.DocumentTypes.Add(entity);
        }
        await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
            $"{(delete?"Deleted":model.Id==0?"Created":"Updated")} document type {entity.DocumentTypeId} ({entity.TypeName}) for organisation {org}; active={entity.IsActive}.");
        await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok(delete?"Document type deleted.":"Document type saved.");
    }
}
