namespace SwagLabsAutomation.Models
{
    public enum UserType
    {
        Standard,
        LockedOut,
        Problem,
        PerformanceGlitch
    }

    public class UserModel
    {
        public string Username { get; }
        public string Password { get; }
        public UserType Type { get; }
        public string Description { get; }
        public string[] ExpectedBehaviors { get; }

        private UserModel(string username, string password, UserType type, string description, string[] expectedBehaviors)
        {
            Username = username;
            Password = password;
            Type = type;
            Description = description;
            ExpectedBehaviors = expectedBehaviors;
        }

        public static UserModel Standard => new UserModel(
            "standard_user",
            "secret_sauce",
            UserType.Standard,
            "Usuário padrão com comportamento normal",
            new[] { "Login normal", "Navegação normal", "Checkout completo" }
        );

        public static UserModel LockedOut => new UserModel(
            "locked_out_user",
            "secret_sauce",
            UserType.LockedOut,
            "Usuário bloqueado que não consegue fazer login",
            new[] { "Não consegue fazer login", "Exibe mensagem de erro específica" }
        );

        public static UserModel Problem => new UserModel(
            "problem_user",
            "secret_sauce",
            UserType.Problem,
            "Usuário com problemas de UI e comportamento",
            new[] {
                "Todas as imagens de produtos são iguais",
                "Não consegue preencher formulários corretamente",
                "Ordenação não funciona",
                "Alguns links não funcionam corretamente"
            }
        );

        public static UserModel PerformanceGlitch => new UserModel(
            "performance_glitch_user",
            "secret_sauce",
            UserType.PerformanceGlitch,
            "Usuário com problemas de performance/lentidão",
            new[] {
                "Login mais lento",
                "Navegação lenta entre páginas",
                "Checkout funciona, mas é lento"
            }
        );

        public override string ToString()
        {
            return $"{Username} ({Type})";
        }
    }
}