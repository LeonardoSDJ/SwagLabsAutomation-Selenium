namespace SwagLabsAutomation.Utils;

public enum UserType
{
    Standard,
    LockedOut,
    Problem,
    PerformanceGlitch
}

public class UserModel
{
    private readonly string _description;
    public string Username { get; }
    public string Password { get; }
    public UserType Type { get; }
    public string[] ExpectedBehaviors { get; }

    private UserModel(string username, string password, UserType type, string description, string[] expectedBehaviors)
    {
        _description = description;
        Username = username;
        Password = password;
        Type = type;
        ExpectedBehaviors = expectedBehaviors;
    }

    public static UserModel Standard => new UserModel(
        "standard_user",
        "secret_sauce",
        UserType.Standard,
        "Standard user with normal behavior",
        ["Normal login", "Normal navigation", "Complete checkout"]
    );

    public static UserModel LockedOut => new UserModel(
        "locked_out_user",
        "secret_sauce",
        UserType.LockedOut,
        "Locked out user who cannot login",
        ["Cannot login", "Displays specific error message"]
    );

    public static UserModel Problem => new UserModel(
        "problem_user",
        "secret_sauce",
        UserType.Problem,
        "User with UI and behavior problems",
        [
            "All product images are identical",
            "Cannot fill forms correctly",
            "Sorting doesn't work",
            "Some links don't work properly"
        ]
    );

    public static UserModel PerformanceGlitch => new UserModel(
        "performance_glitch_user",
        "secret_sauce",
        UserType.PerformanceGlitch,
        "User with performance/slowness issues",
        [
            "Slower login",
            "Slow navigation between pages",
            "Checkout works, but is slow"
        ]
    );

    public override string ToString()
    {
        return $"{Username} ({Type})";
    }
}