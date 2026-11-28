using HRM.Models.Recruitment;
using Microsoft.EntityFrameworkCore;
namespace HRM.Models;
public partial class HrmTeContext { public DbSet<RecruitmentRequisition> RecruitmentRequisitions=>Set<RecruitmentRequisition>(); public DbSet<RecruitmentPositionReservation> RecruitmentPositionReservations=>Set<RecruitmentPositionReservation>(); public DbSet<RecruitmentVacancy> RecruitmentVacancies=>Set<RecruitmentVacancy>(); }
