using NUnit.Framework;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation;

[SetUpFixture]
public class TestAssemblyConfig
{
    [OneTimeSetUp]
    public void SetupExtentReports()
    {
        Console.WriteLine("OneTimeSetUp - Inicializando ExtentReports");
        ExtentReportManager.GetInstance();
    }

    [OneTimeTearDown]
    public void TearDownExtentReports()
    {
        Console.WriteLine("OneTimeTearDown - Finalizando ExtentReports");
        ExtentReportManager.EndReport();
    }
}