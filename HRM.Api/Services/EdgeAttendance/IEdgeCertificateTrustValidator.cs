using System.Security.Cryptography.X509Certificates;
namespace HRM.Services.EdgeAttendance;
public interface IEdgeCertificateTrustValidator { Task ValidateAsync(X509Certificate2 certificate,CancellationToken ct=default); }
