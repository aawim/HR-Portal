using Microsoft.EntityFrameworkCore;
using HRM.Models;
using System.Threading.Tasks;
namespace HRM.Services
{
    public class EmployeeService
    {
        private readonly HrmTeContext _context;
        private readonly UserSettingsService? _accounts;

        public EmployeeService(HrmTeContext context, UserSettingsService? accounts = null)
        {
            _context = context;
            _accounts = accounts;
        }
 
        // 1. Fetch Profile
        public async Task<User?> GetEmployeeProfileAsync(string identifier)
        {
            // Assuming your table is called "Employees"
            // Adjust the query based on how your eFaas ID maps to your database
            return await _context.Users
                .FirstOrDefaultAsync(e => e.Username == identifier || e.Email == identifier);
        }

        // 2. Update Profile
        public async Task<bool> UpdateEmployeeAsync(User updatedEmployee)
        {
            if (_accounts == null) throw new InvalidOperationException("Use Manage Account to update accounts.");
            return (await _accounts.SaveLegacyUserAsync(updatedEmployee)).Success;
        }
    }
}
