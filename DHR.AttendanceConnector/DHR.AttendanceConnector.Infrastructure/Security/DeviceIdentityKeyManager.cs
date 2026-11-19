using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DHR.AttendanceConnector.Core.Interfaces;

namespace DHR.AttendanceConnector.Infrastructure.Security;

public sealed class DeviceIdentityKeyManager(ISecretStore secretStore) : IDeviceIdentityKeyManager
{
    private const string ActivePrivateKeyName = "connector.active.key.pem";
    private const string PendingPrivateKeyName = "connector.pending.key.pem";
    private const string ActiveClientCertName = "connector.active.cert.pem";
    private const string ActiveCaCertName = "connector.active.ca.pem";

    public async Task<(string PublicKeyPem, string CsrPem, string ProofSignatureBase64, DateTime ProofTimestampUtc)> GenerateEnrollmentKeyAndProofAsync(
        string activationCode,
        string connectorName,
        CancellationToken ct = default)
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var csrReq = new CertificateRequest(
            $"CN={SanitizeCn(connectorName)}, O=DHR Attendance Connector",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var csrPem = csrReq.CreateSigningRequestPem();

        var proofTimestamp = DateTime.UtcNow;
        var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(activationCode.Trim().ToUpperInvariant())));
        var canonical = $"{codeHash}|{connectorName.Trim()}|{proofTimestamp:O}";
        var sig = rsa.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        await secretStore.SaveSecretAsync(PendingPrivateKeyName, Encoding.UTF8.GetBytes(privatePem), ct);
        return (publicPem, csrPem, Convert.ToBase64String(sig), proofTimestamp);
    }

    public async Task<(string NewPublicKeyPem, string NewCsrPem, string ProofSignatureBase64, DateTime ProofTimestampUtc)> GenerateRenewalKeyAndProofAsync(
        string agentId,
        CancellationToken ct = default)
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var csrReq = new CertificateRequest(
            $"CN={SanitizeCn(agentId)}, O=DHR Attendance Connector",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var csrPem = csrReq.CreateSigningRequestPem();

        var proofTimestamp = DateTime.UtcNow;
        var canonical = $"RENEW|{agentId.Trim()}|{proofTimestamp:O}";
        var sig = rsa.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        await secretStore.SaveSecretAsync(PendingPrivateKeyName, Encoding.UTF8.GetBytes(privatePem), ct);
        return (publicPem, csrPem, Convert.ToBase64String(sig), proofTimestamp);
    }

    public async Task CommitIssuedCertificateAsync(string clientCertPem, string caCertPem, bool isRenewal, CancellationToken ct = default)
    {
        var pendingKey = await secretStore.LoadSecretAsync(PendingPrivateKeyName, ct)
            ?? throw new InvalidOperationException("No pending device private key was found to bind to the issued certificate.");

        await secretStore.SaveSecretAsync(ActivePrivateKeyName, pendingKey, ct);
        await secretStore.SaveSecretAsync(ActiveClientCertName, Encoding.UTF8.GetBytes(clientCertPem.Trim()), ct);
        await secretStore.SaveSecretAsync(ActiveCaCertName, Encoding.UTF8.GetBytes(caCertPem.Trim()), ct);
        await secretStore.DeleteSecretAsync(PendingPrivateKeyName, ct);
    }

    public async Task<X509Certificate2?> LoadActiveClientCertificateAsync(CancellationToken ct = default)
    {
        var certBytes = await secretStore.LoadSecretAsync(ActiveClientCertName, ct);
        var keyBytes = await secretStore.LoadSecretAsync(ActivePrivateKeyName, ct);
        if (certBytes is null || keyBytes is null) return null;

        var certPem = Encoding.UTF8.GetString(certBytes);
        var keyPem = Encoding.UTF8.GetString(keyBytes);

        using var ephemeral = X509Certificate2.CreateFromPem(certPem, keyPem);
        // Re-import via PFX on Windows so SChannel can access the private key during mTLS handshake
        var pfx = ephemeral.Export(X509ContentType.Pfx);
        // Windows SChannel requires PersistKeySet (EphemeralKeySet fails mTLS handshake with SEC_E_UNKNOWN_CREDENTIALS)
        var flags = OperatingSystem.IsWindows()
            ? X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet
            : X509KeyStorageFlags.EphemeralKeySet;
        return new X509Certificate2(pfx, (string?)null, flags);
    }

    public async Task ClearCredentialsAsync(CancellationToken ct = default)
    {
        await secretStore.DeleteSecretAsync(ActivePrivateKeyName, ct);
        await secretStore.DeleteSecretAsync(PendingPrivateKeyName, ct);
        await secretStore.DeleteSecretAsync(ActiveClientCertName, ct);
        await secretStore.DeleteSecretAsync(ActiveCaCertName, ct);
    }

    private static string SanitizeCn(string value)
    {
        var clean = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "dhr-connector" : clean;
    }
}
