# SwagLabs Automation com Selenium, C# e NUnit

Este projeto é um **estudo prático** de automação de testes para o site Swag Labs (SauceDemo) utilizando Selenium WebDriver com C# e NUnit. O objetivo é demonstrar a implementação de testes automatizados usando o padrão Page Object Model (POM) e explorar recursos avançados como o BiDi (Chrome DevTools Protocol).

_English version below._

## Sobre este Projeto de Estudos

Este é um projeto voltado para **fins educacionais e de aprendizado**. Seu foco principal é:

1. Implementar testes automatizados usando Selenium WebDriver e C#
2. Aplicar o padrão Page Object Model (POM) para organização do código
3. Explorar e demonstrar o uso de recursos BiDi (Chrome DevTools Protocol)
4. Implementar práticas de geração de relatórios e tratamento de erros

Como se trata de um projeto de estudos, nem todas as funcionalidades planejadas serão necessariamente implementadas, e o foco pode mudar ao longo do desenvolvimento para explorar áreas específicas de interesse.

## Pré-requisitos

- Visual Studio 2022 / JetBrains Rider
- .NET 9
- 
## Instalação

1. Clone este repositório
2. Abra a solução no Visual Studio ou Rider
3. Restaure os pacotes NuGet:
   - Selenium.WebDriver
   - Selenium.Support
   - NUnit
   - NUnit3TestAdapter
   - DotNetSeleniumExtras.WaitHelpers
   - AventStack.ExtentReports

## Padrão Page Object Model (POM)

O projeto utiliza o padrão POM para separar a lógica da interface do usuário dos testes em si:

- **BasePage**: Classe base com métodos comuns para todas as páginas
- **LoginPage**: Página de login com métodos para autenticação
- **ProductsPage**: Página de produtos com métodos para adicionar produtos ao carrinho
- **CartPage**: Página do carrinho com métodos para manipular itens
- **CheckoutPage**: Páginas de checkout com métodos para o fluxo de compra

## Testes Implementados

### Login Tests
- Login com credenciais válidas
- Login com credenciais inválidas

### Product Tests
- Verificação da exibição de produtos
- Ordenação de produtos (A-Z, Z-A, preço crescente, preço decrescente)
- Adição de múltiplos produtos ao carrinho
- Remoção de produtos do carrinho na página de produtos
- Navegação para detalhes do produto
- Verificação de imagens, descrições e preços dos produtos
- Teste do botão "Voltar aos Produtos"

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

### BiDi Example Tests
- Login com monitoramento BiDi
- Login inválido com monitoramento de erros
- Login com usuário bloqueado
- Teste de performance de login

### Testes de Usuários Específicos
- Verificação de comportamentos específicos para cada tipo de usuário:
   - **standard_user**: Funcionamento normal
   - **locked_out_user**: Bloqueado sem acesso
   - **problem_user**: Problemas de UI e formulários
   - **performance_glitch_user**: Performance lenta

## Recursos Implementados

### BiDi (Chrome DevTools Protocol)
- Monitoramento de rede, console e performance
- Captura de erros JavaScript
- Medição de desempenho de página
- Análise detalhada de requisições de rede

### Relatórios Detalhados
- Relatórios HTML com ExtentReports
- Capturas de tela automáticas em falhas
- Logs detalhados das operações
- Informações de desempenho e métricas

### Gerenciamento de Driver
- Factory Pattern para instâncias do WebDriver
- Limpeza automática de recursos
- Tratamento de processos órfãos
- Configurações otimizadas do Chrome

## Execução dos Testes

Para executar os testes:

1. Abra o Test Explorer no Visual Studio ou Rider
2. Selecione os testes que deseja executar
3. Clique em "Run" ou "Run All"

Para verificar os resultados:

1. Navegue até a pasta de output do projeto em `bin/Debug/net9.0/`
2. Abra a pasta "Reports" para visualizar os relatórios HTML
3. Abra a pasta "Screenshots" para ver as capturas de tela

## Implementações Futuras (Sujeitas a Alterações)

Como este é um projeto de estudos, as seguintes funcionalidades podem ou não ser implementadas, dependendo do foco do aprendizado:

