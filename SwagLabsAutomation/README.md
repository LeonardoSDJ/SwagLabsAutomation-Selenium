# SwagLabs Automation com Selenium, C# e NUnit

Este projeto contém testes automatizados para o site Swag Labs (SauceDemo) utilizando Selenium WebDriver com C# e NUnit. O objetivo é demonstrar a implementação de testes automatizados usando o padrão Page Object Model (POM) e boas práticas de automação.

## Pré-requisitos

- Visual Studio 2022 ou superior / JetBrains Rider
- .NET 9
- Conhecimentos básicos de C#, Selenium e NUnit

## Instalação

1. Clone este repositório
2. Abra a solução no Visual Studio ou Rider
3. Restaure os pacotes NuGet:
   - Selenium.WebDriver
   - Selenium.Support
   - NUnit
   - NUnit3TestAdapter
   - DotNetSeleniumExtras.WaitHelpers
   - WebDriverManager
   - AventStack.ExtentReports

## Estrutura do Projeto

O projeto segue o padrão Page Object Model (POM) para melhor organização e manutenção:

```
SwagLabsAutomation/
├── Pages/
│   ├── BasePage.cs
│   ├── LoginPage.cs
│   ├── ProductsPage.cs
│   ├── CartPage.cs
│   └── CheckoutPage.cs
├── Tests/
│   ├── LoginTests.cs
│   ├── ProductTests.cs
│   ├── CartTests.cs
│   ├── CheckoutTests.cs
│   ├── ParameterizedUserTests.cs
│   └── UserSpecificTests.cs
└── Utils/
    ├── TestBase.cs
    ├── DriverFactory.cs
    ├── ExtentReportManager.cs
    ├── UserModel.cs
    ├── UserPerformanceTracker.cs
    ├── Constants.cs
    └── BiDiHandler.cs
```

### Pages

- **BasePage**: Classe base com métodos comuns para todas as páginas
- **LoginPage**: Página de login com métodos para autenticação
- **ProductsPage**: Página de produtos com métodos para adicionar produtos ao carrinho
- **CartPage**: Página do carrinho com métodos para manipular itens
- **CheckoutPage**: Páginas de checkout com métodos para o fluxo de compra

### Tests

- **LoginTests**: Testes para validar funcionalidades de login
- **ProductTests**: Testes para validar funcionalidades da página de produtos (a implementar)
- **CartTests**: Testes para validar funcionalidades do carrinho
- **CheckoutTests**: Testes para validar o fluxo de checkout
- **ParameterizedUserTests**: Testes parametrizados para diferentes tipos de usuários
- **UserSpecificTests**: Testes específicos para cada tipo de usuário

### Utils

- **TestBase**: Classe base para configuração dos testes
- **DriverFactory**: Gerenciamento de instâncias do WebDriver
- **ExtentReportManager**: Gerenciamento de relatórios com ExtentReports
- **UserModel**: Modelo de dados para diferentes tipos de usuários
- **UserPerformanceTracker**: Rastreador de performance para testes
- **Constants**: Constantes utilizadas no projeto (a implementar)
- **BiDiHandler**: Manipulação BiDi (Chrome DevTools Protocol) (a implementar)

## Testes Implementados

### Login Tests
- Login com credenciais válidas
- Login com credenciais inválidas

### Cart Tests
- Adicionar item ao carrinho
- Adicionar múltiplos itens
- Ir para o carrinho e voltar para compras
- Remover item do carrinho

### Checkout Tests
- Processo de checkout completo
- Verificar total do checkout
- Cancelar checkout
- Voltar para produtos após compra

### Testes de Usuários Específicos
- Verificação de comportamentos específicos para cada tipo de usuário:
   - **standard_user**: Funcionamento normal
   - **locked_out_user**: Bloqueado sem acesso
   - **problem_user**: Problemas de UI e formulários
   - **performance_glitch_user**: Performance lenta

## Execução dos Testes

Para executar os testes:

1. Abra o Test Explorer no Visual Studio ou Rider
2. Selecione os testes que deseja executar
3. Clique em "Run" ou "Run All"

## Próximas Melhorias Planejadas

1. **Completar implementações pendentes**
   - Implementar a classe `ProductTests.cs` para testes de produtos
   - Implementar a manipulação BiDi no arquivo `BiDiHandler.cs`

2. **Suporte a múltiplos navegadores**
   - Expandir o `DriverFactory` para suportar Firefox e Edge
   - Adicionar configuração para seleção de navegador via arquivo de configuração

3. **Testes em paralelo**
   - Configurar NUnit para execução de testes em paralelo
   - Melhorar o isolamento de instâncias do WebDriver

4. **Integração com CI/CD**
   - Configurar GitHub Actions para execução automática dos testes
   - Adicionar configuração para Azure DevOps ou Jenkins
   - Implementar publicação automática de relatórios

5. **Testes de API**
   - Implementar cliente HTTP para testes de API
   - Criar testes para verificar endpoints da API do SauceDemo
   - Integrar testes de API com testes de UI

6. **Testes de Performance**
   - Expandir o `UserPerformanceTracker` para métricas mais detalhadas
   - Implementar benchmark para comparação entre diferentes tipos de usuários
   - Adicionar monitoramento de recursos (CPU, memória)

7. **Melhorias nos relatórios**
   - Adicionar dashboards personalizados no ExtentReports
   - Implementar capturas de vídeo dos testes
   - Melhorar a visualização de logs e screenshots
8. **Melhorias de arquitetura**
   - Implementar injeção de dependência
   - Refatorar para arquitetura mais modular
   - Melhorar tratamento de exceções e recuperação de erros

## Boas Práticas Implementadas

- **Page Object Model**: Separação de responsabilidades entre páginas e testes
- **Esperas explícitas**: Utilização do WebDriverWait para esperar elementos
- **Gestão de instâncias do driver**: Implementação thread-safe com ThreadLocal
- **Relatórios detalhados**: Uso do ExtentReports para documentar execuções
- **Screenshots automáticos**: Captura em caso de falha nos testes
- **Tratamento de processos órfãos**: Limpeza adequada de recursos
- **Testes parametrizados**: Reuso de código para diferentes cenários
- **Rastreamento de performance**: Métricas para análise de desempenho

## Contribuições

Contribuições são bem-vindas! Por favor, crie um pull request para sugerir melhorias.

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para mais detalhes.