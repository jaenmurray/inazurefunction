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

            string filter = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(record.IdEmployee.ToString()));
            TableQuery<EmployeeEntity> query = new TableQuery<EmployeeEntity>().Where(filter);
            TableQuerySegment<EmployeeEntity> RowsEmployees = await employeeTable.ExecuteQuerySegmentedAsync(query, null);

            List<EmployeeEntity> ListEmployees = new List<EmployeeEntity>();
            foreach (EmployeeEntity Row in RowsEmployees)
            {

                ListEmployees.Add(Row);

            }

            EmployeeEntity RowEmployee = ListEmployees.OrderByDescending(o => o.DateReg).FirstOrDefault();
            string message = string.Empty;

            if (ListEmployees.Count != 0)
            {
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
            }

            EmployeeEntity employeeEntity = new EmployeeEntity
            {
                IdEmployee = record.IdEmployee,
                DateReg = Convert.ToDateTime(record.DateReg),
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


        [FunctionName(nameof(ConsolidateProcess))]
        public static async Task<IActionResult> ConsolidateProcess(
                [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "consolidatedreg")] HttpRequest req,
                [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
                [Table("consolidatedreg", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
                ILogger log)
        {
            log.LogInformation($"Consolidating completed function executed at: {DateTime.Now}"); ;


            string filterNotConsolidatedID = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);

            TableQuery<EmployeeEntity> queryToConsolidate = new TableQuery<EmployeeEntity>().Where(filterNotConsolidatedID);
            TableQuerySegment<EmployeeEntity> records = await employeeTable.ExecuteQuerySegmentedAsync(queryToConsolidate, null);

            List<EmployeeEntity> ListEmployees = new List<EmployeeEntity>();
            foreach (EmployeeEntity Row in records)
            {

                ListEmployees.Add(Row);

            }


            ListEmployees.OrderByDescending(o => o.DateReg);

            int consolCount = 0;
            for (int i = 0; i < ListEmployees.Count;)
            {


                int? Id = ListEmployees[i].IdEmployee;


                string filterEntriesByID = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(Id.ToString())),
                                    TableOperators.And, TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false),
                                    TableOperators.And, TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, 0)));


                TableQuery<EmployeeEntity> queryEntries = new TableQuery<EmployeeEntity>().Where(filterEntriesByID);
                TableQuerySegment<EmployeeEntity> RowsEntries = await employeeTable.ExecuteQuerySegmentedAsync(queryEntries, null);


                List<EmployeeEntity> RowsOfEntries = new List<EmployeeEntity>();
                foreach (EmployeeEntity Item in RowsEntries)
                {

                    RowsOfEntries.Add(Item);

                }

                EmployeeEntity firtsDateEntry = RowsOfEntries.OrderBy(o => o.DateReg).FirstOrDefault();

                if (RowsOfEntries.Count == 0 || firtsDateEntry.IsConsolidated == true)
                {
                    i++;
                    continue;
                }

                string filterOutputsID = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(Id.ToString())),
                                    TableOperators.And, TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false),
                                    TableOperators.And, TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, 1)));


                TableQuery<EmployeeEntity> queryOutPuts = new TableQuery<EmployeeEntity>().Where(filterOutputsID);
                TableQuerySegment<EmployeeEntity> RowsOutputs = await employeeTable.ExecuteQuerySegmentedAsync(queryOutPuts, null);



                List<EmployeeEntity> RowsOfOutputs = new List<EmployeeEntity>();
                foreach (EmployeeEntity Item in RowsOutputs)
                {

                    RowsOfOutputs.Add(Item);

                }

                EmployeeEntity lastDateOutput = RowsOfOutputs.OrderByDescending(o => o.DateReg).FirstOrDefault();


                if (RowsOfOutputs.Count == 0)
                {
                    i++;
                    continue;
                }

                DateTime stardate = Convert.ToDateTime(firtsDateEntry.DateReg);

                DateTime lastdate = Convert.ToDateTime(lastDateOutput.DateReg);

                TimeSpan subtract = lastdate.Subtract(stardate);

                int minutes = Convert.ToInt32(subtract.TotalMinutes);


                string filterConsolidatedByDate = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate("WorkedDate", QueryComparisons.GreaterThanOrEqual, Convert.ToDateTime(ListEmployees[i].DateReg.Date)),
                                                  TableOperators.And, TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(Id.ToString())));

                TableQuery<ConsolidatedEntity> queryValidateConsolidates = new TableQuery<ConsolidatedEntity>().Where(filterConsolidatedByDate);
                TableQuerySegment<ConsolidatedEntity> RowsConsolidated = await consolidatedTable.ExecuteQuerySegmentedAsync(queryValidateConsolidates, null);


                List<ConsolidatedEntity> RowsOfConsolidates = new List<ConsolidatedEntity>();
                foreach (ConsolidatedEntity Reg in RowsConsolidated)
                {

                    RowsOfConsolidates.Add(Reg);

                }

                ConsolidatedEntity consolidates = RowsOfConsolidates.OrderByDescending(o => o.WorkedDate).FirstOrDefault();

                if (RowsOfConsolidates.Count != 0)
                {
                    TableOperation findOperationConsolidated = TableOperation.Retrieve<ConsolidatedEntity>("ConsolidatedReg", consolidates.RowKey.ToString());
                    TableResult findResult = await consolidatedTable.ExecuteAsync(findOperationConsolidated);

                    ConsolidatedEntity consolidatedEntity = (ConsolidatedEntity)findResult.Result;

                    int workedminutes = consolidatedEntity.WorkedMinutes;

                    consolidatedEntity.WorkedMinutes = workedminutes + minutes;

                    consolidatedEntity.WorkedDate = lastDateOutput.DateReg;

                    TableOperation addOperation2 = TableOperation.Replace(consolidatedEntity);
                    await consolidatedTable.ExecuteAsync(addOperation2);
                }
                else
                {
                    ConsolidatedEntity consolidatedEntity = new ConsolidatedEntity
                    {
                        IdEmployee = int.Parse(lastDateOutput.IdEmployee.ToString()),
                        WorkedDate = lastDateOutput.DateReg,
                        WorkedMinutes = minutes,
                        ETag = "*",
                        PartitionKey = "ConsolidatedReg",
                        RowKey = Guid.NewGuid().ToString()
                    };

                    TableOperation addOperation = TableOperation.Insert(consolidatedEntity);
                    await consolidatedTable.ExecuteAsync(addOperation);
                }


                for (int k = 0; k < RowsOfEntries.Count; k++)
                {

                    TableOperation findOperation = TableOperation.Retrieve<EmployeeEntity>("EmployeeReg", RowsOfEntries[k].RowKey.ToString());
                    TableResult findResult = await employeeTable.ExecuteAsync(findOperation);

                    EmployeeEntity employeeEntity = (EmployeeEntity)findResult.Result;

                    employeeEntity.IsConsolidated = true;

                    TableOperation addOperation2 = TableOperation.Replace(employeeEntity);
                    await employeeTable.ExecuteAsync(addOperation2);

                }

                for (int m = 0; m < RowsOfOutputs.Count; m++)
                {

                    TableOperation findOperation = TableOperation.Retrieve<EmployeeEntity>("EmployeeReg", RowsOfOutputs[m].RowKey.ToString());
                    TableResult findResult = await employeeTable.ExecuteAsync(findOperation);

                    EmployeeEntity employeeEntity = (EmployeeEntity)findResult.Result;

                    employeeEntity.IsConsolidated = true;

                    TableOperation addOperation2 = TableOperation.Replace(employeeEntity);
                    await employeeTable.ExecuteAsync(addOperation2);

                }

                log.LogInformation($"New consolidated stored for employee {lastDateOutput.IdEmployee}.");

                i++;

                consolCount++;
            }

            string message = $"Consolidation summary. records added: {consolCount}";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = null
            });
        }


        [FunctionName(nameof(GetAllConsolidatesByDate))]
        public static async Task<IActionResult> GetAllConsolidatesByDate(
              [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consolidatedreg/{date}")] HttpRequest req,
              [Table("consolidatedreg", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
              string date,
              ILogger log)
        {
            log.LogInformation($"Get all consolidates by date: {date}, received.");

            string filterConsolidatedByDate = TableQuery.GenerateFilterConditionForDate("WorkedDate", QueryComparisons.GreaterThanOrEqual, Convert.ToDateTime(date));

            TableQuery<ConsolidatedEntity> queryToConsolidate = new TableQuery<ConsolidatedEntity>().Where(filterConsolidatedByDate);
            TableQuerySegment<ConsolidatedEntity> records = await consolidatedTable.ExecuteQuerySegmentedAsync(queryToConsolidate, null);

            if (records == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Consolidates not found in this date."
                });

            }

            string message = $"Get Consolidates by date: {date}, completed.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = records

            });
        }
    }
}
