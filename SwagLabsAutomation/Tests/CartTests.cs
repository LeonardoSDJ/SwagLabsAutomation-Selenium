using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    public class CartTests : TestBase
    {
        private LoginPage _loginPage;
        private ProductsPage _productsPage;
        private CartPage? _cartPage;

        [SetUp]
        public void SetupTest()
        {
            _loginPage = new LoginPage(Driver);
            _loginPage.NavigateToLoginPage();
            _productsPage = _loginPage.Login("standard_user", "secret_sauce");
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Falha ao fazer login para iniciar os testes de carrinho.");
            
        }

        [Test]
        public void AdicionarItemAoCarrinho()
        {
            // Arrange - Página de produtos já carregada após o SetUp

            // Act - Adicionar produto ao carrinho
            _productsPage.AddProductToCart("sauce-labs-backpack");

            // Assert - Verificar se o contador do carrinho foi atualizado
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(1), "O contador do carrinho não foi atualizado corretamente.");
        }

        [Test]
        public void AdicionarMultiplosItens()
        {
            // Act - Adicionar vários produtos
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _productsPage.AddProductToCart("sauce-labs-bike-light");
            _productsPage.AddProductToCart("sauce-labs-bolt-t-shirt");

            // Assert - Verificar se o contador do carrinho foi atualizado
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(3), "O contador do carrinho não reflete os 3 itens adicionados.");
        }

        [Test]
        public void IrParaCarrinhoEVoltarParaCompras()
        {
            // Arrange - Adicionar um item ao carrinho
            _productsPage.AddProductToCart("sauce-labs-backpack");

            // Act - Ir para o carrinho
            _cartPage = _productsPage.GoToCart();

            // Assert - Verificar se está na página do carrinho
            Assert.That(_cartPage.IsOnCartPage(), Is.True, "Não foi redirecionado para a página do carrinho.");
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Número incorreto de itens no carrinho.");

            // Act - Voltar para continuar comprando
            _productsPage = _cartPage.ContinueShopping();

            // Assert - Verificar se voltou para a página de produtos
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Não voltou para a página de produtos.");
        }

        [Test]
        public void RemoverItemDoCarrinho()
        {
            // Arrange - Adicionar um item e ir para o carrinho
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _cartPage = _productsPage.GoToCart();
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Item não foi adicionado ao carrinho corretamente.");

            // Act - Remover o item
            _cartPage.RemoveItemFromCart("sauce-labs-backpack");

            // Assert - Verificar se o item foi removido
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(0), "O item não foi removido do carrinho.");
        }
    }
}
