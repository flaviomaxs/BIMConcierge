using BIMConcierge.Core.Interfaces;
using BIMConcierge.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace BIMConcierge.Core.Tests;

public class LoginViewModelTests
{
    private readonly Mock<IAuthService> _authMock = new();

    private LoginViewModel CreateSut() => new(_authMock.Object);

    [Fact]
    public void InitialState_PropertiesAreDefault()
    {
        LoginViewModel sut = CreateSut();

        sut.Email.Should().BeEmpty();
        sut.Password.Should().BeEmpty();
        sut.LicenseKey.Should().BeEmpty();
        sut.ErrorMessage.Should().BeEmpty();
        sut.HasError.Should().BeFalse();
        sut.IsBusy.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "pass", "key")]
    [InlineData("email", "", "key")]
    [InlineData("email", "pass", "")]
    [InlineData("", "", "")]
    public async Task LoginCommand_EmptyFields_SetsValidationError(string email, string password, string licenseKey)
    {
        LoginViewModel sut = CreateSut();
        sut.Email = email;
        sut.Password = password;
        sut.LicenseKey = licenseKey;

        await sut.LoginCommand.ExecuteAsync(null);

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("Preencha");
    }

    [Fact]
    public async Task LoginCommand_AuthSuccess_InvokesOnLoginSuccess()
    {
        _authMock.Setup(a => a.LoginAsync("a@b.com", "pass", "key"))
            .ReturnsAsync(new AuthResult(true, "token", null));

        LoginViewModel sut = CreateSut();
        sut.Email = "a@b.com";
        sut.Password = "pass";
        sut.LicenseKey = "key";

        bool callbackInvoked = false;
        sut.OnLoginSuccess = () => callbackInvoked = true;

        await sut.LoginCommand.ExecuteAsync(null);

        callbackInvoked.Should().BeTrue();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_AuthFailure_SetsErrorMessage()
    {
        _authMock.Setup(a => a.LoginAsync("a@b.com", "pass", "key"))
            .ReturnsAsync(new AuthResult(false, null, "Credenciais inválidas"));

        LoginViewModel sut = CreateSut();
        sut.Email = "a@b.com";
        sut.Password = "pass";
        sut.LicenseKey = "key";

        await sut.LoginCommand.ExecuteAsync(null);

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Be("Credenciais inválidas");
    }

    [Fact]
    public async Task LoginCommand_AuthFailure_NullErrorMessage_ShowsFallback()
    {
        _authMock.Setup(a => a.LoginAsync("a@b.com", "pass", "key"))
            .ReturnsAsync(new AuthResult(false, null, null));

        LoginViewModel sut = CreateSut();
        sut.Email = "a@b.com";
        sut.Password = "pass";
        sut.LicenseKey = "key";

        await sut.LoginCommand.ExecuteAsync(null);

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Be("Falha no login.");
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        LoginViewModel sut = CreateSut();
        Action act = () => sut.Dispose();
        act.Should().NotThrow();
    }
}
