using System.Security.Cryptography.X509Certificates;
namespace HRM.Services.EdgeAttendance;
/// <summary>Production PKI provider replaces this registration. It intentionally blocks all client certificates.</summary>
public sealed class FailClosedCertificateTrustValidator : IEdgeCertificateTrustValidator
{ public Task ValidateAsync(X509Certificate2 certificate,CancellationToken ct=default)=>throw new UnauthorizedAccessException("No trusted Edge Agent PKI issuer is configured."); }
