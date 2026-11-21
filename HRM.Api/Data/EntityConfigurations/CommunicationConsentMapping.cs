using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class CommunicationConsentMapping : IEntityTypeConfiguration<CommunicationConsent>
{
    public void Configure(EntityTypeBuilder<CommunicationConsent> b)
    {
        b.ToTable("CM_CommunicationConsents"); b.HasKey(x => x.CommunicationConsentId);
        b.Property(x => x.Source).HasMaxLength(400).IsRequired(); b.Property(x => x.Purpose).HasMaxLength(40).IsRequired();
        b.HasOne<CommunicationDestination>().WithMany().HasForeignKey(x => x.CommunicationDestinationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CommunicationDestinationId, x.IndividualId, x.Purpose, x.RevokedAtUtc }).HasDatabaseName("IX_CM_Consents_DestinationIndividualPurposeRevoked");
    }
}
