using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Api.Data.EntityConfigurations;

public sealed class StaffAccessPolicyMapping : IEntityTypeConfiguration<StaffAccessPolicy>
{
    public void Configure(EntityTypeBuilder<StaffAccessPolicy> b)
    {
        b.ToTable("AC_StaffAccessPolicies");
        b.HasKey(x => x.StaffAccessPolicyId);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.PositionClassificationId }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PositionClassification>().WithMany().HasForeignKey(x => x.PositionClassificationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StaffAccessScopeMapping : IEntityTypeConfiguration<StaffAccessScope>
{
    public void Configure(EntityTypeBuilder<StaffAccessScope> b)
    {
        b.ToTable("AC_StaffAccessScopes", t => t.HasCheckConstraint("CK_AC_StaffAccessScopes_Subject",
            "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)"));
        b.HasKey(x => x.StaffAccessScopeId);
        b.HasIndex(x => new { x.OrganisationId, x.UserId });
        b.HasIndex(x => new { x.OrganisationId, x.UserGroupId });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserGroup>().WithMany().HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StaffAccessGrantMapping : IEntityTypeConfiguration<StaffAccessGrant>
{
    public void Configure(EntityTypeBuilder<StaffAccessGrant> b)
    {
        b.ToTable("AC_StaffAccessGrants", t =>
        {
            t.HasCheckConstraint("CK_AC_StaffAccessGrants_Subject",
                "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)");
            t.HasCheckConstraint("CK_AC_StaffAccessGrants_Resource",
                "([TargetIndividualId] IS NOT NULL AND [PositionClassificationId] IS NULL) OR ([TargetIndividualId] IS NULL AND [PositionClassificationId] IS NOT NULL)");
        });
        b.HasKey(x => x.StaffAccessGrantId);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.UserId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.UserGroupId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.TargetIndividualId, x.IsActive });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserGroup>().WithMany().HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.TargetIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PositionClassification>().WithMany().HasForeignKey(x => x.PositionClassificationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserAccessScopeMapping : IEntityTypeConfiguration<UserAccessScope>
{
    public void Configure(EntityTypeBuilder<UserAccessScope> b)
    {
        b.ToTable("AC_UserAccessScopes", t =>
        {
            t.HasCheckConstraint("CK_AC_UserAccessScopes_Subject",
                "([UserId] IS NOT NULL AND [UserGroupId] IS NULL) OR ([UserId] IS NULL AND [UserGroupId] IS NOT NULL)");
            t.HasCheckConstraint("CK_AC_UserAccessScopes_ScopeType",
                "[ScopeType] IN ('Self', 'Team', 'Department', 'Division', 'DivisionAndDescendants', 'Organisation', 'Custom')");
        });
        b.HasKey(x => x.UserAccessScopeId);
        b.Property(x => x.ScopeType).HasMaxLength(50).IsRequired();
        b.Property(x => x.PermissionFilter).HasMaxLength(200);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.RevocationReason).HasMaxLength(1000);
        b.HasIndex(x => new { x.OrganisationId, x.UserId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.UserGroupId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.UnitId, x.IsActive });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserGroup>().WithMany().HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrganisationStructure>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserAccessScopeAuditMapping : IEntityTypeConfiguration<UserAccessScopeAudit>
{
    public void Configure(EntityTypeBuilder<UserAccessScopeAudit> b)
    {
        b.ToTable("AC_UserAccessScopeAudits");
        b.HasKey(x => x.UserAccessScopeAuditId);
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.ScopeType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Detail).HasMaxLength(2000);
        b.HasIndex(x => new { x.OrganisationId, x.TimestampUtc });
        b.HasIndex(x => new { x.TargetUserId, x.TimestampUtc });
        b.HasOne<UserAccessScope>().WithMany().HasForeignKey(x => x.UserAccessScopeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.TargetUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserGroup>().WithMany().HasForeignKey(x => x.TargetUserGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrganisationStructure>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

