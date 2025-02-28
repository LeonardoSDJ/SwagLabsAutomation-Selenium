using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    public class CheckoutTests : TestBase
    {
        private LoginPage _loginPage;
        private ProductsPage _productsPage;
        private CartPage _cartPage;
        private CheckoutPage? _checkoutPage;

        [SetUp]
        public void SetupTest()
        {
            // Configuração inicial: Login e adicionar um produto ao carrinho
            _loginPage = new LoginPage(Driver);
            _loginPage.NavigateToLoginPage();
            _productsPage = _loginPage.Login("standard_user", "secret_sauce");

            // Adicionar produto ao carrinho
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _cartPage = _productsPage.GoToCart();
        }

        [Test]
        public void ProcessoDeCheckoutCompleto()
        {
            // Act - Iniciar checkout
            _checkoutPage = _cartPage.GoToCheckout();

            // Assert - Verificar se está na página de checkout step one
            Assert.That(_checkoutPage.IsOnCheckoutStepOne(), Is.True, "Não foi redirecionado para a primeira etapa do checkout.");

            // Act - Preencher informações pessoais e continuar
            _checkoutPage.FillPersonalInfo("João", "Silva", "12345").ClickContinue();

            // Assert - Verificar se avançou para a página de revisão
            Assert.That(_checkoutPage.IsOnCheckoutStepTwo(), Is.True, "Não avançou para a etapa de revisão do checkout.");

            // Act - Finalizar compra
            _checkoutPage.CompleteCheckout();

            // Assert - Verificar se o checkout foi concluído com sucesso
            Assert.That(_checkoutPage.IsOnCheckoutComplete(), Is.True, "O checkout não foi concluído com sucesso.");
        }

        [Test]
        public void VerificarTotalDoCheckout()
        {
            // Arrange - Adicionar mais um produto para aumentar o valor total
            _cartPage.ContinueShopping();
            _productsPage.AddProductToCart("sauce-labs-bike-light");
            _cartPage = _productsPage.GoToCart();

            // Act - Iniciar checkout e preencher informações
            _checkoutPage = _cartPage.GoToCheckout();
            _checkoutPage.FillPersonalInfo("Maria", "Souza", "54321").ClickContinue();

            // Assert - Verificar se o total está maior que zero
            double totalPrice = _checkoutPage.GetTotalPrice();
            Assert.That(totalPrice,Is.GreaterThan(0), "O preço total não foi calculado corretamente.");
        }

        [Test]
        public void CancelarCheckout()
        {
            // Act - Iniciar checkout e depois cancelar
            _checkoutPage = _cartPage.GoToCheckout();
            _cartPage = _checkoutPage.ClickCancel();

            // Assert - Verificar se voltou para a página do carrinho
            Assert.That(_cartPage.IsOnCartPage(), Is.True, "Não voltou para a página do carrinho após cancelar o checkout.");
        }

        [Test]
        public void VoltarParaProdutosAposCompra()
        {
            // Act - Completar o processo de checkout
            _checkoutPage = _cartPage.GoToCheckout();
            _checkoutPage.FillPersonalInfo("Carlos", "Ferreira", "67890").ClickContinue();
            _checkoutPage.CompleteCheckout();

            // Act - Voltar para a página de produtos
            _productsPage = _checkoutPage.GoBackToProducts();

            // Assert - Verificar se voltou para a página de produtos
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Não voltou para a página de produtos após concluir a compra.");

            // Assert - Verificar se o carrinho está vazio
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(0), "O carrinho não está vazio após a conclusão da compra.");
        }
    }
}