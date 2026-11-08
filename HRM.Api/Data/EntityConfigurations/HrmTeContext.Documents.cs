using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<DmsCategory> DmsCategories => Set<DmsCategory>();
    public DbSet<DmsDocumentType> DmsDocumentTypes => Set<DmsDocumentType>();
    public DbSet<DmsRetentionPolicy> DmsRetentionPolicies => Set<DmsRetentionPolicy>();
    public DbSet<DmsStoredObject> DmsStoredObjects => Set<DmsStoredObject>();
    public DbSet<DmsDocument> DmsDocuments => Set<DmsDocument>();
    public DbSet<DmsDocumentVersion> DmsDocumentVersions => Set<DmsDocumentVersion>();
    public DbSet<DmsDocumentLink> DmsDocumentLinks => Set<DmsDocumentLink>();
    public DbSet<DmsDocumentPermission> DmsDocumentPermissions => Set<DmsDocumentPermission>();
    public DbSet<DmsDocumentEvent> DmsDocumentEvents => Set<DmsDocumentEvent>();
    public DbSet<DmsDocumentTemplate> DmsDocumentTemplates => Set<DmsDocumentTemplate>();
    public DbSet<DmsDocumentRequest> DmsDocumentRequests => Set<DmsDocumentRequest>();
    public DbSet<DmsLegalHold> DmsLegalHolds => Set<DmsLegalHold>();
    public DbSet<DmsDocumentSignature> DmsDocumentSignatures => Set<DmsDocumentSignature>();
}
