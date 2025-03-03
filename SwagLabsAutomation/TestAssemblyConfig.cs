using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation;

[SetUpFixture]
public class TestAssemblyConfig
{
    [OneTimeSetUp]
    public void SetupExtentReports()
    {
        Console.WriteLine("OneTimeSetUp - Initializing ExtentReports");
        ExtentReportManager.GetInstance();
    }

    [OneTimeTearDown]
    public void TearDownExtentReports()
    {
        Console.WriteLine("OneTimeTearDown - Finalizing ExtentReports");
        ExtentReportManager.EndReport();
    }
}