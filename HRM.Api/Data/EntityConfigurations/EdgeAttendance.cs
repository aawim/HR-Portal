using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.EdgeAttendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Api.Data.EntityConfigurations;

public sealed class EdgeAttendanceMapping :
    IEntityTypeConfiguration<EdgeAttendanceAgent>,
    IEntityTypeConfiguration<EdgeAttendanceTrustedIssuer>,
    IEntityTypeConfiguration<EdgeAttendanceAgentDevice>,
    IEntityTypeConfiguration<EdgeAttendanceEventReceipt>,
    IEntityTypeConfiguration<EdgeAttendanceIngestionFailure>,
    IEntityTypeConfiguration<EdgeAttendanceActivationCode>,
    IEntityTypeConfiguration<EdgeAttendanceConnectorCommand>
{
    public void Configure(EntityTypeBuilder<EdgeAttendanceAgent> b)
    {
        b.ToTable("EA_Agents");
        b.HasKey(x => x.EdgeAttendanceAgentId);
        b.Property(x => x.AgentIdentifier).HasMaxLength(100);
        b.Property(x => x.CertificateThumbprint).HasMaxLength(128);
        b.Property(x => x.CertificateIssuer).HasMaxLength(500);
        b.Property(x => x.SiteName).HasMaxLength(200);
        b.Property(x => x.ConnectorVersion).HasMaxLength(64);
        b.Property(x => x.OperatingSystem).HasMaxLength(128);
        b.Property(x => x.ConfigurationVersion).HasDefaultValue(1);
        b.Property(x => x.PendingEventCount).HasDefaultValue(0);
        b.Property(x => x.PublicKeyPem).HasMaxLength(4000);
        b.HasIndex(x => x.AgentIdentifier).IsUnique();
        b.HasIndex(x => x.CertificateThumbprint).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceTrustedIssuer> b)
    {
        b.ToTable("EA_TrustedIssuers");
        b.HasKey(x => x.EdgeAttendanceTrustedIssuerId);
        b.Property(x => x.Subject).HasMaxLength(500);
        b.Property(x => x.Thumbprint).HasMaxLength(128);
        b.Property(x => x.CertificateRawData).HasColumnType("varbinary(max)");
        b.HasIndex(x => x.Thumbprint).IsUnique();
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceAgentDevice> b)
    {
        b.ToTable("EA_AgentDevices");
        b.HasKey(x => x.EdgeAttendanceAgentDeviceId);
        b.Property(x => x.PollingIntervalSeconds).HasDefaultValue(30);
        b.HasIndex(x => new { x.EdgeAttendanceAgentId, x.AttendanceDeviceId }).IsUnique();
        b.HasOne<EdgeAttendanceAgent>().WithMany().HasForeignKey(x => x.EdgeAttendanceAgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceDevice>().WithMany().HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceEventReceipt> b)
    {
        b.ToTable("EA_EventReceipts");
        b.HasKey(x => x.EdgeAttendanceEventReceiptId);
        b.Property(x => x.DeviceEventId).HasMaxLength(200);
        b.Property(x => x.EventHash).HasMaxLength(128);
        b.Property(x => x.LocalUserId).HasMaxLength(100);
        b.Property(x => x.ProcessingStatus).HasMaxLength(30);
        b.HasIndex(x => new { x.AttendanceDeviceId, x.DeviceEventId }).IsUnique();
        b.HasIndex(x => x.EventHash).IsUnique();
        b.HasOne<EdgeAttendanceAgent>().WithMany().HasForeignKey(x => x.EdgeAttendanceAgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceDevice>().WithMany().HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.AttendanceLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceIngestionFailure> b)
    {
        b.ToTable("EA_IngestionFailures");
        b.HasKey(x => x.EdgeAttendanceIngestionFailureId);
        b.Property(x => x.FailureCode).HasMaxLength(80);
        b.Property(x => x.Detail).HasMaxLength(2000);
        b.HasIndex(x => new { x.EdgeAttendanceAgentId, x.OccurredAtUtc });
        b.HasOne<EdgeAttendanceAgent>().WithMany().HasForeignKey(x => x.EdgeAttendanceAgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceDevice>().WithMany().HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceActivationCode> b)
    {
        b.ToTable("EA_ActivationCodes");
        b.HasKey(x => x.EdgeAttendanceActivationCodeId);
        b.Property(x => x.CodeHash).HasMaxLength(128);
        b.Property(x => x.CodePrefix).HasMaxLength(32);
        b.Property(x => x.SiteName).HasMaxLength(200);
        b.Property(x => x.SuggestedConnectorName).HasMaxLength(100);
        b.Property(x => x.DeviceIdsJson).HasMaxLength(2000);
        b.HasIndex(x => x.CodeHash).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.ExpiresAtUtc });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<EdgeAttendanceAgent>().WithMany().HasForeignKey(x => x.UsedByEdgeAttendanceAgentId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<EdgeAttendanceConnectorCommand> b)
    {
        b.ToTable("EA_ConnectorCommands");
        b.HasKey(x => x.EdgeAttendanceConnectorCommandId);
        b.Property(x => x.CommandType).HasMaxLength(80);
        b.Property(x => x.PayloadJson).HasMaxLength(4000);
        b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.ResultJson).HasMaxLength(4000);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.HasIndex(x => new { x.EdgeAttendanceAgentId, x.Status, x.CreatedAtUtc });
        b.HasOne<EdgeAttendanceAgent>().WithMany().HasForeignKey(x => x.EdgeAttendanceAgentId).OnDelete(DeleteBehavior.Restrict);
    }
}
