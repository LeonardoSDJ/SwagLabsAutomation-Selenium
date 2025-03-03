using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SwagLabsAutomation.Pages;

public class ProductsPage(IWebDriver? driver) : BasePage(driver)
{
    private readonly By _productImages = By.CssSelector(".inventory_item_img img");
    private readonly By _productNames = By.CssSelector(".inventory_item_name");
    private readonly By _productSortContainer = By.ClassName("product_sort_container");
    private static By ProductsTitle => By.ClassName("title");
    private static By CartBadge => By.ClassName("shopping_cart_badge");
    private static By CartLink => By.ClassName("shopping_cart_link");

    public bool IsOnProductsPage()
    {
        WaitForElementVisible(ProductsTitle);
        return Driver!.Url.Contains("inventory.html") && IsElementDisplayed(ProductsTitle);
    }

    public void AddProductToCart(string productId)
    {
        var addToCartButtonId = $"add-to-cart-{productId}";
        var addToCartButton = By.Id(addToCartButtonId);

        WaitForElementClickable(addToCartButton);
        Driver!.FindElement(addToCartButton).Click();
    }

    public int GetCartCount()
    {
        return IsElementDisplayed(CartBadge) ? int.Parse(Driver!.FindElement(CartBadge).Text) : 0;
    }

    public CartPage GoToCart()
    {
        WaitForElementClickable(CartLink);
        Driver!.FindElement(CartLink).Click();

        return new CartPage(Driver);
    }

    public string GetFirstProductImageSrc()
    {
        var images = Driver!.FindElements(_productImages);
        return images.Count > 0 ? images[0].GetAttribute("src") : string.Empty;
    }

    public bool AreAllProductImagesTheSame()
    {
        var images = Driver!.FindElements(_productImages);
        if (images.Count <= 1) return true;

        var firstSrc = images[0].GetAttribute("src");

        for (var i = 1; i < images.Count; i++)
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
        var select = new SelectElement(Driver!.FindElement(_productSortContainer));

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
        var productNameElements = Driver!.FindElements(_productNames);

        if (productNameElements.Count <= 1) return true;

        var productNames = productNameElements.Select(e => e.Text).ToList();

        var sortedNames = new List<string>(productNames);
        sortedNames.Sort();
        sortedNames.Reverse(); // Para ordem decrescente (Z-A)

        return productNames.SequenceEqual(sortedNames);
    }
    public List<string> GetAllProductNames()
    {
        WaitForElementVisible(_productNames);
        var elements = Driver!.FindElements(_productNames);
        return elements.Select(e => e.Text).ToList();
    }

    public void RemoveProductFromCart(string productId)
    {
        var removeButtonId = $"remove-{productId}";
        var removeButton = By.Id(removeButtonId);

        WaitForElementClickable(removeButton);
        Driver!.FindElement(removeButton).Click();
    }

    private List<double> GetProductPrices()
    {
        var priceElements = Driver!.FindElements(By.CssSelector(".inventory_item_price"));
        var prices = new List<double>();
        
        foreach (var element in priceElements)
        {
            var priceText = element.Text;
            // Remove symbol $ e and convert to double
            if (priceText.StartsWith(new StringBuilder().Append('$').ToString()) && double.TryParse(priceText.AsSpan(1), out var price))
            {
                prices.Add(price);
            }
        }
        
        return prices;
    }

    public void NavigateToProductDetails(int index)
    {
        var productElements = Driver!.FindElements(_productNames);
    
        if (index >= 0 && index < productElements.Count)
        {
            productElements[index].Click();
        }
        else
        {
            throw new IndexOutOfRangeException($"Índice {index} fora dos limites (0-{productElements.Count - 1})");
        }
    }

    public List<string> GetProductDescriptions()
    {
        var descElements = Driver!.FindElements(By.CssSelector(".inventory_item_desc"));
        return descElements.Select(e => e.Text).ToList();
    }

    public bool AreProductsSortedByPriceAscending()
    {
        var prices = GetProductPrices();
        
        if (prices.Count <= 1) return true;
        
        for (int i = 0; i < prices.Count - 1; i++)
        {
            if (prices[i] > prices[i + 1])
            {
                return false;
            }
        }
        
        return true;
    }

    public bool AreProductsSortedByPriceDescending()
    {
        var prices = GetProductPrices();
        
        if (prices.Count <= 1) return true;
        
        for (var i = 0; i < prices.Count - 1; i++)
        {
            if (prices[i] < prices[i + 1])
            {
                return false;
            }
        }
        
        return true;
    }

    public bool IsProductDetailsPage()
    {
        return Driver!.Url.Contains("inventory-item.html");
    }

    public ProductsPage BackToProducts()
    {
        var backButton = By.Id("back-to-products");
        
        WaitForElementClickable(backButton);
        Driver!.FindElement(backButton).Click();
        WaitForElementVisible(ProductsTitle);
        return this;
    }
}