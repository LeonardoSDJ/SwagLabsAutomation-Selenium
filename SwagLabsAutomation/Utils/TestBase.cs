using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SwagLabsAutomation.Utils
{
    public class TestBase : IDisposable
    {
        protected IWebDriver Driver;

        [SetUp]
        public void Setup()
        {
            // Configuração automática do driver
            new DriverManager().SetUpDriver(new ChromeConfig());

            // Inicialização do driver
            Driver = new ChromeDriver();
            Driver.Manage().Window.Maximize();
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        public void Dispose()
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
    }
}