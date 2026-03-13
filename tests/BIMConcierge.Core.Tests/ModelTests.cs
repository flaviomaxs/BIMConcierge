using BIMConcierge.Core.Models;
using FluentAssertions;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class LicenseModelTests
{
    [Fact]
    public void IsValid_WhenNotExpiredAndHasSeats_ReturnsTrue()
    {
        var license = new License
        {
            Key       = "TEST-0001-0001-0001",
            MaxSeats  = 10,
            UsedSeats = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        license.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        var license = new License
        {
            MaxSeats  = 10,
            UsedSeats = 0,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        license.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenNoSeatsLeft_ReturnsFalse()
    {
        var license = new License
        {
            MaxSeats  = 5,
            UsedSeats = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        license.IsValid.Should().BeFalse();
    }
}

public class TutorialProgressTests
{
    [Fact]
    public void ProgressPercent_CalculatesCorrectly()
    {
        var progress = new TutorialProgress { CurrentStep = 2, TotalSteps = 4 };
        progress.ProgressPercent.Should().Be(50.0);
    }

    [Fact]
    public void ProgressPercent_WhenTotalStepsZero_ReturnsZero()
    {
        var progress = new TutorialProgress { CurrentStep = 0, TotalSteps = 0 };
        progress.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void ProgressPercent_WhenComplete_Returns100()
    {
        var progress = new TutorialProgress { CurrentStep = 5, TotalSteps = 5 };
        progress.ProgressPercent.Should().Be(100.0);
    }
}

public class CorrectionEventTests
{
    [Fact]
    public void NewCorrectionEvent_HasUniqueId()
    {
        var e1 = new CorrectionEvent();
        var e2 = new CorrectionEvent();
        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void NewCorrectionEvent_IsNotFixed()
    {
        var ev = new CorrectionEvent();
        ev.IsFixed.Should().BeFalse();
    }
}
