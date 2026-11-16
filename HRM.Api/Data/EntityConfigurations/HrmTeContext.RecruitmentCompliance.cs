using HRM.Models.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<RecruitmentPolicyProfile> RecruitmentPolicyProfiles => Set<RecruitmentPolicyProfile>();
    public DbSet<RecruitmentPolicyVersion> RecruitmentPolicyVersions => Set<RecruitmentPolicyVersion>();
    public DbSet<RecruitmentComplianceRule> RecruitmentComplianceRules => Set<RecruitmentComplianceRule>();
    public DbSet<RecruitmentPolicyAssignment> RecruitmentPolicyAssignments => Set<RecruitmentPolicyAssignment>();
}
