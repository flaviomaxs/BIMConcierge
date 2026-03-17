using BIMConcierge.Core.Interfaces;
using BIMConcierge.Core.Models;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class CompanyStandardsViewModelTests
{
    private readonly Mock<IStandardsService> _standardsMock = new();
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private static readonly User TestUser = new()
    {
        Id = "u1", Name = "Carlos", CompanyId = "c1"
    };

    private CompanyStandardsViewModel CreateSut()
    {
        _authMock.SetupGet(a => a.CurrentUser).Returns(TestUser);
        return new CompanyStandardsViewModel(_standardsMock.Object, _authMock.Object, _navMock.Object);
    }

    [Fact]
    public void InitialState_HasDefaultCategories()
    {
        CompanyStandardsViewModel sut = CreateSut();

        sut.Categories.Should().Contain("Naming Conventions");
        sut.Categories.Should().Contain("LOD Requirements");
        sut.Categories.Should().Contain("Workset Rules");
        sut.Categories.Should().Contain("Best Practices");
        sut.SelectedCategory.Should().Be("Naming Conventions");
    }

    [Fact]
    public async Task LoadCommand_PopulatesStandards()
    {
        var standards = new List<CompanyStandard>
        {
            new() { Id = "s1", Name = "Rule A" },
            new() { Id = "s2", Name = "Rule B" }
        };
        _standardsMock.Setup(s => s.GetStandardsAsync("c1")).ReturnsAsync(standards);

        CompanyStandardsViewModel sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.Standards.Should().HaveCount(2);
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task SaveStandardCommand_DelegatesToService()
    {
        var standard = new CompanyStandard { Id = "s1", Name = "Test" };

        CompanyStandardsViewModel sut = CreateSut();
        await sut.SaveStandardCommand.ExecuteAsync(standard);

        _standardsMock.Verify(s => s.SaveStandardAsync(standard), Times.Once);
    }

    [Fact]
    public async Task DeleteStandardCommand_RemovesFromCollection()
    {
        var standard = new CompanyStandard { Id = "s1", Name = "Test" };

        CompanyStandardsViewModel sut = CreateSut();
        sut.Standards.Add(standard);
        await sut.DeleteStandardCommand.ExecuteAsync(standard);

        sut.Standards.Should().BeEmpty();
        _standardsMock.Verify(s => s.DeleteStandardAsync("s1"), Times.Once);
    }

    [Fact]
    public void SelectCategoryCommand_ChangesSelectedCategory()
    {
        CompanyStandardsViewModel sut = CreateSut();
        sut.SelectCategoryCommand.Execute("LOD Requirements");

        sut.SelectedCategory.Should().Be("LOD Requirements");
    }

    [Fact]
    public void OpenWindowCommand_DelegatesToNavigationService()
    {
        CompanyStandardsViewModel sut = CreateSut();
        sut.OpenWindowCommand.Execute("Dashboard");

        _navMock.Verify(n => n.NavigateTo("Dashboard"), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        CompanyStandardsViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
