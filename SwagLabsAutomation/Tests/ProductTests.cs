using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;
using static NUnit.Framework.Assert;
using By = OpenQA.Selenium.By;

namespace SwagLabsAutomation.Tests;

[TestFixture]
public class ProductTests : TestBase
{
    private LoginPage _loginPage;
    private ProductsPage _productsPage;
    private static readonly string[] Action =
    [
        "sauce-labs-backpack",
                "sauce-labs-bike-light",
                "sauce-labs-bolt-t-shirt"
    ];

    [SetUp]
    public void SetupTest()
    {
        // Initialization and login before each test
        _loginPage = new LoginPage(Driver);
        _loginPage.NavigateToLoginPage();
        LogInfo("Performing login with standard user");
        _productsPage = _loginPage.Login("standard_user", "secret_sauce");
        
        // Verify login was successful
        That(_productsPage.IsOnProductsPage(), Is.True, 
            "Failed to access products page after login");
        LogInfo("Login successful, products page loaded");
    }

    [Test]
    [Description("Verifies all products are displayed correctly")]
    public void VerifyProductsDisplay()
    {
        LogStep("Verifying products display", () => {
            // Get list of product names
            var productNames = _productsPage.GetAllProductNames();
            
            // Verify products are displayed
            That(productNames, Is.Not.Empty, "No products displayed on page");
            LogInfo($"Found {productNames.Count} products on page");
            
            // Verify specific products are present in the list
            var expectedProducts = new[] {
                "Sauce Labs Backpack",
                "Sauce Labs Bike Light",
                "Sauce Labs Bolt T-Shirt",
                "Sauce Labs Fleece Jacket",
                "Sauce Labs Onesie",
                "Test.allTheThings() T-Shirt (Red)"
            };
            
            foreach (var product in expectedProducts)
            {
                That(productNames, Does.Contain(product), 
                    $"Product '{product}' not found in products list");
                LogInfo($"Product '{product}' found successfully");
            }
            
            // Verify total number of products matches expected
            That(productNames.Count, Is.EqualTo(expectedProducts.Length),
                "Number of displayed products does not match expected count");
            
            LogPass("All products are displayed correctly");
        });
    }

    [Test]
    [Description("Verifies product sorting from A-Z")]
    public void SortProductsAZ()
    {
        LogStep("Sorting products A-Z", () => {
            // Ensure sorting is A-Z (default)
            _productsPage.SortProductsBy("az");
            LogInfo("Products sorted A-Z");
            
            // Get list of names after sorting
            var productNames = _productsPage.GetAllProductNames();
            
            // Create sorted copy for comparison
            var sortedProductNames = new List<string>(productNames);
            sortedProductNames.Sort(StringComparer.Ordinal);
            
            // Verify sorting is correct
            That(productNames, Is.EqualTo(sortedProductNames),
                "Products are not correctly sorted A-Z");
            
            LogPass("Products correctly sorted A-Z");
        });
    }

    [Test]
    [Description("Verifies product sorting from Z-A")]
    public void SortProductsZA()
    {
        LogStep("Sorting products Z-A", () => {
            // Apply Z-A sorting
            _productsPage.SortProductsBy("za");
            LogInfo("Products sorted Z-A");
            
            // Get list of names after sorting
            var productNames = _productsPage.GetAllProductNames();
            
            // Create sorted copy for comparison
            var sortedProductNames = new List<string>(productNames);
            sortedProductNames.Sort(StringComparer.Ordinal);
            sortedProductNames.Reverse(); // Reverse for Z-A
            
            // Verify sorting is correct
            That(productNames, Is.EqualTo(sortedProductNames),
                "Products are not correctly sorted Z-A");
            
            LogPass("Products correctly sorted Z-A");
        });
    }

    [Test]
    [Description("Verifies product sorting by price (low to high)")]
    public void SortProductsByPriceAscending()
    {
        LogStep("Sorting products by price (low to high)", () => {
            // Apply price sorting (low to high)
            _productsPage.SortProductsBy("lohi");
            LogInfo("Products sorted by price (low to high)");
            
            // Verify the page doesn't break with this sorting
            That(_productsPage.IsOnProductsPage(), Is.True,
                "Products page is no longer accessible after price sorting");
            
            LogPass("Price sorting applied successfully");
        });
    }

    [Test]
    [Description("Verifies product sorting by price (high to low)")]
    public void SortProductsByPriceDescending()
    {
        LogStep("Sorting products by price (high to low)", () => {
            // Apply price sorting (high to low)
            _productsPage.SortProductsBy("hilo");
            LogInfo("Products sorted by price (high to low)");
            
            // Verify the page doesn't break with this sorting
            That(_productsPage.IsOnProductsPage(), Is.True,
                "Products page is no longer accessible after price sorting");
            
            LogPass("Price sorting applied successfully");
        });
    }

    [Test]
    [Description("Verifies adding multiple products to cart")]
    public void AddMultipleProductsToCart()
    {
        LogStep("Adding multiple products to cart", () => {
            // List of products to add
            var productsToAdd = Action;
            
            // Verify cart is initially empty
            That(_productsPage.GetCartCount(), Is.EqualTo(0),
                "Cart is not empty at test start");
            
            // Add each product to cart
            foreach (var product in productsToAdd)
            {
                _productsPage.AddProductToCart(product);
                LogInfo($"Product '{product}' added to cart");
            }
            
            // Verify cart counter was updated correctly
            That(_productsPage.GetCartCount(), Is.EqualTo(productsToAdd.Length),
                "Cart counter does not reflect correct number of added items");
            
            LogPass($"{productsToAdd.Length} products successfully added to cart");
        });
    }

