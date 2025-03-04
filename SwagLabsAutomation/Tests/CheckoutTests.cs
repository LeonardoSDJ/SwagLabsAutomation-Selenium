using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class CheckoutTests : TestBase
{
   private LoginPage _loginPage;
   private ProductsPage _productsPage;
   private CartPage _cartPage;
   private CheckoutPage? _checkoutPage;

   [SetUp]
   public void SetupTest()
   {
       // Initialize base TestBase including BiDi monitoring
       base.Setup();
       
       // Initial setup: Login and add a product to cart
       _loginPage = new LoginPage(Driver);
       _loginPage.NavigateToLoginPage();
       _productsPage = _loginPage.Login("standard_user", "secret_sauce");

       // Add product to cart
       _productsPage.AddProductToCart("sauce-labs-backpack");
       _cartPage = _productsPage.GoToCart();
   }

   [Test]
   public void CompleteCheckoutProcess()
   {
       LogInfo("Starting checkout process");
       
       // Act - Start checkout
       _checkoutPage = _cartPage.GoToCheckout();

       // Assert - Verify we're on checkout step one page
       Assert.That(_checkoutPage.IsOnCheckoutStepOne(), Is.True, "Was not redirected to checkout step one.");

       // Act - Fill personal information and continue
       _checkoutPage.FillPersonalInfo("John", "Smith", "12345").ClickContinue();

       // Assert - Verify we've advanced to the review page
       Assert.That(_checkoutPage.IsOnCheckoutStepTwo(), Is.True, "Did not advance to checkout review step.");

       // Act - Complete purchase
       _checkoutPage.CompleteCheckout();

       // Assert - Verify checkout was completed successfully
       Assert.That(_checkoutPage.IsOnCheckoutComplete(), Is.True, "Checkout was not completed successfully.");
       
       LogInfo("Checkout process completed successfully");
   }

   [Test]
   public void VerifyCheckoutTotal()
   {
      
       LogInfo("Starting checkout total verification");
       
       // Arrange - Add another product to increase total value
       _cartPage.ContinueShopping();
       _productsPage.AddProductToCart("sauce-labs-bike-light");
       _cartPage = _productsPage.GoToCart();

       // Act - Start checkout and fill information
       _checkoutPage = _cartPage.GoToCheckout();
       _checkoutPage.FillPersonalInfo("Mary", "Jones", "54321").ClickContinue();

       // Assert - Verify the total is greater than zero
       double totalPrice = _checkoutPage.GetTotalPrice();
       Assert.That(totalPrice, Is.GreaterThan(0), "Total price was not calculated correctly.");
       
       LogInfo($"Verified checkout total: ${totalPrice}");
   }

   [Test]
   public void CancelCheckout()
   {
       LogInfo("Testing checkout cancellation flow");
       
       // Act - Start checkout and then cancel
       _checkoutPage = _cartPage.GoToCheckout();
       _cartPage = _checkoutPage.ClickCancel();

       // Assert - Verify we returned to cart page
       Assert.That(_cartPage.IsOnCartPage(), Is.True, "Did not return to cart page after canceling checkout.");
   }

   [Test]
   public void ReturnToProductsAfterPurchase()
   {
       LogInfo("Testing post-purchase navigation flow");
       
       // Act - Complete checkout process
       _checkoutPage = _cartPage.GoToCheckout();
       _checkoutPage.FillPersonalInfo("Charles", "Miller", "67890").ClickContinue();
       _checkoutPage.CompleteCheckout();

       // Act - Return to products page
       _productsPage = _checkoutPage.GoBackToProducts();

        using (Assert.EnterMultipleScope())
        {
            // Assert - Verify we returned to products page
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, "Did not return to products page after completing purchase.");

            // Assert - Verify cart is empty
            Assert.That(_productsPage.GetCartCount(), Is.EqualTo(0), "Cart is not empty after purchase completion.");
        }
   }
}