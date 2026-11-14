using Microsoft.EntityFrameworkCore;
using HRM.Models;

namespace HRM.Models;

public partial class HrmTeContext
{
    public virtual DbSet<StaffBankAccount> StaffBankAccounts { get; set; } = null!;
    public virtual DbSet<StaffEmergencyContact> StaffEmergencyContacts { get; set; } = null!;
}
