using HRM.Services.Interfaces;

namespace HRM.Services
{
    public class JobStore : IJobStore
    {
        private int? _jobId;

        public int? CurrentJobId => _jobId;

        public Task<int> GetJobID()
        {
            throw new NotImplementedException();
        }

        public void SetJobId(int jobId)
        {
            _jobId = jobId;
        }
    }
}
