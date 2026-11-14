using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffBankAndEmergencyMapping :
    IEntityTypeConfiguration<StaffBankAccount>,
    IEntityTypeConfiguration<StaffEmergencyContact>
{
    public void Configure(EntityTypeBuilder<StaffBankAccount> b)
    {
        b.ToTable("BK_StaffBankAccounts");
        b.HasKey(x => x.Id).HasName("PK_BK_StaffBankAccounts");
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.BankName).HasMaxLength(150).IsRequired();
        b.Property(x => x.BankCode).HasMaxLength(50);
        b.Property(x => x.BranchName).HasMaxLength(150);
        b.Property(x => x.AccountHolderName).HasMaxLength(200).IsRequired();
        b.Property(x => x.AccountNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("MVR");
        b.Property(x => x.IsPrimary).HasDefaultValue(false);
        b.Property(x => x.Remarks).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Individual)
            .WithMany()
            .HasForeignKey(x => x.IndividualId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_BK_StaffBankAccounts_Individual");

        b.HasIndex(x => x.IndividualId).HasDatabaseName("IX_BK_StaffBankAccounts_IndividualId");
        b.HasIndex(x => new { x.IndividualId, x.IsPrimary }).HasDatabaseName("IX_BK_StaffBankAccounts_IsPrimary");
    }

    public void Configure(EntityTypeBuilder<StaffEmergencyContact> b)
    {
        b.ToTable("EC_StaffEmergencyContacts");
        b.HasKey(x => x.Id).HasName("PK_EC_StaffEmergencyContacts");
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Relationship).HasMaxLength(100).IsRequired();
        b.Property(x => x.PrimaryPhone).HasMaxLength(50).IsRequired();
        b.Property(x => x.SecondaryPhone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.IsPrimary).HasDefaultValue(false);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Individual)
            .WithMany()
            .HasForeignKey(x => x.IndividualId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_EC_StaffEmergencyContacts_Individual");

        b.HasIndex(x => x.IndividualId).HasDatabaseName("IX_EC_StaffEmergencyContacts_IndividualId");
    }
}
