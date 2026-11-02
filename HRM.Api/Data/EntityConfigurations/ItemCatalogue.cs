using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class ItemCatalogueMapping : IEntityTypeConfiguration<ItemCategory>, IEntityTypeConfiguration<Item>, IEntityTypeConfiguration<ItemUnit>, IEntityTypeConfiguration<ItemStaffRequest>, IEntityTypeConfiguration<ItemStaffRequestLine>
{
    public void Configure(EntityTypeBuilder<ItemCategory> b)
    {
        b.ToTable("IM_ItemCategories");
        b.HasKey(x => x.ItemCategoryId);
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.Name }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.ToTable("IM_Items", t => t.HasCheckConstraint("CK_IM_Items_Kind", "[Kind] IN ('Asset','Consumable') AND [QuantityOnHand] >= 0 AND [ReorderLevel] >= 0"));
        b.HasKey(x => x.ItemId);
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Kind).HasMaxLength(20);
        b.Property(x => x.UnitOfMeasure).HasMaxLength(50);
        b.Property(x => x.Brand).HasMaxLength(120);
        b.Property(x => x.Model).HasMaxLength(120);
        b.HasIndex(x => new { x.OrganisationId, x.ItemCategoryId, x.Name }).IsUnique();
        b.HasOne(x => x.Category).WithMany(x => x.Items).HasForeignKey(x => x.ItemCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ItemUnit> b)
    {
        b.ToTable("IM_ItemUnits", t => t.HasCheckConstraint("CK_IM_ItemUnits_Status",
            "[Status] IN ('Available','Issued','Returned','Damaged','Lost','Retired') AND [Condition] IN ('Good','Fair','Damaged','Unusable')"));
        b.HasKey(x => x.ItemUnitId);
        b.Property(x => x.InventoryNumber).HasMaxLength(100);
        b.Property(x => x.SerialNumber).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Condition).HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.InventoryNumber }).IsUnique();
        b.HasIndex(x => new { x.ItemId, x.Status });
        b.HasOne(x => x.Item).WithMany(x => x.Units).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IssuedToIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ItemStaffRequest> b)
    {
        b.ToTable("IM_ItemStaffRequests", t => t.HasCheckConstraint("CK_IM_ItemStaffRequests_Values",
            "[Quantity] > 0 AND [Purpose] IN ('Job related','Common') AND [Kind] IN ('Issue','Requisition')"));
        b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.Purpose).HasMaxLength(20);
        b.Property(x => x.Kind).HasMaxLength(20);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId, x.CreatedAtUtc });
        b.HasOne(x => x.Request).WithOne().HasForeignKey<ItemStaffRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ItemUnit).WithMany().HasForeignKey(x => x.ItemUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ItemStaffRequestLine> b)
    {
        b.ToTable("IM_ItemStaffRequestLines", t => t.HasCheckConstraint("CK_IM_ItemStaffRequestLines_Quantity", "[Quantity] > 0"));
        b.HasKey(x => x.ItemStaffRequestLineId);
        b.Property(x => x.ItemName).HasMaxLength(200);
        b.HasIndex(x => new { x.RequestId, x.ItemStaffRequestLineId });
        b.HasOne(x => x.Request).WithMany(x => x.Lines).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemUnit> ItemUnits => Set<ItemUnit>();
    public DbSet<ItemStaffRequest> ItemStaffRequests => Set<ItemStaffRequest>();
    public DbSet<ItemStaffRequestLine> ItemStaffRequestLines => Set<ItemStaffRequestLine>();
}
