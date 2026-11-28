using HRM.Models.Recruitment;using Microsoft.EntityFrameworkCore;namespace HRM.Models;public partial class HrmTeContext{public DbSet<RecruitmentOffer> RecruitmentOffers=>Set<RecruitmentOffer>();}
