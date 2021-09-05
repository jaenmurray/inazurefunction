using inazurefunction.Functions.Functions;
using inazurefunction.Tests.Helpers;
using System;
using Xunit;

namespace inazurefunction.Tests.Tests
{
    public class ScheduledFunctionTest
    {
        [Fact]
        public void ScheduledFunction_Should_log_Message()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployeeReg = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            ListLogger logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);
            // Act
            ScheduledFunction.Run(null, mockEmployeeReg, mockEmployeeReg, logger);
            string message = logger.Logs[0];

            //Assert
            Assert.Contains("Consolidating completed", message);
        }
    }
}
