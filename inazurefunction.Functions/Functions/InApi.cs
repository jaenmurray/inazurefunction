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
        [FunctionName(nameof(CreateEmployeeReg))]
        public static async Task<IActionResult> CreateEmployeeReg(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "employeereg")] HttpRequest req,
            [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
            ILogger log)
        {
            log.LogInformation("Received a new record");

            string message = "";
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


            EmployeeEntity employeeEntity = new EmployeeEntity
            {
                ETag = "*",
                PartitionKey = "EmployeeReg",
                RowKey = Guid.NewGuid().ToString(),
                IdEmployee = record.IdEmployee,
                DateReg = Convert.ToDateTime(record.DateReg),
                Type = record.Type,
                IsConsolidated = false,
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


        [FunctionName(nameof(UpdateEmployeeReg))]
        public static async Task<IActionResult> UpdateEmployeeReg(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "employeereg/{id}")] HttpRequest req,
        [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
        string id,
        ILogger log)
        {
            log.LogInformation($"Update for employee: {id}, Received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            EmployeeReg record = JsonConvert.DeserializeObject<EmployeeReg>(requestBody);


            //Validate record RowKey id
            TableOperation findOperation = TableOperation.Retrieve<EmployeeEntity>("EmployeeReg", id);
            TableResult findResult = await employeeTable.ExecuteAsync(findOperation);

            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Record not found."
                });

            }

            //Update record
            EmployeeEntity employeeEntity = (EmployeeEntity)findResult.Result;

            if (!string.IsNullOrEmpty(record.IdEmployee.ToString()) || record.IdEmployee > 0)
            {
                employeeEntity.IdEmployee = record.IdEmployee;

            }

            if (!string.IsNullOrEmpty(record.Type.ToString()))
            {
                employeeEntity.Type = record.Type;

            }

            if (record.IsConsolidated == true || record.IsConsolidated == false)
            {
                employeeEntity.IsConsolidated = record.IsConsolidated;

            }

            TableOperation addOperation = TableOperation.Replace(employeeEntity);
            await employeeTable.ExecuteAsync(addOperation);

            string message = $"Todo: {id}, updated in table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = employeeEntity

            });
        }


        [FunctionName(nameof(GetAllEmployeeRegs))]
        public static async Task<IActionResult> GetAllEmployeeRegs(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employeereg")] HttpRequest req,
                [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
                ILogger log)
        {
            log.LogInformation("Get all records received.");

            TableQuery<EmployeeEntity> query = new TableQuery<EmployeeEntity>();
            TableQuerySegment<EmployeeEntity> records = await employeeTable.ExecuteQuerySegmentedAsync(query, null);


            string message = "Retrieved all records.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = records

            });
        }


        [FunctionName(nameof(GetEmployeeRegById))]
        public static IActionResult GetEmployeeRegById(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employeereg/{id}")] HttpRequest req,
                [Table("employeereg", "EmployeeReg", "{id}", Connection = "AzureWebJobsStorage")] EmployeeEntity employeeEntity,
                string id,
                ILogger log)
        {
            log.LogInformation($"Get record by id: {id}, received.");

            if (employeeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Record not found."
                });

            }

            string message = $"Todo: {employeeEntity.RowKey}, retrieved.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = employeeEntity

            });
        }


        [FunctionName(nameof(DeleteEmployeeReg))]
        public static async Task<IActionResult> DeleteEmployeeReg(
                [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "employeereg/{id}")] HttpRequest req,
                [Table("employeereg", "EmployeeReg", "{id}", Connection = "AzureWebJobsStorage")] EmployeeEntity employeeEntity,
                [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
                string id,
                ILogger log)
        {
            log.LogInformation($"Delete record: {id}, received.");

            if (employeeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Record not found."
                });

            }

            await employeeTable.ExecuteAsync(TableOperation.Delete(employeeEntity));
            string message = $"Todo: {employeeEntity.RowKey}, deteled.";
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
