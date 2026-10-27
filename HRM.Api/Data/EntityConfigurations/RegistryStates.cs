using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class VerifiedStateMapping:IEntityTypeConfiguration<VerifiedState>
{
    public void Configure(EntityTypeBuilder<VerifiedState> b)
    {b.ToTable("BE_VerifiedStates");b.HasKey(x=>x.VerifiedStateId).HasName("PK_VerifiedStates");b.Property(x=>x.VerifiedStateId).HasColumnName("VerifiedStateID");b.Property(x=>x.StateName).HasMaxLength(250).IsUnicode(false);}
}

public sealed class BusinessEntityStateMapping:IEntityTypeConfiguration<BusinessEntityState>
{
    public void Configure(EntityTypeBuilder<BusinessEntityState> b)
    {b.ToTable("BE_BusinessEntityStates");b.HasKey(x=>x.BusinessEntityStateId).HasName("PK_BusinessEntityStates");b.Property(x=>x.BusinessEntityStateId).HasColumnName("BusinessEntityStateID");b.Property(x=>x.StateName).HasMaxLength(250).IsUnicode(false);}
}

public partial class HrmTeContext
{
    public DbSet<VerifiedState> VerifiedStates=>Set<VerifiedState>();
    public DbSet<BusinessEntityState> BusinessEntityStates=>Set<BusinessEntityState>();
}
