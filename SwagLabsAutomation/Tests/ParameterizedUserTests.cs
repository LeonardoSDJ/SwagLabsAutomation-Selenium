using SwagLabsAutomation.Models;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests
{
    [TestFixture]
    public class ParameterizedUserTests : TestBase
    {
        private LoginPage _loginPage;
        private UserPerformanceTracker? _tracker;

        [SetUp]
        public new void Setup()
        {
            _loginPage = new LoginPage(Driver);
            _loginPage.GoTo();
        }

        // Dados de teste - pares de usuário e resultado esperado do login
        public static IEnumerable<TestCaseData> LoginTestCases
        {
            get
            {
                yield return new TestCaseData(UserModel.Standard, true, null)
                    .SetName("Standard_User_Can_Login");

                yield return new TestCaseData(UserModel.LockedOut, false, "Epic sadface: Sorry, this user has been locked out.")
                    .SetName("LockedOut_User_Cannot_Login");

                yield return new TestCaseData(UserModel.Problem, true, null)
                    .SetName("Problem_User_Can_Login_With_UI_Issues");

                yield return new TestCaseData(UserModel.PerformanceGlitch, true, null)
                    .SetName("Performance_Glitch_User_Can_Login_Slowly");
            }
        }

        [Test, TestCaseSource(nameof(LoginTestCases))]
        [Description("Testa comportamento de login para diferentes tipos de usuário")]
        public void User_Login_Test(UserModel user, bool shouldSucceed, string expectedErrorMessage)
        {
            // Arrange
            _tracker = new UserPerformanceTracker(Driver, user.Username, ExtentReportManager.GetInstance());

            // Act
            _tracker.StartTracking("login");
            var productsPage = _loginPage.Login(user.Username, user.Password);
            var elapsedTime = _tracker.StopTracking("login");

            // Assert
            if (shouldSucceed)
            {
                Assert.That(productsPage.IsOnProductsPage(), Is.True,
                    $"O usuário {user.Username} deveria fazer login com sucesso");

                // Verificação adicional para performance_glitch_user
                if (user.Type == UserType.PerformanceGlitch)
                {
                    Assert.That(elapsedTime, Is.GreaterThan(2000),
                        "O login com performance_glitch_user deveria ser mais lento");

                    _tracker.LogUserBehavior("Performance Lenta",
                        $"Login demorou {elapsedTime}ms, significativamente mais lento que o normal");
                }
            }
            else
            {
                Assert.That(_loginPage.GetErrorMessage(), Is.EqualTo(expectedErrorMessage),
                    $"Mensagem de erro para {user.Username} não corresponde ao esperado");

                _tracker.LogUserBehavior("Login Bloqueado",
                    $"Usuário bloqueado tentou fazer login e recebeu a mensagem: {_loginPage.GetErrorMessage()}");
            }
        }

        // Dados de teste para verificação de UI por tipo de usuário
        public static IEnumerable<TestCaseData>  UiTestCaseDatas
        {
            get
            {
                yield return new TestCaseData(UserModel.Standard, false)
                    .SetName("Standard_User_Shows_Correct_Images");

                yield return new TestCaseData(UserModel.Problem, true)
                    .SetName("Problem_User_Shows_Incorrect_Images");
            }
        }

        [Test, TestCaseSource(nameof(UiTestCaseDatas))]
        [Description("Testa comportamento de UI para diferentes tipos de usuário")]
        public void User_UI_Test(UserModel user, bool shouldHaveSameImages)
        {
            // Arrange - fazer login
            _tracker = new UserPerformanceTracker(Driver, user.Username, ExtentReportManager.GetInstance());
            var productsPage = _loginPage.Login(user.Username, user.Password);

            // Act
            _tracker.StartTracking("verificar_ui");
            bool allImagesAreSame = productsPage.AreAllProductImagesTheSame();
            _tracker.StopTracking("verificar_ui");

            // Assert
            Assert.That(allImagesAreSame, Is.EqualTo(shouldHaveSameImages),
                shouldHaveSameImages
                    ? "As imagens deveriam ser todas iguais para este tipo de usuário"
                    : "As imagens não deveriam ser todas iguais para este tipo de usuário");

            if (shouldHaveSameImages)
            {
                _tracker.LogUserBehavior("Problema de UI",
                    "Todas as imagens dos produtos são idênticas, indicando problema de UI conhecido");
            }
        }

        // Dados de teste para checkout por tipo de usuário
        public static IEnumerable<TestCaseData> CheckoutTestCases
        {
            get
            {
                yield return new TestCaseData(UserModel.Standard, true)
                    .SetName("Standard_User_Can_Complete_Checkout");

                yield return new TestCaseData(UserModel.Problem, false)
                    .SetName("Problem_User_Cannot_Complete_Checkout");

                yield return new TestCaseData(UserModel.PerformanceGlitch, true)
                    .SetName("Performance_Glitch_User_Can_Complete_Checkout_Slowly");
            }
        }

        [Test, TestCaseSource(nameof(CheckoutTestCases))]
        [Description("Testa fluxo de checkout para diferentes tipos de usuário")]
        public void User_Checkout_Test(UserModel user, bool shouldComplete)
        {
            // Ignorar teste para usuário bloqueado
            if (user.Type == UserType.LockedOut)
            {
                Assert.Ignore("Teste de checkout ignorado para usuário bloqueado");
                return;
            }

            // Arrange - login e adicionar produto ao carrinho
            _tracker = new UserPerformanceTracker(Driver, user.Username, ExtentReportManager.GetInstance());
            var productsPage = _loginPage.Login(user.Username, user.Password);

            _tracker.StartTracking("checkout_completo");

            // Adicionar produto ao carrinho
            productsPage.AddProductToCart("Sauce Labs Backpack");
            productsPage.GoToCart();

            var cartPage = new CartPage(Driver);
            cartPage.GoToCheckout();

            // Tentar completar o checkout
            var checkoutPage = new CheckoutPage(Driver);
            checkoutPage.FillPersonalInfo("Test", "User", "12345");

            bool hasErrors = checkoutPage.HasFormErrors();

            if (!hasErrors)
            {
                checkoutPage.ClickContinue();
                checkoutPage.CompleteCheckout();
            }

            var elapsedTime = _tracker.StopTracking("checkout_completo");

            // Assert - verificar se o checkout foi concluído conforme esperado
            if (shouldComplete)
            {
                Assert.That(checkoutPage.IsOrderComplete(), Is.True,
                    $"O usuário {user.Username} deveria conseguir completar o checkout");

                if (user.Type == UserType.PerformanceGlitch)
                {
                    Assert.That(elapsedTime, Is.GreaterThan(3000),
                        "O checkout com performance_glitch_user deveria ser mais lento");

                    _tracker.LogUserBehavior("Checkout Lento",
                        $"Checkout completo demorou {elapsedTime}ms, significativamente mais lento que o normal");
                }
            }
            else
            {
                Assert.That(hasErrors || !checkoutPage.IsOrderComplete(), Is.True,
                    $"O usuário {user.Username} não deveria conseguir completar o checkout");

                _tracker.LogUserBehavior("Problemas no Checkout",
                    "Não foi possível completar o checkout devido a problemas com o formulário");
            }
        }
    }
}