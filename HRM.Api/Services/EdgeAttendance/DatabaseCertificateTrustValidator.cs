using System.Security.Cryptography.X509Certificates;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.EdgeAttendance;
public sealed class DatabaseCertificateTrustValidator(IDbContextFactory<HrmTeContext> factory) : IEdgeCertificateTrustValidator
{
 public async Task ValidateAsync(X509Certificate2 certificate,CancellationToken ct=default)
 { if(certificate.NotBefore.ToUniversalTime()>DateTime.UtcNow||certificate.NotAfter.ToUniversalTime()<=DateTime.UtcNow)throw new UnauthorizedAccessException("Client certificate is outside its validity period.");await using var db=await factory.CreateDbContextAsync(ct);var issuers=await db.EdgeAttendanceTrustedIssuers.AsNoTracking().Where(x=>x.IsActive&&x.CertificateRawData!=null).Select(x=>x.CertificateRawData!).ToListAsync(ct);if(issuers.Count==0)throw new UnauthorizedAccessException("No trusted Edge Agent CA root is configured.");using var chain=new X509Chain();chain.ChainPolicy.TrustMode=X509ChainTrustMode.CustomRootTrust;chain.ChainPolicy.RevocationMode=X509RevocationMode.NoCheck;chain.ChainPolicy.DisableCertificateDownloads=true;foreach(var raw in issuers)chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(raw));if(!chain.Build(certificate))throw new UnauthorizedAccessException("Client certificate chain is not trusted."); }
}
