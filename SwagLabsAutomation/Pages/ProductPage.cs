using OpenQA.Selenium;

namespace SwagLabsAutomation.Pages
{
    public class ProductsPage : BasePage
    {
        // Locators
        private By ProductsTitle => By.ClassName("title");
        private By CartBadge => By.ClassName("shopping_cart_badge");
        private By CartLink => By.ClassName("shopping_cart_link");

        public ProductsPage(IWebDriver driver) : base(driver) { }

        public bool IsOnProductsPage()
        {
            WaitForElementVisible(ProductsTitle);
            return Driver.Url.Contains("inventory.html") && IsElementDisplayed(ProductsTitle);
        }

        public void AddProductToCart(string productId)
        {
            string addToCartButtonId = $"add-to-cart-{productId}";
            By addToCartButton = By.Id(addToCartButtonId);

            WaitForElementClickable(addToCartButton);
            Driver.FindElement(addToCartButton).Click();
        }

        public int GetCartCount()
        {
            if (IsElementDisplayed(CartBadge))
            {
                return int.Parse(Driver.FindElement(CartBadge).Text);
            }
            return 0;
        }

        public CartPage GoToCart()
        {
            WaitForElementClickable(CartLink);
            Driver.FindElement(CartLink).Click();

            return new CartPage(Driver);
        }
    }
}