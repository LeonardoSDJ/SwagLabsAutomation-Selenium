using OpenQA.Selenium;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
public class ParameterizedUserTests : TestBase
{
    private LoginPage _paginaLogin;
    private UserPerformanceTracker? _rastreador;
       
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        DriverFactory.QuitDriver();
    }
       
    [SetUp]
    public void ConfigurarTeste()
    {
        // Chame o Setup da classe base primeiro
        base.Setup();
   
        _paginaLogin = new LoginPage(Driver);
        LogInfo("Navegando para a página de login");
        _paginaLogin.NavigateToLoginPage();
        LogInfo("Página de login carregada");
    }

    [TearDown]
    public new void TearDown()
    {
        try
        {
            _rastreador = null;
            base.TearDown();
            Thread.Sleep(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no TearDown: {ex.Message}");
        }
    }
       
    [OneTimeTearDown]
    public void FinalCleanup()
    {
        DriverFactory.QuitDriver();
        Thread.Sleep(1000);
    }

    // Dados de teste - pares de usuário e resultado esperado do login
    public static IEnumerable<TestCaseData> CasosTesteLogin
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, true, null)
                .SetName("Usuario_Padrao_Pode_Fazer_Login");

            yield return new TestCaseData(UserModel.LockedOut, false, "Epic sadface: Sorry, this user has been locked out.")
                .SetName("Usuario_Bloqueado_Nao_Pode_Fazer_Login");

            yield return new TestCaseData(UserModel.Problem, true, null)
                .SetName("Usuario_Problema_Pode_Fazer_Login_Com_Problemas_UI");

            yield return new TestCaseData(UserModel.PerformanceGlitch, true, null)
                .SetName("Usuario_Performance_Pode_Fazer_Login_Lentamente");
        }
    }

    [Test, TestCaseSource(nameof(CasosTesteLogin))]
    [Description("Testa comportamento de login para diferentes tipos de usuário")]
    public void Teste_Login_Usuario(UserModel usuario, bool deveTerSucesso, string mensagemErroEsperada)
    {
        // Arrange
        LogInfo($"Testando login para usuário: {usuario.Username} ({usuario.Type})");
        _rastreador = new UserPerformanceTracker(Driver, usuario.Username, Test);

        // Act
        LogStep("Iniciando login", () => {
            _rastreador.StartTracking("login");
            var paginaProdutos = _paginaLogin.Login(usuario.Username, usuario.Password);
            var tempoDecorrido = _rastreador.StopTracking("login");
               
            // Assert
            if (deveTerSucesso)
            {
                LogInfo("Verificando redirecionamento para a página de produtos");
                bool estaNaPaginaProdutos = paginaProdutos.IsOnProductsPage();
                Assert.That(estaNaPaginaProdutos, Is.True,
                    $"O usuário {usuario.Username} deveria fazer login com sucesso");

                if (estaNaPaginaProdutos)
                    LogPass($"Login bem-sucedido para {usuario.Username}");

                // Verificação adicional para performance_glitch_user
                if (usuario.Type == UserType.PerformanceGlitch)
                {
                    LogInfo($"Verificando tempo de resposta para usuário com glitch: {tempoDecorrido}ms");
                    Assert.That(tempoDecorrido, Is.GreaterThan(2000),
                        "O login com performance_glitch_user deveria ser mais lento");

                    if (tempoDecorrido > 2000)
                        LogPass($"Comportamento de lentidão confirmado: {tempoDecorrido}ms");

                    _rastreador.LogUserBehavior("Performance Lenta",
                        $"Login demorou {tempoDecorrido}ms, significativamente mais lento que o normal");
                }
            }
            else
            {
                LogInfo("Verificando mensagem de erro para usuário bloqueado");
                string mensagemErroObtida = _paginaLogin.GetErrorMessage();
                LogInfo($"Mensagem obtida: '{mensagemErroObtida}'");
                   
                Assert.That(mensagemErroObtida, Is.EqualTo(mensagemErroEsperada),
                    $"Mensagem de erro para {usuario.Username} não corresponde ao esperado");

                if (mensagemErroObtida == mensagemErroEsperada)
                    LogPass("Mensagem de erro verificada com sucesso");

                _rastreador.LogUserBehavior("Login Bloqueado",
                    $"Usuário bloqueado tentou fazer login e recebeu a mensagem: {mensagemErroObtida}");
            }
        });
    }

    // Dados de teste para verificação de UI por tipo de usuário
    public static IEnumerable<TestCaseData> CasosTesteUi
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, false)
                .SetName("Usuario_Padrao_Mostra_Imagens_Corretas");

            yield return new TestCaseData(UserModel.Problem, true)
                .SetName("Usuario_Problema_Mostra_Imagens_Incorretas");
        }
    }

    [Test, TestCaseSource(nameof(CasosTesteUi))]
    [Description("Testa comportamento de UI para diferentes tipos de usuário")]
    public void Teste_UI_Usuario(UserModel usuario, bool deveTerMesmasImagens)
    {
        // Arrange - fazer login
        LogInfo($"Testando UI para usuário: {usuario.Username} ({usuario.Type})");
        _rastreador = new UserPerformanceTracker(Driver, usuario.Username, Test);
           
        LogStep("Realizando login", () => {
            var paginaProdutos = _paginaLogin.Login(usuario.Username, usuario.Password);
               
            // Act
            LogInfo("Verificando comportamento das imagens dos produtos");
            _rastreador.StartTracking("verificar_ui");
            bool todasImagensSaoIguais = paginaProdutos.AreAllProductImagesTheSame();
            _rastreador.StopTracking("verificar_ui");

            // Assert
            LogInfo($"Resultado da verificação: todas as imagens são iguais = {todasImagensSaoIguais}");
            Assert.That(todasImagensSaoIguais, Is.EqualTo(deveTerMesmasImagens),
                deveTerMesmasImagens
                    ? "As imagens deveriam ser todas iguais para este tipo de usuário"
                    : "As imagens não deveriam ser todas iguais para este tipo de usuário");

            if (todasImagensSaoIguais == deveTerMesmasImagens)
                LogPass("Comportamento de imagens verificado com sucesso");
            else
                LogFail("Comportamento de imagens diferente do esperado");

            if (deveTerMesmasImagens && todasImagensSaoIguais)
            {
                _rastreador.LogUserBehavior("Problema de UI",
                    "Todas as imagens dos produtos são idênticas, indicando problema de UI conhecido");
            }
        });
    }

    // Dados de teste para checkout por tipo de usuário
    public static IEnumerable<TestCaseData> CasosTesteCheckout
    {
        get
        {
            yield return new TestCaseData(UserModel.Standard, true)
                .SetName("Usuario_Padrao_Pode_Completar_Checkout");

            yield return new TestCaseData(UserModel.Problem, false)
                .SetName("Usuario_Problema_Nao_Pode_Completar_Checkout");

            yield return new TestCaseData(UserModel.PerformanceGlitch, true)
                .SetName("Usuario_Performance_Pode_Completar_Checkout_Lentamente");
        }
    }

    [Test, TestCaseSource(nameof(CasosTesteCheckout))]
    [Description("Testa fluxo de checkout para diferentes tipos de usuário")]
    public void Teste_Checkout_Usuario(UserModel usuario, bool deveCompletar)
    {
        try
        {
            // Ignorar teste para usuário bloqueado
            if (usuario.Type == UserType.LockedOut)
            {
                LogInfo("Teste de checkout ignorado para usuário bloqueado");
                Assert.Ignore("Teste de checkout ignorado para usuário bloqueado");
                return;
            }

            // Arrange - login e adicionar produto ao carrinho
            LogInfo($"Testando checkout para usuário: {usuario.Username} ({usuario.Type})");
            _rastreador = new UserPerformanceTracker(Driver, usuario.Username, Test);
           
            LogStep("Realizando processo de checkout", () => {
                var paginaProdutos = _paginaLogin.Login(usuario.Username, usuario.Password);
                LogInfo("Login realizado com sucesso");

                _rastreador.StartTracking("checkout_completo");

                // Adicionar produto ao carrinho
                LogInfo("Adicionando produto ao carrinho");
                paginaProdutos.AddProductToCart("sauce-labs-backpack");
                paginaProdutos.GoToCart();
                LogInfo("Produto adicionado e navegando para o carrinho");

                var paginaCarrinho = new CartPage(Driver);
                paginaCarrinho.GoToCheckout();
                LogInfo("Iniciando checkout");

                // Tentar completar o checkout
                var paginaCheckout = new CheckoutPage(Driver);
                LogInfo("Preenchendo informações pessoais");
                paginaCheckout.FillPersonalInfo("Teste", "Usuario", "12345");

                bool temErros = paginaCheckout.HasFormErrors();
                LogInfo($"Formulário possui erros: {temErros}");

                if (!temErros)
                {
                    LogInfo("Continuando com o checkout");
                    paginaCheckout.ClickContinue();
                    paginaCheckout.CompleteCheckout();
                    LogInfo("Checkout finalizado");
                }

                var tempoDecorrido = _rastreador.StopTracking("checkout_completo");

                // Assert - verificar se o checkout foi concluído conforme esperado
                if (deveCompletar)
                {
                    bool pedidoCompleto = paginaCheckout.IsOrderComplete();
                    LogInfo($"Pedido completo: {pedidoCompleto}");
                   
                    Assert.That(pedidoCompleto, Is.True,
                        $"O usuário {usuario.Username} deveria conseguir completar o checkout");

                    if (pedidoCompleto)
                        LogPass("Checkout completado com sucesso");
                    else
                        LogFail("Checkout não foi completado como esperado");

                    if (usuario.Type == UserType.PerformanceGlitch)
                    {
                        LogInfo($"Verificando tempo de resposta para checkout: {tempoDecorrido}ms");
                        Assert.That(tempoDecorrido, Is.GreaterThan(3000),
                            "O checkout com performance_glitch_user deveria ser mais lento");

                        if (tempoDecorrido > 3000)
                            LogPass($"Comportamento de lentidão confirmado: {tempoDecorrido}ms");

                        _rastreador.LogUserBehavior("Checkout Lento",
                            $"Checkout completo demorou {tempoDecorrido}ms, significativamente mais lento que o normal");
                    }
                }
                else
                {
                    bool checkoutFalhou = temErros || !paginaCheckout.IsOrderComplete();
                    LogInfo($"Checkout falhou conforme esperado: {checkoutFalhou}");
                   
                    Assert.That(checkoutFalhou, Is.True,
                        $"O usuário {usuario.Username} não deveria conseguir completar o checkout");

                    if (checkoutFalhou)
                        LogPass("Comportamento de checkout para usuário com problemas verificado com sucesso");
                    else
                        LogFail("Checkout foi completado, o que não era esperado");

                    _rastreador.LogUserBehavior("Problemas no Checkout",
                        "Não foi possível completar o checkout devido a problemas com o formulário");
                }
            });
        }
        catch (WebDriverTimeoutException tex)
        {
            // Tratamento especial para timeouts
            LogInfo($"Timeout detectado durante o teste: {tex.Message}");
            LogFail($"Falha por timeout: {tex.Message}");
               
            // Falhar o teste de forma controlada
            Assert.Fail($"Timeout durante o teste com {usuario.Username}: {tex.Message}");
        }
        catch (Exception ex)
        {
            // Tratamento para outras exceções
            LogInfo($"Exceção detectada durante o teste: {ex.Message}");
            LogFail($"Falha por exceção: {ex.Message}");
               
            // Falhar o teste de forma controlada
            Assert.Fail($"Erro durante o teste com {usuario.Username}: {ex.Message}");
        }
    }
}