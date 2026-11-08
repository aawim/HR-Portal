using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace HRM.Api.Authentication;

/// <summary>Only a protected opaque reference goes to the browser; claims and eFaas tokens stay server-side.</summary>
public sealed class ProtectedTicketStore(IDistributedCache cache, IDataProtectionProvider protection, TimeProvider clock) : ITicketStore
{
    private const string Prefix = "HRM.Session.";
    private static string RevokedKey(string key) => key + ".revoked";
    private readonly IDataProtector protector = protection.CreateProtector("HRM.Api.Authentication.Tickets.v1");
    private static bool Valid(string key) => key.StartsWith(Prefix, StringComparison.Ordinal) && key.Length == Prefix.Length + 64 &&
        key[Prefix.Length..].All(Uri.IsHexDigit);
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await SaveAsync(key, ticket); return key;
    }
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (!Valid(key)) throw new ArgumentException("Invalid authentication session key.");
        if (await cache.GetAsync(RevokedKey(key)) is not null || await cache.GetAsync(key) is null) return;
        await SaveAsync(key, ticket);
    }
    private Task SaveAsync(string key, AuthenticationTicket ticket)
    {
        if (!Valid(key)) throw new ArgumentException("Invalid authentication session key.");
        var maximum = clock.GetUtcNow().AddHours(8);
        var expires = ticket.Properties.ExpiresUtc is { } supplied && supplied < maximum ? supplied : maximum;
        ticket.Properties.ExpiresUtc = expires;
        if (expires <= clock.GetUtcNow()) return RemoveAsync(key);
        return cache.SetAsync(key, protector.Protect(TicketSerializer.Default.Serialize(ticket)), new DistributedCacheEntryOptions { AbsoluteExpiration = expires });
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!Valid(key)) return null;
        if (await cache.GetAsync(RevokedKey(key)) is not null) return null;
        var data = await cache.GetAsync(key);
        if (data is null) return null;
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(data));
            if (ticket?.Properties.ExpiresUtc <= clock.GetUtcNow()) { await RemoveAsync(key); return null; }
            return await cache.GetAsync(RevokedKey(key)) is null ? ticket : null;
        }
        catch (CryptographicException) { await RemoveAsync(key); return null; }
    }
    public async Task RemoveAsync(string key)
    {
        if (!Valid(key)) return;
        // A concurrent sliding renewal must never resurrect a signed-out browser's ticket.
        await cache.SetAsync(RevokedKey(key), [1], new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8) });
        await cache.RemoveAsync(key);
    }
}
