using HRM.Application.Contracts.Recruitment;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class RecruitmentScoreValidationTests
{
    private const string Definition = "{\"total\":100,\"components\":[[\"qualification\",60],[\"interview\",40]]}";

    [Theory]
    [InlineData("{\"qualification\":55,\"interview\":35}", true, 90)]
    [InlineData("{\"qualification\":61,\"interview\":35}", false, 0)]
    [InlineData("{\"qualification\":-1,\"interview\":35}", false, 0)]
    [InlineData("{\"qualification\":55}", false, 0)]
    [InlineData("{\"qualification\":55,\"interview\":35,\"bonus\":5}", false, 0)]
    [InlineData("{\"qualification\":55,\"qualification\":50}", false, 0)]
    [InlineData("{\"qualification\":\"55\",\"interview\":35}", false, 0)]
    [InlineData("invalid", false, 0)]
    public void EnforcesOfficialComponents(string json, bool expected, decimal total)
    {
        var valid = RecruitmentScoreValidation.TryCalculate(Definition, json, out var actual);
        Assert.Equal(expected, valid);
        if (valid) Assert.Equal(total, actual);
    }
}
