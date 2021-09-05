using inazurefunction.Common.Models;
using inazurefunction.Functions.Functions;
using inazurefunction.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace inazurefunction.Tests.Tests
{
    public class ConsolidatedApiTest
    {

        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void GetConsolidatedRegByDate_Should_Return_200()
        {
            // Arrange
            MockCloudTableConsolidatedReg mockEmployeeReg = new MockCloudTableConsolidatedReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            ConsolidatedReg consolRequest = TestFactory.GetConsolidatedRegRequest();
            string entryDate = Convert.ToString(DateTime.Now.ToString("yyyy-MM-dd"));
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(entryDate, consolRequest);

            // Act
            IActionResult response = await ConsolidatedApi.GetAllConsolidatesByDate(request, mockEmployeeReg, entryDate, logger);

            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }
    }
}
