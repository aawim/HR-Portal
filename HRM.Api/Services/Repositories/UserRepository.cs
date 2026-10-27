using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Repositories
{
    public class UserRepository
    {
        private readonly IDbContextFactory<HrmTeContext> _factory;

        public UserRepository(IDbContextFactory<HrmTeContext> factory)
        {
            _factory = factory;
        }

        public async Task<User?> GetProfileAsync(string username)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Users
                .Include(u => u.Jobs)
                .ThenInclude(j => j.Leaves)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<JobLeaveType>> GetLeaveBalances(int jobId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.JobLeaveTypes
                .Where(x => x.JobId == jobId && x.IsValid)
                .Include(x => x.LeaveType)
                .ToListAsync();
        }

        public async Task<List<JobLeaveType>> GetJobLeaveTypes(int jobId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.JobLeaveTypes
                .Where(x => x.JobId == jobId && x.IsValid)
                .Include(x => x.LeaveType)
                .ToListAsync();
        }
    }
}
