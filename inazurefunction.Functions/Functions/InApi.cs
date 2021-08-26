using inazurefunction.Common.Models;
using inazurefunction.Common.Responses;
using inazurefunction.Functions.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace inazurefunction.Functions.Functions
{
    public static class InApi
    {
        [FunctionName(nameof(CreateReg))]
        public static async Task<IActionResult> CreateReg(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "employeereg")] HttpRequest req,
            [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
            ILogger log)
        {
            log.LogInformation("Received a new record");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            EmployeeReg record = JsonConvert.DeserializeObject<EmployeeReg>(requestBody);

            if (string.IsNullOrEmpty(record?.IdEmployee.ToString()))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a IdEmployee."
                });
            }


            if (record.IdEmployee == 0)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a IdEmployee."
                });
            }

            if (string.IsNullOrEmpty(record?.Type.ToString()))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a Type, 0 or 1 (In/Out)."
                });
            }

            if (record.Type >= 2 || record.Type < 0)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a Type, 0 or 1 (In/Out)."
                });
            }

            string filter = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, short.Parse(record.IdEmployee.ToString()));
            TableQuery<EmployeeEntity> query = new TableQuery<EmployeeEntity>().Where(filter);
            TableQuerySegment<EmployeeEntity> RowsEmployees = await employeeTable.ExecuteQuerySegmentedAsync(query, null);

            List<EmployeeEntity> ListEmployees = new List<EmployeeEntity>();
            foreach (EmployeeEntity Row in RowsEmployees)
            {

                ListEmployees.Add(Row);

            }

            EmployeeEntity RowEmployee = ListEmployees.OrderByDescending(o => o.DateReg).FirstOrDefault();
            string message = string.Empty;

            if (RowEmployee.Type.Equals(record.Type))
            {

                if (record.Type == 0)
                {
                    message = "This employee already registered an entry (0).";
                }
                else
                {
                    message = "This employee already checked out (1).";
                }

                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = message
                });
            }

            EmployeeEntity employeeEntity = new EmployeeEntity
            {
                IdEmployee = record.IdEmployee,
                DateReg = DateTime.UtcNow,
                Type = record.Type,
                IsConsolidated = false,
                ETag = "*",
                PartitionKey = "EmployeeReg",
                RowKey = Guid.NewGuid().ToString()
            };

            TableOperation addOperation = TableOperation.Insert(employeeEntity);
            await employeeTable.ExecuteAsync(addOperation);

            message = $"New record stored for employee {record.IdEmployee}.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = employeeEntity
            });
        }
    }
}
