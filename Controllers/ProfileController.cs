using Microsoft.AspNetCore.Mvc;
using HRM.Models;

namespace HRM.Controllers
{
    public class ProfileController : Controller
    {
        // GET: /Profile/Index
        public IActionResult Index()
        {
            // In a real app, you would fetch this from your database using the logged-in user's ID
            var model = new EmployeeProfileViewModel
            {
                FullName = "Jan Doe",
                EmployeeNumber = "12544",
                SAPNumber = "02145",
                NidPassport = "AA00000",
                Address = "123, Nirolhu, Binmatheege",
                Island = "My Island",
                Atoll = "My Atoll",
                Country = "My Country",
                ProfileImageUrl = "https://i.pravatar.cc/150?img=11",
                Email = "aaa@mail.com",
                Mobile = "4455777",
                Phone = "3322114",
                EmergencyName = "Marry Mosi",
                EmergencyRelation = "Wife",
                EmergencyEmail = "wife@mail.com",
                EmergencyMobile = "7787887",
                Organisation = "MNPS",
                Division = "Development",
                EmployeeType = "Contract",
                JobState = "Approved",
                ActiveDate = new DateTime(2026, 6, 1),
                BasicSalary = 9180.00m
            };

            return View(model);
        }
    }
}
