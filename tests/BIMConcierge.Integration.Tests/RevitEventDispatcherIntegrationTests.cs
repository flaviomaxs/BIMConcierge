using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.Infrastructure.Revit;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Integration.Tests;

public class RevitEventDispatcherIntegrationTests : IDisposable
{
    private readonly Mock<ILocalDatabase> _dbMock = new();
    private readonly RevitEventDispatcher _sut;

    public RevitEventDispatcherIntegrationTests()
    {
        _sut = new RevitEventDispatcher(_dbMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CompanyStandard NamingStandard(
        string id = "s1",
        string rule = "^PRJ-.*$",
        string category = "Walls",
        bool isActive = true,
        bool autoFix = false,
        Severity alertLevel = Severity.Warning) => new()
    {
        Id = id,
        CompanyId = "c1",
        Category = category,
        Name = $"Naming Rule {id}",
        Description = "Element must start with PRJ-",
        Rule = rule,
        IsActive = isActive,
        AutoFix = autoFix,
        AlertLevel = alertLevel
    };

    private void SetupStandards(params CompanyStandard[] standards)
    {
        _dbMock.Setup(d => d.GetStandardsAsync("c1"))
               .ReturnsAsync(standards.ToList());
    }

    private async Task LoadStandards()
    {
        await _sut.LoadStandardsAsync("c1");
    }

    private static List<(string ElementId, string Name, string Category)> Elements(
        params (string id, string name, string category)[] elems) =>
        elems.Select(e => (e.id, e.name, e.category)).ToList();

    // ── LoadStandards + Validate pipeline ────────────────────────────────────

    [Fact]
    public async Task LoadStandards_ThenValidate_RaisesCorrections()
    {
        SetupStandards(NamingStandard());
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        corrections.Should().HaveCount(1);
        corrections[0].RuleId.Should().Be("s1");
        corrections[0].ElementId.Should().Be("e1");
        corrections[0].CanAutoFix.Should().BeFalse();
    }

    // ── Naming convention detection ──────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_NamingConvention_DetectsViolation()
    {
        SetupStandards(NamingStandard(rule: "^PRJ-.*$"));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        corrections.Should().HaveCount(1);
        corrections[0].Description.Should().Contain("Wall-01");
        corrections[0].Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public async Task ValidateElements_PassingElement_NoCorrectionRaised()
    {
        SetupStandards(NamingStandard(rule: "^PRJ-.*$"));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "PRJ-Wall-01", "Walls")));

        corrections.Should().BeEmpty();
        _sut.ActiveCount.Should().Be(0);
    }

    // ── Deduplication ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_DeduplicatesCorrections()
    {
        SetupStandards(NamingStandard());
        await LoadStandards();

        var elements = Elements(("e1", "Wall-01", "Walls"));

        var first = _sut.ValidateElements(elements);
        var second = _sut.ValidateElements(elements);

        first.Should().HaveCount(1);
        second.Should().BeEmpty();
        _sut.ActiveCount.Should().Be(1);
    }

    // ── Dismiss + Clear ──────────────────────────────────────────────────────

    [Fact]
    public async Task DismissCorrection_PreventsReRaise()
    {
        SetupStandards(NamingStandard());
        await LoadStandards();

        var elements = Elements(("e1", "Wall-01", "Walls"));
        var corrections = _sut.ValidateElements(elements);
        corrections.Should().HaveCount(1);

        _sut.DismissCorrection(corrections[0].Id);

        var afterDismiss = _sut.ValidateElements(elements);
        afterDismiss.Should().BeEmpty();
        _sut.ActiveCount.Should().Be(0);
    }

    [Fact]
    public async Task ClearDismissals_AllowsReRaise()
    {
        SetupStandards(NamingStandard());
        await LoadStandards();

        var elements = Elements(("e1", "Wall-01", "Walls"));
        var corrections = _sut.ValidateElements(elements);
        _sut.DismissCorrection(corrections[0].Id);

        _sut.ClearDismissals();

        var afterClear = _sut.ValidateElements(elements);
        afterClear.Should().HaveCount(1);
    }

