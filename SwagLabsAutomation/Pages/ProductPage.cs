using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SwagLabsAutomation.Pages
{
    public class ProductsPage : BasePage
    {
        private By _productItems = By.CssSelector(".inventory_item");
        private By _productImages = By.CssSelector(".inventory_item_img img");
        private By _productNames = By.CssSelector(".inventory_item_name");
        private By _addToCartButtons = By.CssSelector("[data-test^='add-to-cart']");
        private By _cartIcon = By.CssSelector(".shopping_cart_link");
        private By _productSortContainer = By.CssSelector("[data-test='product_sort_container']");

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

        public string GetFirstProductImageSrc()
        {
            var images = Driver.FindElements(_productImages);
            if (images.Count > 0)
            {
                return images[0].GetAttribute("src");
            }
            return string.Empty;
        }

        public bool AreAllProductImagesTheSame()
        {
            var images = Driver.FindElements(_productImages);
            if (images.Count <= 1) return true;

            string firstSrc = images[0].GetAttribute("src");

            for (int i = 1; i < images.Count; i++)
            {
                if (images[i].GetAttribute("src") != firstSrc)
                {
                    return false;
                }
            }

            return true;
        }
        public void SortProductsBy(string sortOption)
        {
            WaitForElementVisible(_productSortContainer);
            var select = new SelectElement(Driver.FindElement(_productSortContainer));

            switch (sortOption.ToLower())
            {
                case "az":
                    select.SelectByValue("az");
                    break;
                case "za":
                    select.SelectByValue("za");
                    break;
                case "lohi":
                    select.SelectByValue("lohi");
                    break;
                case "hilo":
                    select.SelectByValue("hilo");
                    break;
            }
        }

        public bool AreProductsSortedByNameDescending()
        {
            WaitForElementVisible(_productNames);
            var productNameElements = Driver.FindElements(_productNames);

            if (productNameElements.Count <= 1) return true;

            var productNames = productNameElements.Select(e => e.Text).ToList();

            var sortedNames = new List<string>(productNames);
            sortedNames.Sort();
            sortedNames.Reverse(); // Para ordem decrescente (Z-A)

            return Enumerable.SequenceEqual(productNames, sortedNames);
        }
    }
}