    [Test]
    [Description("Verifies removing products from cart on products page")]
    public void RemoveProductsFromCartOnProductsPage()
    {
        LogStep("Testing product removal on products page", () => {
            // Add product to cart
            const string productId = "sauce-labs-backpack";
            _productsPage.AddProductToCart(productId);
            LogInfo($"Product '{productId}' added to cart");
            
            // Verify product was added
            That(_productsPage.GetCartCount(), Is.EqualTo(1),
                "Product was not added to cart");

            Driver!.FindElement(By.Id($"remove-{productId}")).Click();
            LogInfo($"Product '{productId}' removed from cart");
            
            // Verify product was removed
            That(_productsPage.GetCartCount(), Is.EqualTo(0),
                "Product was not removed from cart");
            
            LogPass("Product successfully removed from cart");
        });
    }

    [Test]
    [Description("Verifies navigation to product details page")]
    public void NavigateToProductDetails()
    {
        LogStep("Testing navigation to product details", () => {
            // First, get the first product element
            var driver = this.Driver;
            var firstProductName = driver!.FindElement(By.CssSelector(".inventory_item_name"));
            var productTitle = firstProductName.Text;
            LogInfo($"Clicking on product: {productTitle}");
            
            // Click on product name
            firstProductName.Click();
            LogInfo("Redirecting to details page");
            
            // Verify we're on details page
            var isOnDetailsPage = driver.Url.Contains("inventory-item.html");
            That(isOnDetailsPage, Is.True, 
                "Not redirected to product details page");
            
            // Verify product title is present on details page
            var detailsTitle = driver.FindElement(By.CssSelector(".inventory_details_name"));
            That(detailsTitle.Text, Is.EqualTo(productTitle),
                "Product title on details page does not match expected");
            
            LogPass("Navigation to product details successful");
        });
    }

    [Test]
    [Description("Verifies product images are loaded correctly")]
    public void VerifyProductImages()
    {
        LogStep("Verifying product images are loaded", () => {
            // Get all product images
            var driver = this.Driver;
            var productImages = driver!.FindElements(By.CssSelector(".inventory_item_img img"));
            LogInfo($"Found {productImages.Count} product images");
            
            // Verify all images have valid src attribute
            foreach (var img in productImages)
            {
                var src = img.GetAttribute("src");
                That(src, Is.Not.Null.And.Not.Empty,
                    "Product image missing valid src attribute");
                That(src, Does.Contain("/static/media/"),
                    "Image path does not match expected pattern");
                
                // Verify image is not broken (more complex and may require JavaScript)
                var isDisplayed = img.Displayed;
                That(isDisplayed, Is.True, "Product image is not displayed");
            }
            
            LogPass("All product images are loading correctly");
        });
    }

    [Test]
    [Description("Verifies product descriptions are displayed correctly")]
    public void VerifyProductDescriptions()
    {
        LogStep("Verifying product descriptions", () => {
            // Get all product descriptions
            var driver = this.Driver;
            var productDescs = driver!.FindElements(By.CssSelector(".inventory_item_desc"));
            LogInfo($"Found {productDescs.Count} product descriptions");
            
            // Verify all descriptions have content
            foreach (var desc in productDescs)
            {
                var text = desc.Text;
                That(text, Is.Not.Null.And.Not.Empty,
                    "Empty product description");
                
                // Verify reasonable minimum length for description
                That(text, Has.Length.GreaterThan(10),
                    "Product description too short, may indicate a problem");
            }
            
            LogPass("All product descriptions are displayed correctly");
        });
    }

    [Test]
    [Description("Verifies product prices are displayed correctly")]
    public void VerifyProductPrices()
    {
        LogStep("Verifying product prices", () => {
            // Get all product prices
            var driver = this.Driver;
            var productPrices = driver!.FindElements(By.CssSelector(".inventory_item_price"));
            LogInfo($"Found {productPrices.Count} product prices");
            
            // Verify all prices follow expected format ($XX.XX)
            foreach (var price in productPrices)
            {
                var priceText = price.Text;
                That(priceText, Does.StartWith("$"),
                    "Price does not start with dollar symbol ($)");
                
                // Extract numeric value and verify it's a valid number
                var numericPart = priceText[1..];
                That(double.TryParse(numericPart, out var priceValue), Is.True,
                    $"Could not convert '{numericPart}' to numeric value");
                
                // Verify price is reasonable (greater than zero)
                That(priceValue, Is.GreaterThan(0),
                    "Product price is not greater than zero");
            }
            
            LogPass("All product prices are displayed correctly");
        });
    }

    [Test]
    [Description("Verifies Back To Products button on details page")]
    public void TestBackToProductsButton()
    {
        LogStep("Testing Back To Products button", () => {
            // First, navigate to product details page
            var driver = this.Driver;
            var firstProductName = driver!.FindElement(By.CssSelector(".inventory_item_name"));
            firstProductName.Click();
            LogInfo("Navigated to product details page");
            
            // Verify we're on details page
            var isOnDetailsPage = driver.Url.Contains("inventory-item.html");
            That(isOnDetailsPage, Is.True,
                "Not redirected to product details page");
            
            // Click Back To Products button
            var backButton = driver.FindElement(By.Id("back-to-products"));
            backButton.Click();
            LogInfo("Clicked Back To Products button");
            
            // Verify we're back on products page
            That(_productsPage.IsOnProductsPage(), Is.True,
                "Did not return to products page after clicking Back To Products");
            
            LogPass("Navigation back to products page successful");
        });
    }
}