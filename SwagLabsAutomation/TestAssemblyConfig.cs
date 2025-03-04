using NUnit.Framework;
using SwagLabsAutomation.Utils;

namespace SwagLabsAutomation;

[SetUpFixture]
/* Parallelization won't be finished by now
 * [assembly: Parallelizable(ParallelScope.Fixtures)]
 *[assembly: LevelOfParallelism(4)]  Number of parallel threads
 */

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