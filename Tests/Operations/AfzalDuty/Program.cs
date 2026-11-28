using HRM.Models;
using Microsoft.EntityFrameworkCore;

if (args.Length != 4 || args[0] is not ("--inspect" or "--remove") || !int.TryParse(args[2], out var jobId) || !DateTime.TryParse(args[3], out var day))
    throw new InvalidOperationException("Usage: --inspect|--remove <template-code> <job-id> <yyyy-MM-dd>");

var options = new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer("Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=NewHRM_DB;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=10").Options;
await using var db = new HrmTeContext(options);
var template = await db.WorkTemplates.SingleOrDefaultAsync(x => x.Code == args[1]);
if (template is null) { Console.WriteLine("No matching duty template exists."); return; }
var assignments = await db.JobWorkTemplateAssignments.Where(x => x.JobID == jobId && x.WorkTemplateID == template.WorkTemplateId).ToListAsync();
var plans = await db.WorkPlans.Include(x => x.WorkPlanSegments).Where(x => x.JobId == jobId && x.WorkDate.Date == day.Date).ToListAsync();
var evidence = plans.SelectMany(x => x.WorkPlanSegments).Any(x => x.AttendanceId.HasValue || x.IsCompleted);
Console.WriteLine($"Template={template.WorkTemplateId}; assignments={assignments.Count}; plans={plans.Count}; completed-evidence={evidence}");
if (args[0] == "--inspect") return;
if (evidence) throw new InvalidOperationException("Duty source can be removed, but historical completed attendance evidence prevents changing the work-plan snapshot.");
db.JobWorkTemplateAssignments.RemoveRange(assignments);
template.IsActive = false;
await db.SaveChangesAsync();
Console.WriteLine("Duty source removed and template deactivated. No historical attendance evidence was changed.");
