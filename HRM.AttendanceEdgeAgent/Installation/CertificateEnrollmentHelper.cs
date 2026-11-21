using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HRM.AttendanceEdgeAgent.Installation;

public static class CertificateEnrollmentHelper
{
    public static async Task GenerateEnrollmentArtifactsAsync(string agentId, string pfxPath, string? password)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"Generating mTLS Machine Identity for Agent: {agentId}");
        Console.WriteLine("==================================================");

        var dir = Path.GetDirectoryName(pfxPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 1. Generate Root CA
        using var caRsa = RSA.Create(2048);
        var caReq = new CertificateRequest(
            "CN=DHR Edge Authority Root CA, O=HRM Attendance Edge",
            caRsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        caReq.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        caReq.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));

        var caCert = caReq.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(10));

        // 2. Generate Agent Client Certificate signed by Root CA
        using var clientRsa = RSA.Create(2048);
        var clientReq = new CertificateRequest(
            $"CN={agentId}, O=HRM Attendance Edge",
            clientRsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        clientReq.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: false));
        clientReq.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        clientReq.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.2") }, critical: false)); // Client Authentication

        var serialNumber = new byte[8];
        RandomNumberGenerator.Fill(serialNumber);
        var clientCert = clientReq.Create(
            caCert,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(2),
            serialNumber);

        // Associate private key with client cert
        var clientWithKey = clientCert.CopyWithPrivateKey(clientRsa);

        // Export PFX containing client cert and CA
        var collection = new X509Certificate2Collection();
        collection.Add(clientWithKey);
        collection.Add(caCert);

        var pfxBytes = collection.Export(X509ContentType.Pfx, password);
        if (pfxBytes != null)
        {
            await File.WriteAllBytesAsync(pfxPath, pfxBytes);
        }

        var caRawBase64 = Convert.ToBase64String(caCert.RawData);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"PFX Certificate saved to: {pfxPath}");
        Console.ResetColor();
        Console.WriteLine($"Client Thumbprint:        {clientWithKey.Thumbprint}");
        Console.WriteLine($"Client Expiry:            {clientWithKey.NotAfter:u}");
        Console.WriteLine($"Issuer:                   {caCert.Subject}");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("Step 1: In the DHR Portal (/admin/settings/edge-agents), add this under 'Trusted PKI roots':");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(caRawBase64);
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Step 2: Enrol this agent in the DHR portal with:");
        Console.WriteLine($"  - Agent identifier:     {agentId}");
        Console.WriteLine($"  - Certificate thumbprint: {clientWithKey.Thumbprint}");
        Console.WriteLine($"  - Certificate issuer:     {caCert.Subject}");
        Console.WriteLine($"  - Certificate expiry:     {clientWithKey.NotAfter:yyyy-MM-dd}");
        Console.WriteLine("==================================================");
    }
}
