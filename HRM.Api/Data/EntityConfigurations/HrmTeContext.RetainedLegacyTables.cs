using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Models.Archives;
namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<TableRetirementAudit> TableRetirementAudits=>Set<TableRetirementAudit>();
    private static void ConfigureRetainedLegacyTableNames(ModelBuilder modelBuilder)
    {
        (Type Type,string Name)[] mappings=
        [
            (typeof(BreakTime),"BT_BreakTimes"),
            (typeof(BusinessEntitiesDocument),"BE_BusinessEntityDocuments"),
            (typeof(BusinessEntityCalendar),"BE_BusinessEntityCalendars"),
            (typeof(BusinessEntityLocation),"BE_BusinessEntityLocations"),
            (typeof(BusinessEntityLocationType),"BE_BusinessEntityLocationTypes"),
            (typeof(BusinessEntitySchedule),"BE_BusinessEntitySchedules"),
            (typeof(BusinessEntityType),"BE_BusinessEntityTypes"),
            (typeof(BussinessEntityContactInformation),"BE_BusinessEntityContacts")
        ];
        foreach(var item in mappings)
        {
            var entity=modelBuilder.Entity(item.Type);
            // Preserve existing keys/index names and identities; only the physical table changes.
            var key=entity.Metadata.FindPrimaryKey();var keyName=key?.GetName();
            var indexes=entity.Metadata.GetIndexes().Select(x=>(Index:x,Name:x.GetDatabaseName())).ToArray();
            entity.ToTable(item.Name);
            if(key!=null&&keyName!=null)key.SetName(keyName);
            foreach(var index in indexes)if(index.Name!=null)index.Index.SetDatabaseName(index.Name);
        }
    }
}
public sealed class TableRetirementAuditMapping:IEntityTypeConfiguration<TableRetirementAudit>
{
    public void Configure(EntityTypeBuilder<TableRetirementAudit> b)
    {
        b.ToTable("SC_TableRetirementAudits",t=>t.UseSqlOutputClause(false));b.HasKey(x=>x.Id);
        b.Property(x=>x.MigrationId).HasMaxLength(150);b.Property(x=>x.OriginalTableName).HasMaxLength(128);b.Property(x=>x.Reason).HasMaxLength(500);
        b.HasIndex(x=>new{x.MigrationId,x.OriginalTableName}).IsUnique();
    }
}
