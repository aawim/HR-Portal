using Microsoft.EntityFrameworkCore;
using HRM.Models;
using System.Threading.Tasks;
namespace HRM.Services
{
    public class EmployeeService
    {
        private readonly HrmTeContext _context;

        public EmployeeService(HrmTeContext context)
        {
            _context = context;
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
            _context.Users.Update(updatedEmployee);
            var saved = await _context.SaveChangesAsync();
            return saved > 0;
        }
    }
}
