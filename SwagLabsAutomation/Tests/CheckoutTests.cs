using NUnit.Framework;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    public class CheckoutTests : TestBase
    {
        private LoginPage loginPage;
        private ProductsPage productsPage;
        private CartPage cartPage;
        private CheckoutPage? checkoutPage;

        [SetUp]
        public void SetupTest()
        {
            // Configuração inicial: Login e adicionar um produto ao carrinho
            loginPage = new LoginPage(Driver);
            loginPage.NavigateToLoginPage();
            productsPage = loginPage.Login("standard_user", "secret_sauce");

            // Adicionar produto ao carrinho
            productsPage.AddProductToCart("sauce-labs-backpack");
            cartPage = productsPage.GoToCart();
        }

        [Test]
        public void ProcessoDeCheckoutCompleto()
        {
            // Act - Iniciar checkout
            checkoutPage = cartPage.GoToCheckout();

            // Assert - Verificar se está na página de checkout step one
            Assert.That(checkoutPage.IsOnCheckoutStepOne(), Is.True, "Não foi redirecionado para a primeira etapa do checkout.");

            // Act - Preencher informações pessoais e continuar
            checkoutPage.FillPersonalInfo("João", "Silva", "12345").ClickContinue();

            // Assert - Verificar se avançou para a página de revisão
            Assert.That(checkoutPage.IsOnCheckoutStepTwo(), Is.True, "Não avançou para a etapa de revisão do checkout.");

            // Act - Finalizar compra
            checkoutPage.CompleteCheckout();

            // Assert - Verificar se o checkout foi concluído com sucesso
            Assert.That(checkoutPage.IsOnCheckoutComplete(), Is.True, "O checkout não foi concluído com sucesso.");
        }

        [Test]
        public void VerificarTotalDoCheckout()
        {
            // Arrange - Adicionar mais um produto para aumentar o valor total
            cartPage.ContinueShopping();
            productsPage.AddProductToCart("sauce-labs-bike-light");
            cartPage = productsPage.GoToCart();

            // Act - Iniciar checkout e preencher informações
            checkoutPage = cartPage.GoToCheckout();
            checkoutPage.FillPersonalInfo("Maria", "Souza", "54321").ClickContinue();

            // Assert - Verificar se o total está maior que zero
            double totalPrice = checkoutPage.GetTotalPrice();
            Assert.That(totalPrice,Is.GreaterThan(0), "O preço total não foi calculado corretamente.");
        }

        [Test]
        public void CancelarCheckout()
        {
            // Act - Iniciar checkout e depois cancelar
            checkoutPage = cartPage.GoToCheckout();
            cartPage = checkoutPage.ClickCancel();

            // Assert - Verificar se voltou para a página do carrinho
            Assert.That(cartPage.IsOnCartPage(), Is.True, "Não voltou para a página do carrinho após cancelar o checkout.");
        }

        [Test]
        public void VoltarParaProdutosAposCompra()
        {
            // Act - Completar o processo de checkout
            checkoutPage = cartPage.GoToCheckout();
            checkoutPage.FillPersonalInfo("Carlos", "Ferreira", "67890").ClickContinue();
            checkoutPage.CompleteCheckout();

            // Act - Voltar para a página de produtos
            productsPage = checkoutPage.GoBackToProducts();

            // Assert - Verificar se voltou para a página de produtos
            Assert.That(productsPage.IsOnProductsPage(), Is.True, "Não voltou para a página de produtos após concluir a compra.");

            // Assert - Verificar se o carrinho está vazio
            Assert.That(productsPage.GetCartCount(), Is.EqualTo(0), "O carrinho não está vazio após a conclusão da compra.");
        }
    }
}