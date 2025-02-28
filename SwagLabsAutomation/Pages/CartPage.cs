using OpenQA.Selenium;

namespace SwagLabsAutomation.Pages
{
    public class CartPage(IWebDriver driver) : BasePage(driver)
    {
        // Locators
        private static By CartTitle => By.ClassName("title");
        private static By CheckoutButton => By.Id("checkout");
        private static By ContinueShoppingButton => By.Id("continue-shopping");
        private static By CartItems => By.ClassName("cart_item");

        public bool IsOnCartPage()
        {
            WaitForElementVisible(CartTitle);
            return Driver.Url.Contains("cart.html") && IsElementDisplayed(CartTitle);
        }

        public int GetNumberOfCartItems()
        {
            return Driver.FindElements(CartItems).Count;
        }

        public void RemoveItemFromCart(string productId)
        {
            var removeButtonId = $"remove-{productId}";
            var removeButton = By.Id(removeButtonId);

            if (!IsElementDisplayed(removeButton)) return;
            WaitForElementClickable(removeButton);
            Driver.FindElement(removeButton).Click();
        }

        public CheckoutPage GoToCheckout()
        {
            WaitForElementClickable(CheckoutButton);
            Driver.FindElement(CheckoutButton).Click();

            return new CheckoutPage(Driver);
        }

        public ProductsPage ContinueShopping()
        {
            WaitForElementClickable(ContinueShoppingButton);
            Driver.FindElement(ContinueShoppingButton).Click();

            return new ProductsPage(Driver);
        }
    }
}