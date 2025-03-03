using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
public class BiDiExampleTests : TestBase
{
    private LoginPage _loginPage;
    private ProductsPage _productsPage;
    private BiDiHandler _biDiHandler;

    [SetUp]
    public void SetupTest()
    {
        // Chama o Setup da classe base para configurar driver e relatório
        base.Setup();
            
        // Inicializar o BiDiHandler com monitoramento completo
        _biDiHandler = Driver!.SetupBiDiMonitoring(Test, enableNetwork: true);
        _biDiHandler.EnableFullMonitoring();
        
        LogInfo("BiDiHandler configurado com sucesso");
            
        // Inicializar páginas
        _loginPage = new LoginPage(Driver);
        _productsPage = new ProductsPage(Driver);
    }

    [TearDown]
    public new void TearDown()
    {
        try
        {
            // Processar resultados do BiDi antes de finalizar o teste
            {
                var testName = TestContext.CurrentContext.Test.Name;
                _biDiHandler.ProcessBiDiResults(testName);
                LogInfo("Processamento BiDi concluído");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Erro ao processar resultados BiDi: {ex.Message}");
        }
        finally
        {
            // Chama o TearDown da classe base para finalizar o teste
            base.TearDown();
        }
    }

    [Test]
    [Description("Testa login com BiDi ativado para monitorar requisições e console")]
    public void Login_Com_Monitoramento_BiDi()
    {
        // Arrange - Navegar para a página de login
        LogStep("Navegando para a página de login", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Fazer login com usuário padrão
        LogStep("Realizando login", () => { 
            var productsPage = _loginPage.Login("standard_user", "secret_sauce");
            
            // Aguardar o carregamento completo da página
            Thread.Sleep(1000);

            // Verificar se o login foi bem-sucedido
            Assert.That(productsPage.IsOnProductsPage(), Is.True, 
                "Login não redirecionou para a página de produtos");
        });
        
        // Assert - Verificar informações capturadas pelo BiDi
        LogStep("Verificando métricas e requisições de rede", () => {
            // Verificar se não houve erros de JavaScript
            var jsErrors = _biDiHandler.CollectJavaScriptErrors();
            Assert.That(jsErrors.Count, Is.EqualTo(0), 
                $"Foram encontrados {jsErrors.Count} erros de JavaScript durante o teste");
            
            LogPass("Login realizado com sucesso e monitorado pelo BiDi");
        });
    }
    
    [Test]
    [Description("Testa login inválido com monitoramento de erros")]
    public void Login_Invalido_Com_Monitoramento_BiDi()
    {
        // Arrange - Navegar para a página de login
        LogStep("Navegando para a página de login", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Tentar login com credenciais inválidas
        LogStep("Tentando login com credenciais inválidas", () => { 
            _loginPage.Login("invalid_user", "invalid_password");
            
            // Aguardar a exibição da mensagem de erro
            Thread.Sleep(500);
        });
        
        // Assert - Verificar mensagem de erro e informações BiDi
        LogStep("Verificando mensagem de erro e requisições de rede", () => {
            // Verificar se permaneceu na página de login
            Assert.That(_loginPage.IsOnLoginPage(), Is.True, 
                "Usuário saiu da página de login, o que não era esperado");
            
            // Verificar mensagem de erro
            string errorMessage = _loginPage.GetErrorMessage();
            Assert.That(errorMessage, Contains.Substring("Username and password do not match"), 
                "Mensagem de erro não corresponde ao esperado");
            
            LogPass("Teste de login inválido concluído com sucesso");
        });
    }
    
    [Test]
    [Description("Testa login com usuário bloqueado")]
    public void Login_Usuario_Bloqueado_Com_BiDi()
    {
        // Arrange - Navegar para a página de login
        LogStep("Navegando para a página de login", () => {
            _loginPage.NavigateToLoginPage();
        });

        // Act - Tentar login com usuário bloqueado
        LogStep("Tentando login com usuário bloqueado", () => { 
            _loginPage.Login("locked_out_user", "secret_sauce");
            
            // Aguardar a exibição da mensagem de erro
            Thread.Sleep(500);
        });
        
        // Assert - Verificar mensagem específica para usuário bloqueado
        LogStep("Verificando mensagem de erro para usuário bloqueado", () => {
            // Verificar se permaneceu na página de login
            Assert.That(_loginPage.IsOnLoginPage(), Is.True, 
                "Usuário saiu da página de login, o que não era esperado");
            
            // Verificar mensagem de erro específica
            string errorMessage = _loginPage.GetErrorMessage();
            Assert.That(errorMessage, Is.EqualTo("Epic sadface: Sorry, this user has been locked out."), 
                "Mensagem de erro para usuário bloqueado incorreta");
            
            // Verificar se houve erros de JavaScript durante o bloqueio
            var jsErrors = _biDiHandler.CollectJavaScriptErrors();
            Assert.That(jsErrors.Count, Is.EqualTo(0), 
                "Foram encontrados erros de JavaScript durante o bloqueio de usuário");
            
            LogPass("Teste de usuário bloqueado concluído com sucesso");
        });
    }
    
    [Test]
    [Description("Testa performance do login")]
    public void Login_Performance_Com_BiDi()
    {
        // Arrange
        LogStep("Preparando teste de performance", () => {
            _loginPage.NavigateToLoginPage();
            
            // Garantir que o monitoramento de performance está ativo
            if (!_biDiHandler.TestDevToolsConnectivity())
            {
                Assert.Ignore("DevTools não disponível para monitoramento de performance");
            }
        });

        // Act
        LogStep("Medindo performance do login", () => {
            // Iniciar um cronômetro manual para comparação
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Realizar login com usuário de glitch de performance
            _loginPage.Login("performance_glitch_user", "secret_sauce");
            
            // Aguardar o carregamento completo da página
            Thread.Sleep(2000);
            
            // Parar o cronômetro
            stopwatch.Stop();
            var tempoDecorrido = stopwatch.ElapsedMilliseconds;
            
            // Registrar o tempo no relatório
            LogInfo($"Tempo de login para performance_glitch_user: {tempoDecorrido}ms");
            
            // Verificar redirecionamento para página de produtos
            Assert.That(_productsPage.IsOnProductsPage(), Is.True, 
                "Login não redirecionou para a página de produtos");
            
            // Verificar se o tempo é significativamente mais lento (>2s)
            Assert.That(tempoDecorrido, Is.GreaterThan(2000), 
                "O tempo de login não foi significativamente mais lento como esperado");
        });
        
        // Assert
        LogPass("Teste de performance de login concluído com sucesso");
    }
}