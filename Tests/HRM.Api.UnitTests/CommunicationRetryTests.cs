using HRM.Communications;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationRetryTests
{
    [Theory]
    [InlineData("CredentialNotConfigured", true)]
    [InlineData("DestinationDisabledOrUnavailable", true)]
    [InlineData("Telegram_IndeterminateTimeout", false)]
    [InlineData("IndeterminateWorkerInterruption", false)]
    [InlineData("DeliveryExpired", false)]
    [InlineData("Telegram_500", false)]
    public void OnlyKnownUnsentFailuresCanBeRetried(string error, bool expected)
    {
        var now = DateTime.UtcNow;
        Assert.Equal(expected, CommunicationRetryPolicy.CanRetry("Failed", error, 1, now, now, 24));
    }

    [Fact]
    public void SentExpiredAndExhaustedDeliveriesCannotBeRetried()
    {
        var now = DateTime.UtcNow;
        Assert.False(CommunicationRetryPolicy.CanRetry("Sent", "CredentialNotConfigured", 1, now, now, 24));
        Assert.False(CommunicationRetryPolicy.CanRetry("Failed", "CredentialNotConfigured", 5, now, now, 24));
        Assert.False(CommunicationRetryPolicy.CanRetry("Failed", "CredentialNotConfigured", 1, now.AddHours(-25), now, 24));
    }
}
