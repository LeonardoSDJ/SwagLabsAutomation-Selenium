using NUnit.Framework;
using OpenQA.Selenium;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;
using System;

namespace SwagLabsAutomation.Tests
{
    [TestFixture]
    public class UserSpecificTests : TestBase
    {
        private LoginPage _loginPage;
        private ProductsPage _productsPage;
        private CartPage _cartPage;

        [SetUp]
        public new void Setup()
        {
            _loginPage = new LoginPage(Driver);
            _productsPage = new ProductsPage(Driver);
            _cartPage = new CartPage(Driver);
        }

        [Test]
        [Description("Verifica se o locked_out_user não consegue fazer login")]
        public void LockedOutUser_CannotLogin()
        {
            // Arrange - Navegar para a página de login
            _loginPage.GoTo();

            // Act - Tentar fazer login com locked_out_user
            _loginPage.Login("locked_out_user", "secret_sauce");

            // Assert - Verificar se a mensagem de erro específica para usuário bloqueado é exibida
            Assert.That(_loginPage.GetErrorMessage(), Is.EqualTo("Epic sadface: Sorry, this user has been locked out."),
                "Mensagem de erro para usuário bloqueado não foi exibida corretamente");

            // Verificar que o usuário não foi redirecionado para a página de produtos
            Assert.That(Driver.Url, Does.Not.Contain("/inventory.html"),
                "Usuário bloqueado conseguiu fazer login, o que não deveria acontecer");
        }

        [Test]
        [Description("Verifica comportamentos específicos do problem_user")]
        public void ProblemUser_DisplaysWrongProductImages()
        {
            // Arrange - Login com problem_user
            _loginPage.GoTo();
            _loginPage.Login("problem_user", "secret_sauce");

            // Act & Assert - Verificar se todas as imagens dos produtos são iguais (comportamento conhecido do problem_user)
            string firstImageSrc = _productsPage.GetFirstProductImageSrc();

            Assert.That(_productsPage.AreAllProductImagesTheSame(), Is.True,
                "As imagens dos produtos para problem_user deveriam ser todas iguais");
        }

        [Test]
        [Description("Verifica problemas de preenchimento de formulário do problem_user")]
        public void ProblemUser_CannotCompleteCheckout()
        {
            // Arrange - Login com problem_user e adicionar um produto ao carrinho
            _loginPage.GoTo();
            _loginPage.Login("problem_user", "secret_sauce");
            _productsPage.AddProductToCart("Sauce Labs Backpack");
            _productsPage.GoToCart();

            // Act - Iniciar checkout
            _cartPage.GoToCheckout();

            // Tentar preencher o formulário (isso deve falhar para o problem_user no campo de sobrenome)
            var checkoutPage = new CheckoutPage(Driver);
            checkoutPage.FillPersonalInfo("Test", "User", "12345");

            // Assert - Verificar se o botão continuar está desabilitado ou se aparece uma mensagem de erro
            // O problem_user não consegue preencher o sobrenome corretamente
            Assert.That(checkoutPage.HasFormErrors(), Is.True,
                "Problem user deveria ter problemas ao preencher o formulário de checkout");
        }

        [Test]
        [Description("Verifica problemas específicos do problem_user com ordenação")]
        public void ProblemUser_SortingDoesNotWork()
        {
            // Arrange - Login com problem_user
            _loginPage.GoTo();
            _loginPage.Login("problem_user", "secret_sauce");

            // Act - Tentar ordenar produtos por nome (Z-A)
            _productsPage.SortProductsBy("za");

            // Assert - Verificar se a ordenação não funcionou (comportamento conhecido do problem_user)
            Assert.That(_productsPage.AreProductsSortedByNameDescending(), Is.False,
                "A ordenação para problem_user não deveria funcionar corretamente");
        }

        [Test]
        [Description("Verifica tempo de carregamento do performance_glitch_user")]
        public void PerformanceGlitchUser_HasSlowPageLoad()
        {
            // Arrange - Iniciar temporizador
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Fazer login com performance_glitch_user
            _loginPage.GoTo();
            _loginPage.Login("performance_glitch_user", "secret_sauce");

            // Parar o temporizador após o carregamento da página de produtos
            stopwatch.Stop();

            // Assert - Verificar se o tempo de carregamento é significativamente maior 
            // (ajuste o valor de acordo com o esperado para sua rede/ambiente)
            Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThan(2000),
                "O tempo de carregamento do performance_glitch_user deveria ser mais lento");
        }

        [Test]
        [Description("Verifica que o performance_glitch_user pode completar o fluxo de compra, apesar da performance lenta")]
        public void PerformanceGlitchUser_CanCompleteCheckout()
        {
            // Arrange - Login com performance_glitch_user
            _loginPage.GoTo();
            _loginPage.Login("performance_glitch_user", "secret_sauce");

            // Act - Completar o fluxo de compra
            _productsPage.AddProductToCart("Sauce Labs Backpack");
            _productsPage.GoToCart();
            _cartPage.GoToCheckout();

            var checkoutPage = new CheckoutPage(Driver);
            checkoutPage.FillPersonalInfo("Test", "User", "12345");
            checkoutPage.ClickContinue();
            checkoutPage.CompleteCheckout();

            // Assert - Verificar se a compra foi concluída com sucesso
            Assert.That(checkoutPage.IsOrderComplete(), Is.True,
                "O performance_glitch_user deveria conseguir completar o checkout");
        }
    }
}