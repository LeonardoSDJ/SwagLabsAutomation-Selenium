using NUnit.Framework;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    public class CartTests : TestBase
    {
        private LoginPage loginPage;
        private ProductsPage productsPage;
        private CartPage cartPage;

        [SetUp]
        public void SetupTest()
        {
            loginPage = new LoginPage(Driver);
            loginPage.NavigateToLoginPage();
            productsPage = loginPage.Login("standard_user", "secret_sauce");
            Assert.That(productsPage.IsOnProductsPage(), Is.True, "Falha ao fazer login para iniciar os testes de carrinho.");
            
            // Inicializando o cartPage após navegar para a página de produtos
            cartPage = productsPage.GoToCart();
        }

        [Test]
        public void AdicionarItemAoCarrinho()
        {
            // Arrange - Página de produtos já carregada após o SetUp

            // Act - Adicionar produto ao carrinho
            productsPage.AddProductToCart("sauce-labs-backpack");

            // Assert - Verificar se o contador do carrinho foi atualizado
            Assert.That(productsPage.GetCartCount(), Is.EqualTo(1), "O contador do carrinho não foi atualizado corretamente.");
        }

        [Test]
        public void AdicionarMultiplosItens()
        {
            // Act - Adicionar vários produtos
            productsPage.AddProductToCart("sauce-labs-backpack");
            productsPage.AddProductToCart("sauce-labs-bike-light");
            productsPage.AddProductToCart("sauce-labs-bolt-t-shirt");

            // Assert - Verificar se o contador do carrinho foi atualizado
            Assert.That(productsPage.GetCartCount(), Is.EqualTo(3), "O contador do carrinho não reflete os 3 itens adicionados.");
        }

        [Test]
        public void IrParaCarrinhoEVoltarParaCompras()
        {
            // Arrange - Adicionar um item ao carrinho
            productsPage.AddProductToCart("sauce-labs-backpack");

            // Act - Ir para o carrinho
            cartPage = productsPage.GoToCart();

            // Assert - Verificar se está na página do carrinho
            Assert.That(cartPage.IsOnCartPage(), Is.True, "Não foi redirecionado para a página do carrinho.");
            Assert.That(cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Número incorreto de itens no carrinho.");

            // Act - Voltar para continuar comprando
            productsPage = cartPage.ContinueShopping();

            // Assert - Verificar se voltou para a página de produtos
            Assert.That(productsPage.IsOnProductsPage(), Is.True, "Não voltou para a página de produtos.");
        }

        [Test]
        public void RemoverItemDoCarrinho()
        {
            // Arrange - Adicionar um item e ir para o carrinho
            productsPage.AddProductToCart("sauce-labs-backpack");
            cartPage = productsPage.GoToCart();
            Assert.That(cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Item não foi adicionado ao carrinho corretamente.");

            // Act - Remover o item
            cartPage.RemoveItemFromCart("sauce-labs-backpack");

            // Assert - Verificar se o item foi removido
            Assert.That(cartPage.GetNumberOfCartItems(), Is.EqualTo(0), "O item não foi removido do carrinho.");
        }
    }
}
