using inazurefunction.Functions.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace inazurefunction.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer,
        [Table("consolidatedreg", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
        [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
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
        }
    }
}
