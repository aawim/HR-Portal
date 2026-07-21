using HRM.Services.Interfaces;
using HRM.Services.Repositories;
using HRM.Models;

namespace HRM.Services
{
    public class UserAppService
    {
        private readonly IUserContext _context;
        private readonly UserRepository _repo;
        private readonly IUserAccessService _userAccessService;
  

        public UserAppService(IUserContext context, UserRepository repo, IUserAccessService userAccessService)
        {
            _context = context;
            _repo = repo;
            _userAccessService = userAccessService;
 
        }

        public async Task<List<JobLeaveType>> GetJobLeaveTypesAsync()
        {
            var context = await _userAccessService.RequireContextAsync();
            return await _repo.GetJobLeaveTypes(context.ActiveJob.JobID);
        }


        public async Task<User?> GetMyProfileAsync()
        {
            var username = await _context.GetUsernameAsync();
            return await _repo.GetProfileAsync(username!);
        }
    }
}
