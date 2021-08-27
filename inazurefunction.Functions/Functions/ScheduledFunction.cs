using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using inazurefunction.Functions.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace inazurefunction.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
        [Table("consolidatedreg", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
        [Table("employeereg", Connection = "AzureWebJobsStorage")] CloudTable employeeTable,
        ILogger log)
        {
            log.LogInformation($"Consolidating completed function executed at: {DateTime.Now}"); ;

            TableQuery<EmployeeEntity> query1 = new TableQuery<EmployeeEntity>();
            TableQuerySegment<EmployeeEntity> records = await employeeTable.ExecuteQuerySegmentedAsync(query1, null);

            List<EmployeeEntity> ListEmployees = new List<EmployeeEntity>();
            foreach (EmployeeEntity Row in records)
            {

                ListEmployees.Add(Row);

            }

            ListEmployees.OrderBy(o => o.DateReg);

            for (int i = 0; i < ListEmployees.Count; i++)
            {
                DateTime validateDate1 = ListEmployees[i].DateReg.Date;
                DateTime validateDate2 = ListEmployees[i].DateReg.Date.AddDays(1);

                string filter = "(PartitionKey eq 'EmployeeReg') and (Date ge datetime'26/08/2021 12:00:00 a. m.' and Date lt datetime'27/08/2021 12:00:00 a. m.')";

                //string filterDate = TableQuery.GenerateFilterCondition("DateReg", QueryComparisons.LessThan, validateDate.ToString());

                //string filterId = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, short.Parse(ListEmployees[i].IdEmployee.ToString()));
                TableQuery<EmployeeEntity> query2 = new TableQuery<EmployeeEntity>().Where(filter);
                TableQuerySegment<EmployeeEntity> RowsEmployees = await employeeTable.ExecuteQuerySegmentedAsync(query2, null);


                List<EmployeeEntity> ListEm = new List<EmployeeEntity>();
                foreach (EmployeeEntity Row in RowsEmployees)
                {

                    ListEm.Add(Row);

                }

                EmployeeEntity RowEmployee = ListEm.OrderByDescending(o => o.DateReg).FirstOrDefault();

            }





            //EmployeeEntity RowEmployee = ListEmployees.OrderByDescending(o => o.DateReg).FirstOrDefault();
            //string message = string.Empty;
        }
    }
}