1. **Suporte a múltiplos navegadores**
2. **Testes em paralelo**
3. **Integração com CI/CD**
4. **Testes de API**
5. **Melhorias de arquitetura**

## Contribuições

Contribuições e sugestões são bem-vindas! Como este é um projeto de estudos, feedback educacional é especialmente valioso.

---

# SwagLabs Automation with Selenium, C# and NUnit

This project is a **practical study** of test automation for the Swag Labs (SauceDemo) website using Selenium WebDriver with C# and NUnit. The goal is to demonstrate the implementation of automated tests using the Page Object Model (POM) pattern and explore advanced features such as BiDi (Chrome DevTools Protocol).

## About this Study Project

This is a project aimed at **educational and learning purposes**. Its main focus is:

1. Implementing automated tests using Selenium WebDriver and C#
2. Applying the Page Object Model (POM) pattern for code organization
3. Exploring and demonstrating the use of BiDi (Chrome DevTools Protocol) features
4. Implementing report generation and error handling practices

As this is a study project, not all planned features will necessarily be implemented, and the focus may shift during development to explore specific areas of interest.

## Prerequisites

- Visual Studio 2022 or higher / JetBrains Rider
- .NET 9

## Installation

1. Clone this repository
2. Open the solution in Visual Studio or Rider
3. Restore NuGet packages:
   - Selenium.WebDriver
   - Selenium.Support
   - NUnit
   - NUnit3TestAdapter
   - DotNetSeleniumExtras.WaitHelpers
   - AventStack.ExtentReports

## Page Object Model (POM) Pattern

The project uses the POM pattern to separate the user interface logic from the tests themselves:

- **BasePage**: Base class with common methods for all pages
- **LoginPage**: Login page with authentication methods
- **ProductsPage**: Products page with methods to add products to cart
- **CartPage**: Cart page with methods to handle items
- **CheckoutPage**: Checkout pages with methods for purchase flow

## Implemented Tests

### Login Tests
- Login with valid credentials
- Login with invalid credentials

### Product Tests
- Verification of product display
- Product sorting (A-Z, Z-A, price ascending, price descending)
- Adding multiple products to cart
- Removing products from cart on products page
- Navigation to product details
- Verification of product images, descriptions, and prices
- Testing the "Back to Products" button

### Cart Tests
- Add item to cart
- Add multiple items
- Go to cart and continue shopping
- Remove item from cart

### Checkout Tests
- Complete checkout process
- Verify checkout total
- Cancel checkout
- Return to products after purchase

### BiDi Example Tests
- Login with BiDi monitoring
- Invalid login with error monitoring
- Login with locked-out user
- Login performance test

### User-Specific Tests
- Verification of specific behaviors for each user type:
   - **standard_user**: Normal functionality
   - **locked_out_user**: Blocked without access
   - **problem_user**: UI and form issues
   - **performance_glitch_user**: Slow performance

## Implemented Features

### BiDi (Chrome DevTools Protocol)
- Monitoring of network, console, and performance
- JavaScript error capture
- Page performance measurement
- Detailed analysis of network requests

### Detailed Reports
- HTML reports with ExtentReports
- Automatic screenshots on failures
- Detailed operation logs
- Performance information and metrics

### Driver Management
- Factory Pattern for WebDriver instances
- Automatic resource cleanup
- Orphaned process handling
- Optimized Chrome settings

## Running the Tests

To run the tests:

1. Open the Test Explorer in Visual Studio or Rider
2. Select the tests you want to run
3. Click "Run" or "Run All"

To check the results:

1. Navigate to the project output folder in `bin/Debug/net9.0/`
2. Open the "Reports" folder to view HTML reports
3. Open the "Screenshots" folder to see the screenshots

## Future Implementations (Subject to Change)

As this is a study project, the following features may or may not be implemented, depending on the focus of learning:

1. **Multi-browser support**
2. **Parallel testing**
3. **CI/CD integration**
4. **API testing**
5. **Architecture improvements**

## Contributions

Contributions and suggestions are welcome! As this is a study project, educational feedback is especially valuable.
