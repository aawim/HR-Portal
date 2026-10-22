using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;
public sealed class StaffNotification
{
    public long StaffNotificationId { get; set; }
    public int UserId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public string Kind { get; set; } = "";
    public string Message { get; set; } = "";
    public string Url { get; set; } = "/online";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
public sealed class StaffNotificationMapping : IEntityTypeConfiguration<StaffNotification>
{
    public void Configure(EntityTypeBuilder<StaffNotification> b)
    {
        b.ToTable("SN_StaffNotifications"); b.HasKey(x => x.StaffNotificationId);
        b.Property(x => x.Kind).HasMaxLength(48); b.Property(x => x.Message).HasMaxLength(500); b.Property(x => x.Url).HasMaxLength(250);
        b.HasIndex(x => new { x.UserId, x.OrganisationId, x.ReadAtUtc, x.CreatedAtUtc });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
