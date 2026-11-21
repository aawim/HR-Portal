using HRM.Communications;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationApprovalTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("approved")]
    public void HeldOrUnrecognisedApprovalStatesCannotDispatch(string? status) => Assert.False(CommunicationApprovalPolicy.CanDispatch(status));

    [Theory]
    [InlineData("Approved")]
    [InlineData("NotRequired")]
    public void OnlyExplicitDispatchableStatesAreAccepted(string status) => Assert.True(CommunicationApprovalPolicy.CanDispatch(status));
}