    // ── Multiple standards ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_MultipleStandards_AllApplied()
    {
        SetupStandards(
            NamingStandard(id: "s1", rule: "^PRJ-.*$", category: "Walls"),
            NamingStandard(id: "s2", rule: "^LVL-.*$", category: "Levels"),
            NamingStandard(id: "s3", rule: "^FAM-.*$", category: "Families")
        );
        await LoadStandards();

        var elements = Elements(
            ("e1", "BadWall", "Walls"),
            ("e2", "BadLevel", "Levels"),
            ("e3", "BadFamily", "Families")
        );

        var corrections = _sut.ValidateElements(elements);

        corrections.Should().HaveCount(3);
        corrections.Select(c => c.RuleId).Should().BeEquivalentTo(["s1", "s2", "s3"]);
    }

    // ── Inactive standard ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_InactiveStandard_Ignored()
    {
        SetupStandards(NamingStandard(isActive: false));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        corrections.Should().BeEmpty();
    }

    // ── Invalid regex ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_InvalidRegex_SkipsGracefully()
    {
        SetupStandards(NamingStandard(rule: "[invalid(regex"));
        await LoadStandards();

        var act = () => _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        act.Should().NotThrow();
        act().Should().BeEmpty();
    }

    // ── Category filtering ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_CategoryFiltering_OnlyMatchingApplied()
    {
        SetupStandards(NamingStandard(category: "Walls"));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(
            ("e1", "BadDoor", "Doors"),
            ("e2", "BadWall", "Walls")
        ));

