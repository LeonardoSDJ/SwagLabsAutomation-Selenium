using NUnit.Framework;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

public class LoginTests : TestBase
{
    private LoginPage _loginPage;

    [SetUp]
    public void SetupTest()
    {
        _loginPage = new LoginPage(Driver);
        LogInfo("Navegando para a página de login");
        _loginPage.NavigateToLoginPage();
        LogInfo("Página de login carregada");
    }

    [Test]
    public void LoginComCredenciaisValidas()
    {
        // Arrange
        string username = "standard_user";
        string password = "secret_sauce";
        LogInfo($"Tentando login com usuário: {username}");

        // Act
        LogInfo("Realizando login");
        var productsPage = _loginPage.Login(username, password);

        // Assert
        LogInfo("Verificando redirecionamento para a página de produtos");
        bool isOnProductsPage = productsPage.IsOnProductsPage();
        Assert.That(isOnProductsPage, Is.True, "Login falhou, não redirecionou para a página de produtos.");

        if (isOnProductsPage)
            LogPass("Login realizado com sucesso, redirecionado para página de produtos");
        else
            LogFail("Falha no redirecionamento após login");
    }

    [Test]
    public void LoginComCredenciaisInvalidas()
    {
        // Arrange
        string username = "invalid_user";
        string password = "invalid_password";
        LogInfo($"Tentando login com credenciais inválidas: {username}");

        // Act
        LogInfo("Realizando login com credenciais inválidas");
        _loginPage.Login(username, password);

        LogInfo("Verificando mensagem de erro");
        string errorMessage = _loginPage.GetErrorMessage();
        LogInfo($"Mensagem de erro obtida: '{errorMessage}'");

        // Assert
        LogInfo("Verificando se permaneceu na página de login");
        bool isOnLoginPage = _loginPage.IsOnLoginPage();
        Assert.That(isOnLoginPage, Is.True, "Não permaneceu na página de login após tentativa com credenciais inválidas.");

        LogInfo("Verificando conteúdo da mensagem de erro");
        bool containsErrorMessage = errorMessage.Contains("Username and password do not match");
        Assert.That(containsErrorMessage, Is.True, "Mensagem de erro incorreta ou não exibida.");

        if (isOnLoginPage && containsErrorMessage)
            LogPass("Teste de login inválido concluído com sucesso");
    }
}