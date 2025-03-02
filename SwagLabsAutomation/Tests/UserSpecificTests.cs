using OpenQA.Selenium;
using SwagLabsAutomation.Pages;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation.Tests;

[TestFixture]
public class TesteUsuariosEspecificos : TestBase
{
    private LoginPage _paginaLogin;
    private ProductsPage _paginaProdutos;
    private CartPage _paginaCarrinho;

    [SetUp]
    public void ConfigurarTeste()
    {
        // Chama o Setup da classe base para configurar driver e relatório
        Setup();
            
        _paginaLogin = new LoginPage(Driver);
        _paginaProdutos = new ProductsPage(Driver);
        _paginaCarrinho = new CartPage(Driver);
            
        AddTestMetadata();
    }

    [Test]
    [Description("Verifica se o locked_out_user não consegue fazer login")]
    public void UsuarioBloqueado_NaoConsegueFazerLogin()
    {
        // Arrange - Navegar para a página de login
        LogStep("Navegando para a página de login", () => {
            _paginaLogin.NavigateToLoginPage();
        });

        // Act - Tentar fazer login com locked_out_user
        LogStep("Tentando login com usuário bloqueado", () => {
            _paginaLogin.Login("locked_out_user", "secret_sauce");
        });

        // Assert - Verificar se a mensagem de erro específica para usuário bloqueado é exibida
        LogStep("Verificando mensagem de erro para usuário bloqueado", () => {
            var mensagemErro = _paginaLogin.GetErrorMessage();
            LogInfo($"Mensagem de erro obtida: '{mensagemErro}'");
                
            Assert.That(mensagemErro, Is.EqualTo("Epic sadface: Sorry, this user has been locked out."),
                "Mensagem de erro para usuário bloqueado não foi exibida corretamente");

            // Verificar que o usuário não foi redirecionado para a página de produtos
            LogInfo("Verificando que usuário permanece na página de login");
            if (Driver != null)
                Assert.That(Driver.Url, Does.Not.Contain("/inventory.html"),
                    "Usuário bloqueado conseguiu fazer login, o que não deveria acontecer");

            LogPass("Teste de usuário bloqueado concluído com sucesso");
        });
    }

    [Test]
    [Description("Verifica comportamentos específicos do problem_user")]
    public void UsuarioProblema_MostraImagensProdutosIncorretas()
    {
        // Arrange - Login com problem_user
        LogStep("Realizando login com usuário problema", () => {
            _paginaLogin.NavigateToLoginPage();
            _paginaLogin.Login("problem_user", "secret_sauce");
            LogInfo("Login realizado com usuário problema");
        });

        // Act & Assert - Verificar se todas as imagens dos produtos são iguais (comportamento conhecido do problem_user)
        LogStep("Verificando comportamento das imagens", () => {
            string primeiraImagem = _paginaProdutos.GetFirstProductImageSrc();
            LogInfo($"URL da primeira imagem: {primeiraImagem}");
                
            bool todasImagensIguais = _paginaProdutos.AreAllProductImagesTheSame();
            LogInfo($"Todas as imagens são iguais: {todasImagensIguais}");
                
            Assert.That(todasImagensIguais, Is.True,
                "As imagens dos produtos para problem_user deveriam ser todas iguais");
                
            if (todasImagensIguais)
                LogPass("Comportamento de imagens para usuário problema verificado com sucesso");
        });
    }

    [Test]
    [Description("Verifica problemas de preenchimento de formulário do problem_user")]
    public void UsuarioProblema_NaoConsegueCompletarCheckout()
    {
        // Arrange - Login com problem_user e adicionar um produto ao carrinho
        LogStep("Preparando teste de checkout para usuário problema", () => {
            _paginaLogin.NavigateToLoginPage();
            _paginaLogin.Login("problem_user", "secret_sauce");
        
            _paginaProdutos.AddProductToCart("sauce-labs-backpack");
            _paginaProdutos.GoToCart();
        });

        // Act - Iniciar checkout
        LogStep("Iniciando processo de checkout", () => {
            _paginaCarrinho.GoToCheckout();
        
            // Tentar preencher o formulário
            var paginaCheckout = new CheckoutPage(Driver);
        
            // O problema específico do problem_user é que ele não consegue inserir texto no campo de sobrenome
            // Vamos verificar isso diretamente:
            paginaCheckout.FillPersonalInfo("Teste", "Usuario", "12345");
        
            // Verificar diretamente o valor do campo de sobrenome
            var sobrenomeValue = Driver.FindElement(By.Id("last-name")).GetAttribute("value");
        
            // Assert - Verificar se o campo de sobrenome não foi preenchido corretamente
            Assert.That(sobrenomeValue, Is.Not.EqualTo("Usuario"), 
                "O campo de sobrenome deveria estar vazio ou incorreto para o problem_user");
        
            if (sobrenomeValue != "Usuario")
                LogPass("Comportamento verificado: campo de sobrenome não aceita entrada corretamente");
        });
    }

