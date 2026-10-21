// One-off, explicitly user-authorised operation; not part of the test harness.
// Ahmed Afzal, NCIT job 906, 21 October 2026, duty 10:00–12:00 only.
using System.Data;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Services;
using Microsoft.EntityFrameworkCore;

var day = new DateTime(2026, 10, 21);
if (DateTime.UtcNow.AddHours(5).Date != day || args.Length != 1 || args[0] != "--apply-confirmed-duty")
    throw new InvalidOperationException("This operation requires --apply-confirmed-duty and is authorised for 21 October 2026 only.");
var factory = new ScopedFactory(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(
    "Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=NewHRM_DB;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=10").Options);
const string code = "DUTY-AFZAL-20261021-1000-1200";
await using (var db = factory.CreateDbContext())
{
    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    var user = await db.Users.SingleOrDefaultAsync(x => x.UserId == 425 && x.Username == "A302409" && x.BusinessEntityID == 26 && x.IsActive)
        ?? throw new InvalidOperationException("Ahmed Afzal's account identity did not match.");
    if (!await db.Jobs.AnyAsync(x => x.JobId == 906 && x.IndividualID == 26 && x.OrganisationID == 3 && x.JobStateId == 4 && x.TerminatedDate == null))
        throw new InvalidOperationException("The approved NCIT job did not match.");
    var official = await db.JobWorkTemplateAssignments.SingleAsync(x => x.JobID == 906 && x.SchedulePurpose == "Official" &&
        x.IsActive && x.EffectiveFrom <= day && (x.EffectiveTo == null || x.EffectiveTo >= day));
    if (official.WorkTemplateID != 1) throw new InvalidOperationException("The official schedule changed. Review before applying.");
    if (await db.WorkPlans.AnyAsync(x => x.JobId == 906 && x.WorkDate == day && x.IsValid && x.IsFinalized))
        throw new InvalidOperationException("Today's work plan is finalized; no changes were made.");
    var duties = await db.JobWorkTemplateAssignments.Include(x => x.WorkTemplate).Where(x => x.JobID == 906 &&
        x.SchedulePurpose == "Duty" && x.IsActive && x.EffectiveFrom <= day && (x.EffectiveTo == null || x.EffectiveTo >= day)).ToListAsync();
    if (duties.Any(x => x.WorkTemplate.Code != code || x.EffectiveFrom != day || x.EffectiveTo != day))
        throw new InvalidOperationException("A different duty already exists today. No existing schedule was replaced.");
    if (duties.Count == 0)
    {
        if (await db.WorkTemplates.AnyAsync(x => x.Code == code)) throw new InvalidOperationException("An unattached template already exists. Review before retrying.");
        var type = await db.WorkTemplateTypes.SingleAsync(x => x.Code == "DUTY" && x.IsActive);
        var input = await db.WorkSegmentTypes.SingleAsync(x => x.Code == "CHECK_IN" && x.IsActive);
        var output = await db.WorkSegmentTypes.SingleAsync(x => x.Code == "CHECK_OUT" && x.IsActive);
        var audit = new OperationLog { OperationLogActionId = 293, CreatedByContextId = 1, UpdatedContextId = 1,
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "",
            CreatedByUserOrganisationId = user.UserOrganisationId, UpdatedByUserOrganisationId = user.UserOrganisationId,
            Remarks = "User-authorised via Codex: Ahmed Afzal (User 425, Individual 26, NCIT job 906), duty 10:00–12:00 on 21 October 2026 only, mandatory check-in and check-out. Official schedule unchanged; no attendance punches created." };
        db.OperationLogs.Add(audit); await db.SaveChangesAsync();
        var template = new WorkTemplate { Code = code, Name = "Duty 10:00 - 12:00", WorkTemplateTypeId = type.WorkTemplateTypeId,
            OrganisationBusinessEntityId = 3, IsGlobal = false, IsActive = true, IsRepeatable = false,
            DefaultStartTime = new(10, 0), DefaultEndTime = new(12, 0), RequiresAttendance = true, RequiresCheckOut = true,
            OperationLogId = audit.OperationLogId };
        foreach (var (id, name, sequence, offset) in new[] { (input.WorkSegmentTypeId, "Check in", 1, 0), (output.WorkSegmentTypeId, "Check out", 2, 120) })
            template.WorkTemplateSegments.Add(new() { WorkSegmentTypeId = id, Name = name, SequenceNumber = sequence,
                OffsetMinutes = offset, DurationMinutes = 30, IsMandatory = true, RequiresAttendance = true, IsActive = true,
                IsPaid = true, OperationLogId = audit.OperationLogId, CreatedAt = DateTime.UtcNow });
        db.WorkTemplates.Add(template); await db.SaveChangesAsync();
        db.JobWorkTemplateAssignments.Add(new() { JobID = 906, WorkTemplateID = template.WorkTemplateId,
            SchedulePurpose = "Duty", EffectiveFrom = day, EffectiveTo = day, IsActive = true, CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();
        Console.WriteLine($"Attached duty template {template.WorkTemplateId}, audit {audit.OperationLogId}, for today only.");
    }
    else Console.WriteLine("Confirmed duty already attached; no duplicate created.");
    await tx.CommitAsync();
}
var generator = new WorkPlanGenerator(factory, new PlanningProviderResolver(factory));
var plan = await generator.GenerateOrGetAsync(26, 906, 3, DateOnly.FromDateTime(day))
    ?? throw new InvalidOperationException("No work plan was generated by the current planning provider.");
if (!plan.Assignments.Any(x => x.SchedulePurpose == "Official") || !plan.Assignments.Any(x => x.SchedulePurpose == "Duty" &&
    x.StartDateTime == day.AddHours(10) && x.EndDateTime == day.AddHours(12)))
    throw new InvalidOperationException("The work plan does not contain both expected schedules.");
Console.WriteLine($"Verified work plan {plan.WorkPlanId} contains official and duty schedules.");
foreach (var item in plan.Segments.Where(x => x.SchedulePurpose == "Duty").OrderBy(x => x.StartDateTime))
{
    if (!item.IsMandatory || !item.RequiresAttendance) throw new InvalidOperationException("Duty clock requirements were not saved.");
    Console.WriteLine($"{item.StartDateTime:HH:mm} · {item.Name} · required");
}
sealed class ScopedFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new ScopedContext(options);
}
sealed class ScopedContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder) { }
}
