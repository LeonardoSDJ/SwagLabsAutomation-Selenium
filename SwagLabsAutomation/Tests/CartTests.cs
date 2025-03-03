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
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Failed to login to start cart tests.");
            
        }

        [Test]
        public void AddItemToCart()
        {
            // Arrange - Products page already loaded after SetUp

            // Act - Add product to cart
            _productsPage.AddProductToCart("sauce-labs-backpack");

            // Assert - Verify cart counter was updated
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(1), "Cart counter was not updated correctly.");
        }

        [Test]
        public void AddMultipleItems()
        {
            // Act - Add multiple products
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _productsPage.AddProductToCart("sauce-labs-bike-light");
            _productsPage.AddProductToCart("sauce-labs-bolt-t-shirt");

            // Assert - Verify cart counter was updated
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(3), "Cart counter does not reflect the 3 added items.");
        }

        [Test]
        public void GoToCartAndContinueShopping()
        {
            // Arrange - Add an item to cart
            _productsPage.AddProductToCart("sauce-labs-backpack");

            // Act - Go to cart
            _cartPage = _productsPage.GoToCart();

            // Assert - Verify we're on cart page
            Assert.That(_cartPage.IsOnCartPage(), Is.True, "Was not redirected to cart page.");
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Incorrect number of items in cart.");

            // Act - Return to continue shopping
            _productsPage = _cartPage.ContinueShopping();

            // Assert - Verify we're back on products page
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Did not return to products page.");
        }

        [Test]
        public void RemoveItemFromCart()
        {
            // Arrange - Add an item and go to cart
            _productsPage.AddProductToCart("sauce-labs-backpack");
            _cartPage = _productsPage.GoToCart();
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(1), "Item was not added to cart correctly.");

            // Act - Remove the item
            _cartPage.RemoveItemFromCart("sauce-labs-backpack");

            // Assert - Verify the item was removed
            Assert.That(_cartPage.GetNumberOfCartItems(), Is.EqualTo(0), "The item was not removed from cart.");
        }
    }
}