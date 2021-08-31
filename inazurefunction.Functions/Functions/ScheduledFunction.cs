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

            //Consulta los registros por consolidar
            string filterNotConsolidatedID = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);

            TableQuery<EmployeeEntity> queryToConsolidate = new TableQuery<EmployeeEntity>().Where(filterNotConsolidatedID);
            TableQuerySegment<EmployeeEntity> records = await employeeTable.ExecuteQuerySegmentedAsync(queryToConsolidate, null);

            List<EmployeeEntity> ListEmployees = new List<EmployeeEntity>();
            foreach (EmployeeEntity Row in records)
            {

                ListEmployees.Add(Row);

            }


            ListEmployees.OrderByDescending(o => o.DateReg);

            //Recorre los IdEmployee pendientes por consolidar
            for (int i = 0; i < ListEmployees.Count;)
            {


                int? Id = ListEmployees[i].IdEmployee;


                string filterEntriesByID = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(Id.ToString())),
                                    TableOperators.And, TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, 0));


                TableQuery<EmployeeEntity> query2 = new TableQuery<EmployeeEntity>().Where(filterEntriesByID);
                TableQuerySegment<EmployeeEntity> RowsEntries = await employeeTable.ExecuteQuerySegmentedAsync(query2, null);


                List<EmployeeEntity> RowsOfEntries = new List<EmployeeEntity>();
                foreach (EmployeeEntity Item in RowsEntries)
                {

                    RowsOfEntries.Add(Item);

                }

                EmployeeEntity firtsDateEntry = RowsOfEntries.OrderBy(o => o.DateReg).FirstOrDefault();

                if (firtsDateEntry.IsConsolidated == true)
                {
                    i++;
                    continue;
                }

                string filterOutputsID = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, int.Parse(Id.ToString())),
                                    TableOperators.And, TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, 1));


                TableQuery<EmployeeEntity> query3 = new TableQuery<EmployeeEntity>().Where(filterOutputsID);
                TableQuerySegment<EmployeeEntity> RowsOutputs = await employeeTable.ExecuteQuerySegmentedAsync(query3, null);



                List<EmployeeEntity> RowsOfOutputs = new List<EmployeeEntity>();
                foreach (EmployeeEntity Item in RowsOutputs)
                {

                    RowsOfOutputs.Add(Item);

                }

                EmployeeEntity lastDateOutput = RowsOfOutputs.OrderByDescending(o => o.DateReg).FirstOrDefault();

                if (string.IsNullOrEmpty(lastDateOutput.DateReg.ToString()))
                {
                    i++;
                    continue;
                }

                DateTime stardate = Convert.ToDateTime(firtsDateEntry.DateReg);

                DateTime lastdate = Convert.ToDateTime(lastDateOutput.DateReg);

                TimeSpan subtract = lastdate.Subtract(stardate);

                int minutesInHours = subtract.Hours * 60;

                int minutes = minutesInHours + subtract.Minutes;



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

                for (int k = 0; k < RowsOfEntries.Count; k++)
                {
                    //Update to consolidated records 
                    TableOperation findOperation = TableOperation.Retrieve<EmployeeEntity>("EmployeeReg", RowsOfEntries[k].RowKey.ToString());
                    TableResult findResult = await employeeTable.ExecuteAsync(findOperation);

                    EmployeeEntity employeeEntity = (EmployeeEntity)findResult.Result;

                    employeeEntity.IsConsolidated = true;

                    TableOperation addOperation2 = TableOperation.Replace(employeeEntity);
                    await employeeTable.ExecuteAsync(addOperation2);

                }

                for (int m = 0; m < RowsOfOutputs.Count; m++)
                {
                    //Update to consolidated records 
                    TableOperation findOperation = TableOperation.Retrieve<EmployeeEntity>("EmployeeReg", RowsOfOutputs[m].RowKey.ToString());
                    TableResult findResult = await employeeTable.ExecuteAsync(findOperation);

                    EmployeeEntity employeeEntity = (EmployeeEntity)findResult.Result;

                    employeeEntity.IsConsolidated = true;

                    TableOperation addOperation2 = TableOperation.Replace(employeeEntity);
                    await employeeTable.ExecuteAsync(addOperation2);

                }

                log.LogInformation($"New consolidated stored for employee {lastDateOutput.IdEmployee}.");

                i++;
            }

        }
    }
}
