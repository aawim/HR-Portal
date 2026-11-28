using HRM.Models.Signatures;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<UserSignature> UserSignatures => Set<UserSignature>();
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();
}
