# SwagLabs Automation com Selenium, C# e NUnit

Este projeto contém testes automatizados para o site Swag Labs (SauceDemo) utilizando Selenium WebDriver com C# e NUnit. O objetivo é demonstrar a implementação de testes automatizados usando o padrão Page Object Model (POM) e boas práticas de automação.

## Pré-requisitos

- Visual Studio 2022 ou superior
- .NET 9
- Conhecimentos básicos de C#, Selenium e NUnit

## Instalação

1. Clone este repositório
2. Abra a solução no Visual Studio
3. Restaure os pacotes NuGet:
   - Selenium.WebDriver
   - Selenium.Support
   - NUnit
   - NUnit3TestAdapter
   - DotNetSeleniumExtras.WaitHelpers
   - WebDriverManager

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
│   └── CheckoutTests.cs
└── Utils/
    └── TestBase.cs
```

### Pages

- **BasePage**: Classe base com métodos comuns para todas as páginas
- **LoginPage**: Página de login com métodos para autenticação
- **ProductsPage**: Página de produtos com métodos para adicionar produtos ao carrinho
- **CartPage**: Página do carrinho com métodos para manipular itens
- **CheckoutPage**: Páginas de checkout com métodos para o fluxo de compra

### Tests

- **LoginTests**: Testes para validar funcionalidades de login
- **ProductTests**: Testes para validar funcionalidades da página de produtos
- **CartTests**: Testes para validar funcionalidades do carrinho
- **CheckoutTests**: Testes para validar o fluxo de checkout

## Testes Implementados

### Login Tests
- Login com credenciais válidas
- Login com credenciais inválidas

### Product Tests
- Verificar se produtos são exibidos
- Adicionar produto ao carrinho

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

## Execução dos Testes

Para executar os testes:

1. Abra o Test Explorer no Visual Studio
2. Selecione os testes que deseja executar
3. Clique em "Run" ou "Run All"

## Próximas Melhorias Planejadas

1. **Testes para diferentes tipos de usuários**
   - Implementar testes para `locked_out_user`, `problem_user` e `performance_glitch_user`

2. **Relatórios e Screenshots**
   - Adicionar o Extent Reports para geração de relatórios visuais
   - Capturar screenshots para falhas de testes
   - Implementar logs detalhados

3. **Suporte a múltiplos navegadores**
   - Adicionar suporte para Chrome, Firefox e Edge
   - Criar um DriverFactory para gerenciar diferentes navegadores

4. **Testes em paralelo**
   - Configurar NUnit para execução de testes em paralelo
   - Implementar ThreadLocal para isolamento de instâncias do WebDriver

5. **Integração com CI/CD**
   - Configurar o projeto para execução em pipeline (Azure DevOps, GitHub Actions ou Jenkins)
   - Automatizar a execução dos testes como parte da integração contínua

6. **Testes de API**
   - Adicionar testes para verificar endpoints da API
   - Utilizar RestSharp ou HttpClient para chamadas de API

7. **Testes de Performance**
   - Medir tempo de carregamento das páginas
   - Comparar desempenho entre diferentes tipos de usuários

8. **Testes de componentes específicos**
   - Filtro e ordenação de produtos
   - Menu lateral
   - Logout

## Boas Práticas Implementadas

- **Page Object Model**: Separação de responsabilidades entre páginas e testes
- **Esperas explícitas**: Utilização do WebDriverWait para esperar elementos
- **Gerenciamento automático de drivers**: Utilização do WebDriverManager
- **Assertions descritivas**: Mensagens claras para falhas de testes
- **Métodos encadeados**: Retorno de instâncias de página para melhor legibilidade
- **Locators organizados**: Propriedades para armazenar os localizadores de elementos

## Contribuições

Contribuições são bem-vindas! Por favor, crie um pull request para sugerir melhorias.

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para mais detalhes.