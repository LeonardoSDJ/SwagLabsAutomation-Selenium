﻿using OpenQA.Selenium;

namespace SwagLabsAutomation.Pages
{
    public class CheckoutPage : BasePage
    {
        private By _errorMessage = By.CssSelector("[data-test='error']");
        private By _completeHeader = By.CssSelector(".complete-header");

        // Locators - Step One (Informações Pessoais)
        private By FirstNameField => By.Id("first-name");
        private By LastNameField => By.Id("last-name");
        private By PostalCodeField => By.Id("postal-code");
        private By ContinueButton => By.Id("continue");
        private By CancelButton => By.Id("cancel");

        // Locators - Step Two (Revisão)
        private By FinishButton => By.Id("finish");
        private By SummaryInfoContainer => By.ClassName("summary_info");

        // Locators - Complete
        private By CompleteHeader => By.ClassName("complete-header");
        private By BackToProductsButton => By.Id("back-to-products");

        public CheckoutPage(IWebDriver driver) : base(driver) { }

        public bool IsOnCheckoutStepOne()
        {
            return Driver.Url.Contains("checkout-step-one.html") && IsElementDisplayed(FirstNameField);
        }

        public bool IsOnCheckoutStepTwo()
        {
            return Driver.Url.Contains("checkout-step-two.html") && IsElementDisplayed(FinishButton);
        }

        public bool IsOnCheckoutComplete()
        {
            return Driver.Url.Contains("checkout-complete.html") && IsElementDisplayed(CompleteHeader);
        }

        public CheckoutPage FillPersonalInfo(string firstName, string lastName, string postalCode)
        {
            WaitForElementVisible(FirstNameField);
            Driver.FindElement(FirstNameField).SendKeys(firstName);
            Driver.FindElement(LastNameField).SendKeys(lastName);
            Driver.FindElement(PostalCodeField).SendKeys(postalCode);

            ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile("Checkout_Fill_Info.png");

            return this;
        }

        public CheckoutPage ClickContinue()
        {
            WaitForElementClickable(ContinueButton);
            Driver.FindElement(ContinueButton).Click();

            return this;
        }

        public CartPage ClickCancel()
        {
            WaitForElementClickable(CancelButton);
            Driver.FindElement(CancelButton).Click();

            return new CartPage(Driver);
        }

        public CheckoutPage CompleteCheckout()
        {
            if (IsOnCheckoutStepTwo())
            {
                WaitForElementClickable(FinishButton);
                Driver.FindElement(FinishButton).Click();
            }

            return this;
        }

        public ProductsPage GoBackToProducts()
        {
            if (IsOnCheckoutComplete())
            {
                WaitForElementClickable(BackToProductsButton);
                Driver.FindElement(BackToProductsButton).Click();
            }

            return new ProductsPage(Driver);
        }

        public double GetTotalPrice()
        {
            if (IsOnCheckoutStepTwo())
            {
                By totalPriceLocator = By.ClassName("summary_total_label");
                string totalText = Driver.FindElement(totalPriceLocator).Text;
                // Extrai o valor numérico de um texto como "Total: $32.39"
                return double.Parse(totalText.Split('$')[1]);
            }

            return 0;
        }
        public bool HasFormErrors()
        {
            try
            {
                return Driver.FindElements(_errorMessage).Count > 0;
            }
            catch
            {
                return false;
            }
        }
        public bool IsOrderComplete()
        {
            try
            {
                WaitForElementVisible(_completeHeader);
                return Driver.FindElement(_completeHeader).Displayed;
            }
            catch
            {
                return false;
            }
        }
    }
}