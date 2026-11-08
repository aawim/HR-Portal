using System.Security.Cryptography;
using System.Text;
namespace HRM.Api.Authentication;

/// <summary>Public fingerprints only. The previously exposed credential itself is never stored here.</summary>
public static class CredentialExposure
{
    private static readonly string[] RevokedFingerprints = ["7244FE85125FDDD0DA29F38A082DC7329226331C0A8C9E9A7691A239FA9E9616"];
    public static bool IsExposed(string? secret) => Matches(secret, RevokedFingerprints);
    public static bool Matches(string? secret, IEnumerable<string> fingerprints) => !string.IsNullOrEmpty(secret) &&
        fingerprints.Contains(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))), StringComparer.OrdinalIgnoreCase);
}