    [Test]
    [Description("Verifica problemas específicos do problem_user com ordenação")]
    public void UsuarioProblema_OrdenacaoNaoFunciona()
    {
        // Arrange - Login com problem_user
        LogStep("Realizando login com usuário problema", () => {
            _paginaLogin.NavigateToLoginPage();
            _paginaLogin.Login("problem_user", "secret_sauce");
        });

        // Act e Assert - Verificar como os produtos aparecem após seleção do filtro
        LogStep("Testando ordenação", () => {
            // Capturar os nomes dos produtos antes da ordenação
            var nomesAntes = _paginaProdutos.GetAllProductNames();
        
            // Tentar ordenar produtos por nome (Z-A)
            Driver?.FindElement(By.ClassName("product_sort_container")).Click();
            Driver?.FindElement(By.CssSelector("option[value='za']")).Click();
        
            // Dar tempo para a página responder (adicionar um pequeno delay)
            Thread.Sleep(1000);
        
            // Capturar os nomes após a ordenação
            var nomesDepois = _paginaProdutos.GetAllProductNames();
        
            // Para o problem_user, os nomes não devem estar ordenados corretamente
            var ordenacaoFuncionou = true;
        
            // Verificar se a ordem está revertida (Z-A)
            for (int i = 0; i < nomesAntes.Count - 1; i++)
            {
                if (String.CompareOrdinal(nomesDepois[i], nomesDepois[i + 1]) >= 0) continue;
                ordenacaoFuncionou = false;
                break;
            }
        
            Assert.That(ordenacaoFuncionou, Is.False, 
                "A ordenação para problem_user não deveria funcionar corretamente");
        
            LogInfo($"Ordenação funcionou corretamente? {ordenacaoFuncionou}");
        
            if (!ordenacaoFuncionou)
                LogPass("Comportamento de ordenação incorreta verificado com sucesso");
        });
    }

    [Test]
    [Description("Verifica tempo de carregamento do performance_glitch_user")]
    public void UsuarioPerformance_TemCarregamentoLento()
    {
        // Arrange - Iniciar temporizador
        LogStep("Testando tempo de carregamento para usuário com glitch de performance", () => {
            LogInfo("Iniciando cronômetro");
            var cronometro = System.Diagnostics.Stopwatch.StartNew();

            // Act - Fazer login com performance_glitch_user
            _paginaLogin.NavigateToLoginPage();
            LogInfo("Realizando login com usuário performance_glitch_user");
            _paginaLogin.Login("performance_glitch_user", "secret_sauce");

            // Parar o temporizador após o carregamento da página de produtos
            cronometro.Stop();
            long tempoDecorrido = cronometro.ElapsedMilliseconds;
            LogInfo($"Tempo de carregamento: {tempoDecorrido}ms");

            // Assert - Verificar se o tempo de carregamento é significativamente maior 
            // (ajuste o valor de acordo com o esperado para sua rede/ambiente)
            Assert.That(tempoDecorrido, Is.GreaterThan(2000),
                "O tempo de carregamento do performance_glitch_user deveria ser mais lento");
                
            if (tempoDecorrido > 2000)
                LogPass($"Comportamento de lentidão confirmado: {tempoDecorrido}ms");
            else
                LogFail($"Tempo de carregamento ({tempoDecorrido}ms) não foi significativamente lento");
        });
    }

    [Test]
    [Description("Verifica que o performance_glitch_user pode completar o fluxo de compra, apesar da performance lenta")]
    public void UsuarioPerformance_ConsegueCompletarCheckout()
    {
        // Arrange - Login com performance_glitch_user
        LogStep("Testando fluxo de checkout para usuário com glitch de performance", () => {
            _paginaLogin.NavigateToLoginPage();
            LogInfo("Realizando login com usuário performance_glitch_user");
            _paginaLogin.Login("performance_glitch_user", "secret_sauce");
            LogInfo("Login realizado com sucesso");

            // Act - Completar o fluxo de compra
            LogInfo("Adicionando produto ao carrinho");
            // Correção: usar o ID do produto em vez do nome
            _paginaProdutos.AddProductToCart("sauce-labs-backpack");
            LogInfo("Navegando para o carrinho");
            _paginaProdutos.GoToCart();
            LogInfo("Iniciando checkout");
            _paginaCarrinho.GoToCheckout();

            var paginaCheckout = new CheckoutPage(Driver);
            LogInfo("Preenchendo informações pessoais");
            paginaCheckout.FillPersonalInfo("Teste", "Usuario", "12345");
            LogInfo("Continuando para próxima etapa");
            paginaCheckout.ClickContinue();
            LogInfo("Finalizando compra");
            paginaCheckout.CompleteCheckout();

            // Assert - Verificar se a compra foi concluída com sucesso
            bool pedidoCompleto = paginaCheckout.IsOrderComplete();
            LogInfo($"Pedido completo: {pedidoCompleto}");
                
            Assert.That(pedidoCompleto, Is.True,
                "O performance_glitch_user deveria conseguir completar o checkout");
                
            if (pedidoCompleto)
                LogPass("Checkout completado com sucesso para usuário performance");
            else
                LogFail("Checkout não foi completado para usuário performance");
        });
    }
}