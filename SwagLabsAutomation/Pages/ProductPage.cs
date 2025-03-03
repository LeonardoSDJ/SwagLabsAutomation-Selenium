﻿using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SwagLabsAutomation.Pages;

public class ProductsPage(IWebDriver? driver) : BasePage(driver)
{
    private readonly By _productItems = By.CssSelector(".inventory_item");
    private readonly By _productImages = By.CssSelector(".inventory_item_img img");
    private readonly By _productNames = By.CssSelector(".inventory_item_name");
    private readonly By _addToCartButtons = By.CssSelector("[data-test^='add-to-cart']");
    private readonly By _cartIcon = By.CssSelector(".shopping_cart_link");
    private readonly By _productSortContainer = By.ClassName("product_sort_container");
    private static By ProductsTitle => By.ClassName("title");
    private static By CartBadge => By.ClassName("shopping_cart_badge");
    private static By CartLink => By.ClassName("shopping_cart_link");

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
    public List<string> GetAllProductNames()
    {
        WaitForElementVisible(_productNames);
        var elements = Driver.FindElements(_productNames);
        return elements.Select(e => e.Text).ToList();
    }

    public void RemoveProductFromCart(string productId)
    {
        string removeButtonId = $"remove-{productId}";
        By removeButton = By.Id(removeButtonId);

        WaitForElementClickable(removeButton);
        Driver.FindElement(removeButton).Click();
    }

    public List<double> GetProductPrices()
    {
        var priceElements = Driver.FindElements(By.CssSelector(".inventory_item_price"));
        var prices = new List<double>();
        
        foreach (var element in priceElements)
        {
            string priceText = element.Text;
            // Remove o símbolo $ e converter para double
            if (priceText.StartsWith("$") && double.TryParse(priceText.Substring(1), out double price))
            {
                prices.Add(price);
            }
        }
        
        return prices;
    }

    public void NavigateToProductDetails(int index)
    {
        var productElements = Driver.FindElements(_productNames);
    
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
        var descElements = Driver.FindElements(By.CssSelector(".inventory_item_desc"));
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
        
        for (int i = 0; i < prices.Count - 1; i++)
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
        return Driver.Url.Contains("inventory-item.html");
    }

    public ProductsPage BackToProducts()
    {
        var backButton = By.Id("back-to-products");
        
        WaitForElementClickable(backButton);
        Driver.FindElement(backButton).Click();
        WaitForElementVisible(ProductsTitle);
        return this;
    }
}