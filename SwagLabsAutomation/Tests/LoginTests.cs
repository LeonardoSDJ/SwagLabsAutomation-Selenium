using NUnit.Framework;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    public class LoginTests : TestBase
    {
        private LoginPage loginPage;

        [SetUp]
        public void SetupTest()
        {
            loginPage = new LoginPage(Driver);
            loginPage.NavigateToLoginPage();
        }

        [Test]
        public void LoginComCredenciaisValidas()
        {
            // Arrange
            string username = "standard_user";
            string password = "secret_sauce";

            // Act
            var productsPage = loginPage.Login(username, password);

            // Assert
            Assert.That(productsPage.IsOnProductsPage(), Is.True, "Login falhou, não redirecionou para a página de produtos.");
        }

        [Test]
        public void LoginComCredenciaisInvalidas()
        {
            // Arrange
            string username = "invalid_user";
            string password = "invalid_password";

            // Act
            loginPage.Login(username, password);
            string errorMessage = loginPage.GetErrorMessage();

            // Assert
            Assert.That(loginPage.IsOnLoginPage(), Is.True, "Não permaneceu na página de login após tentativa com credenciais inválidas.");
            Assert.That(errorMessage.Contains("Username and password do not match"), Is.True, "Mensagem de erro incorreta ou não exibida.");
        }
    }
}