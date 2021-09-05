using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using inazurefunction.Common.Models;
using Xunit;
using inazurefunction.Tests.Helpers;
using inazurefunction.Functions.Functions;
using inazurefunction.Functions.Entities;

namespace inazurefunction.Tests.Tests
{
    public class InApiTest
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void CreateEmployeeReg_Should_Return_200()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployeeReg = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            EmployeeReg empRequest = TestFactory.GetEmployeeRegRequest();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(empRequest);

            // Act
            IActionResult response = await InApi.CreateEmployeeReg(request, mockEmployeeReg, logger);

            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void UpdateEmployeeReg_Should_Return_200()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployeeReg = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            EmployeeReg empRequest = TestFactory.GetEmployeeRegRequest();
            Guid entryEmp = Guid.NewGuid();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(entryEmp, empRequest);

            // Act
            IActionResult response = await InApi.UpdateEmployeeReg(request, mockEmployeeReg, entryEmp.ToString(), logger);

            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void GetAllEmployeeRegs_Should_Return_200()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployeeReg = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            EmployeeReg empRequest = TestFactory.GetEmployeeRegRequest();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(empRequest);

            // Act
            IActionResult response = await InApi.GetAllEmployeeRegs(request, mockEmployeeReg, logger);

            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void GetEmployeeRegById_Should_Return_200()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployeeReg = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            EmployeeReg empRequest = TestFactory.GetEmployeeRegRequest();
            Guid entryEmp = Guid.NewGuid();
            EmployeeEntity employeeEntity = TestFactory.GetEmployeeEntity();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(entryEmp, empRequest);

            // Act
            IActionResult response = InApi.GetEmployeeRegById(request, employeeEntity, entryEmp.ToString(), logger);

            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void DelEmployeeById_Should_Return_200()
        {
            // Arrange
            MockCloudTableEmployeeReg mockEmployee = new MockCloudTableEmployeeReg(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            EmployeeReg empRequest = TestFactory.GetEmployeeRegRequest();
            Guid entryEmp = Guid.NewGuid();
            EmployeeEntity employeeEntity = TestFactory.GetEmployeeEntity();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(entryEmp, empRequest);

            // Act
            IActionResult response = await InApi.DeleteEmployeeReg(request, employeeEntity, mockEmployee, entryEmp.ToString(), logger);
            // Assert
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }


    }
}
