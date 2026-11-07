using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<CertificateRequest> CertificateRequests => Set<CertificateRequest>();
    public DbSet<IssuedCertificate> IssuedCertificates => Set<IssuedCertificate>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<CertificateAuditLog> CertificateAuditLogs => Set<CertificateAuditLog>();
}