        corrections.Should().HaveCount(1);
        corrections[0].ElementId.Should().Be("e2");
    }

    // ── Reload standards removes stale corrections ───────────────────────────

    [Fact]
    public async Task ReloadStandards_RemovesStaleCorrectionEvents()
    {
        SetupStandards(NamingStandard(id: "s1"), NamingStandard(id: "s2", rule: "^MEP-.*$", category: "MEP"));
        await LoadStandards();

        _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        _sut.ActiveCount.Should().Be(1);

        // Reload with only s2 (s1 removed)
        _dbMock.Setup(d => d.GetStandardsAsync("c1"))
               .ReturnsAsync([NamingStandard(id: "s2", rule: "^MEP-.*$", category: "MEP")]);
        await _sut.ReloadStandardsAsync("c1");

        _sut.ActiveCount.Should().Be(0);
    }

    // ── AutoFix on detection ─────────────────────────────────────────────────

    [Fact]
    public async Task AutoFixOnDetection_WhenEnabled_TriggersHandler()
    {
        bool handlerCalled = false;
        _sut.RegisterAutoFixHandler((elementId, rule) =>
        {
            handlerCalled = true;
            return Task.FromResult(true);
        });
        _sut.AutoFixOnDetection = true;

        SetupStandards(NamingStandard(autoFix: true));
        await LoadStandards();

        _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        // Fire-and-forget, give it a moment
        await Task.Delay(50);
        handlerCalled.Should().BeTrue();
    }

    // ── TryAutoFix edge cases ────────────────────────────────────────────────

    [Fact]
    public async Task TryAutoFix_UnknownCorrectionId_ReturnsFalse()
    {
        var result = await _sut.TryAutoFixAsync("nonexistent-id");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAutoFix_CanAutoFixFalse_ReturnsFalse()
    {
        SetupStandards(NamingStandard(autoFix: false));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        corrections.Should().HaveCount(1);

        var result = await _sut.TryAutoFixAsync(corrections[0].Id);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAutoFix_NoHandler_ReturnsFalse()
    {
        SetupStandards(NamingStandard(autoFix: true));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        var result = await _sut.TryAutoFixAsync(corrections[0].Id);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAutoFix_Success_MarksAsFixed()
    {
        _sut.RegisterAutoFixHandler((_, _) => Task.FromResult(true));

        SetupStandards(NamingStandard(autoFix: true));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        _sut.ActiveCount.Should().Be(1);

        var result = await _sut.TryAutoFixAsync(corrections[0].Id);

        result.Should().BeTrue();
        _sut.FixedCount.Should().Be(1);
        _sut.ActiveCount.Should().Be(0);
    }

    // ── RunValidation sorting ────────────────────────────────────────────────

    [Fact]
    public async Task RunValidation_ReturnsSortedBySeverityThenDate()
    {
        SetupStandards(
            NamingStandard(id: "s1", rule: "^A-.*$", category: "TypeA", alertLevel: Severity.Info),
            NamingStandard(id: "s2", rule: "^B-.*$", category: "TypeB", alertLevel: Severity.Error),
            NamingStandard(id: "s3", rule: "^C-.*$", category: "TypeC", alertLevel: Severity.Warning)
        );
        await LoadStandards();

        _sut.ValidateElements(Elements(
            ("e1", "BadElem1", "TypeA"),
            ("e2", "BadElem2", "TypeB"),
            ("e3", "BadElem3", "TypeC")
        ));

        var sorted = _sut.RunValidation();

        sorted.Should().HaveCount(3);
        sorted[0].Severity.Should().Be(Severity.Error);
        sorted[1].Severity.Should().Be(Severity.Warning);
        sorted[2].Severity.Should().Be(Severity.Info);
    }

    // ── CorrectionRaised event ───────────────────────────────────────────────

    [Fact]
    public async Task CorrectionRaised_EventFired_WithCorrectData()
    {
        var raisedEvents = new List<CorrectionEvent>();
        _sut.CorrectionRaised += (_, ev) => raisedEvents.Add(ev);

        SetupStandards(NamingStandard());
        await LoadStandards();

        _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        raisedEvents.Should().HaveCount(1);
        raisedEvents[0].RuleId.Should().Be("s1");
        raisedEvents[0].ElementId.Should().Be("e1");
        raisedEvents[0].IsFixed.Should().BeFalse();
    }

    // ── Element auto-resolves when it passes ─────────────────────────────────

    [Fact]
    public async Task ValidateElements_ElementNowPasses_AutoResolvesCorrection()
    {
        SetupStandards(NamingStandard(rule: "^PRJ-.*$"));
        await LoadStandards();

        // First: element fails
        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        corrections.Should().HaveCount(1);
        _sut.ActiveCount.Should().Be(1);

        // Second: same element now passes (renamed)
        _sut.ValidateElements(Elements(("e1", "PRJ-Wall-01", "Walls")));

        _sut.ActiveCount.Should().Be(0);
        _sut.FixedCount.Should().Be(1);
    }

    // ── Statistics ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Statistics_ReflectCurrentState()
    {
        _sut.StandardsLoaded.Should().BeFalse();
        _sut.StandardsCount.Should().Be(0);

        SetupStandards(NamingStandard(), NamingStandard(id: "s2", rule: "^MEP-.*$", category: "MEP"));
        await LoadStandards();

        _sut.StandardsLoaded.Should().BeTrue();
        _sut.StandardsCount.Should().Be(2);
        _sut.ActiveCount.Should().Be(0);
        _sut.TotalValidated.Should().Be(0);

        _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        _sut.ActiveCount.Should().Be(1);
        _sut.TotalValidated.Should().Be(1);
    }

    // ── Empty rule ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateElements_EmptyRule_SkipsStandard()
    {
        SetupStandards(NamingStandard(rule: ""));
        await LoadStandards();

        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        corrections.Should().BeEmpty();
    }

    // ── No standards loaded ──────────────────────────────────────────────────

    [Fact]
    public void ValidateElements_NoStandardsLoaded_ReturnsEmpty()
    {
        var corrections = _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));
        corrections.Should().BeEmpty();
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_ClearsAllState()
    {
        SetupStandards(NamingStandard());
        await LoadStandards();
        _sut.ValidateElements(Elements(("e1", "Wall-01", "Walls")));

        _sut.Dispose();

        _sut.ActiveCount.Should().Be(0);
        _sut.TotalValidated.Should().Be(0);
    }
}
