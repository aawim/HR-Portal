using System.Security.Cryptography;
using System.Text;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Infrastructure.Security;

/// <summary>
/// OS-protected secret storage:
/// - Windows: DPAPI (ProtectedData.Protect) with machine-scoped entropy.
/// - Linux / macOS: AES-256-GCM encrypted secret files with a 256-bit local master key stored with chmod 600.
/// Private keys are never stored in plain JSON or source code.
/// </summary>
public sealed class OsProtectedSecretStore : ISecretStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DHR.AttendanceConnector.SecretStore.v1");
    private readonly string _secretsDir;

    public OsProtectedSecretStore(IOptions<ConnectorOptions> options)
    {
        _secretsDir = Path.Combine(options.Value.ResolveDataDirectory(), "secrets");
        Directory.CreateDirectory(_secretsDir);
        RestrictDirectoryPermissions(_secretsDir);
    }

    public async Task SaveSecretAsync(string key, byte[] plaintext, CancellationToken ct = default)
    {
        var path = GetSecretPath(key);
        byte[] protectedBytes;

        if (OperatingSystem.IsWindows())
        {
            protectedBytes = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
        }
        else
        {
            var masterKey = await GetOrCreateUnixMasterKeyAsync(ct);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var ciphertext = new byte[plaintext.Length];
            using var aes = new AesGcm(masterKey, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, Entropy);

            protectedBytes = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, protectedBytes, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, protectedBytes, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, protectedBytes, nonce.Length + tag.Length, ciphertext.Length);
        }

        await File.WriteAllBytesAsync(path, protectedBytes, ct);
        RestrictFilePermissions(path);
    }

    public async Task<byte[]?> LoadSecretAsync(string key, CancellationToken ct = default)
    {
        var path = GetSecretPath(key);
        if (!File.Exists(path)) return null;

        var protectedBytes = await File.ReadAllBytesAsync(path, ct);
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        }

        if (protectedBytes.Length < 28)
            throw new CryptographicException("Corrupted encrypted secret payload.");

        var masterKey = await GetOrCreateUnixMasterKeyAsync(ct);
        return DecryptAesGcm(masterKey, protectedBytes);
    }

    private static byte[] DecryptAesGcm(byte[] masterKey, byte[] protectedBytes)
    {
        var nonce = protectedBytes.AsSpan(0, 12);
        var tag = protectedBytes.AsSpan(12, 16);
        var ciphertext = protectedBytes.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(masterKey, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, Entropy);
        return plaintext;
    }

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        var path = GetSecretPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(GetSecretPath(key)));

    private string GetSecretPath(string key)
    {
        var safe = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim())));
        return Path.Combine(_secretsDir, $"{safe}.bin");
    }

    private async Task<byte[]> GetOrCreateUnixMasterKeyAsync(CancellationToken ct)
    {
        var masterPath = Path.Combine(_secretsDir, ".master.key");
        if (File.Exists(masterPath))
            return await File.ReadAllBytesAsync(masterPath, ct);

        var key = RandomNumberGenerator.GetBytes(32);
        await File.WriteAllBytesAsync(masterPath, key, ct);
        RestrictFilePermissions(masterPath);
        return key;
    }

    private static void RestrictDirectoryPermissions(string dir)
    {
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
            catch { /* ignore on unsupported FS */ }
        }
    }

    private static void RestrictFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch { /* ignore on unsupported FS */ }
        }
    }
}
