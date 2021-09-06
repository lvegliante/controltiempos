using controltiempos.Function.Functions;
using controltiempos.Tests.Helpers;
using System;
using Xunit;

namespace controltiempos.Tests.Tests
{
    public class consolitedScheduleAPITest
    {
        [Fact]
        public void ScheduledFunction_Should_Log_Message()
        {
            //Arrange
            MockCloudTableConsolidated mockConsolidated = new MockCloudTableConsolidated(
                 new Uri("http://127.0.0.1:10002/devstoreaccount1/report"));
            MockCloudTableInputOutput mockInputOutput = new MockCloudTableInputOutput(
                 new Uri("http://127.0.0.1:10002/devstoreaccount1/report"));

            ListLogger logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);
            //Act
            ConsolidatedSchedule.Run(null, mockInputOutput, mockConsolidated, logger);

            string message = logger.Logs[0];
            //Assert
            Assert.Contains("executed", message);
        }
    }
}
