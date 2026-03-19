using BIMConcierge.Plugin;
using FluentAssertions;
using Xunit;

namespace BIMConcierge.Integration.Tests;

public class RevitEventBridgeTests
{
    [Theory]
    [InlineData("^PRJ-.*$", "PRJ-")]
    [InlineData("^MEP_", "MEP_")]
    [InlineData("WALL-", "WALL-")]
    [InlineData("^.*$", "")]
    [InlineData("", "")]
    [InlineData("^ABC123", "ABC123")]
    [InlineData("^PRJ-MECH-.*", "PRJ-MECH-")]
    [InlineData("^[A-Z]+", "")]
    [InlineData("^Level_01$", "Level_01")]
    public void ExtractPrefixFromRule_ReturnsExpectedPrefix(string rule, string expected)
    {
        string result = RevitEventBridge.ExtractPrefixFromRule(rule);
        result.Should().Be(expected);
    }
}
