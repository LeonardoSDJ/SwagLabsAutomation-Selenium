using OpenQA.Selenium;

namespace SwagLabsAutomation.Pages
{
    public class CartPage : BasePage
    {
        // Locators
        private By CartTitle => By.ClassName("title");
        private By CheckoutButton => By.Id("checkout");
        private By ContinueShoppingButton => By.Id("continue-shopping");
        private By CartItems => By.ClassName("cart_item");

        public CartPage(IWebDriver driver) : base(driver) { }

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
            string removeButtonId = $"remove-{productId}";
            By removeButton = By.Id(removeButtonId);

            if (IsElementDisplayed(removeButton))
            {
                WaitForElementClickable(removeButton);
                Driver.FindElement(removeButton).Click();
            }
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