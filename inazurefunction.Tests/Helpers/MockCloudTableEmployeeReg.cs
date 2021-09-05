using inazurefunction.Functions.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace inazurefunction.Tests.Helpers
{
    public class MockCloudTableEmployeeReg : CloudTable
    {
        public MockCloudTableEmployeeReg(Uri tableAddress) : base(tableAddress)
        {
        }

        public MockCloudTableEmployeeReg(Uri tableAbsoluteUri, StorageCredentials credentials) : base(tableAbsoluteUri, credentials)
        {
        }

        public MockCloudTableEmployeeReg(StorageUri tableAddress, StorageCredentials credentials) : base(tableAddress, credentials)
        {
        }

        public override async Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            return await Task.FromResult(new TableResult
            {
                HttpStatusCode = 200,
                Result = TestFactory.GetEmployeeEntity()
            });
        }

        public override async Task<TableQuerySegment<EmployeeEntity>> ExecuteQuerySegmentedAsync<EmployeeEntity>(TableQuery<EmployeeEntity> query, TableContinuationToken token)
        {
            ConstructorInfo constructor = typeof(TableQuerySegment<EmployeeEntity>)
                   .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                   .FirstOrDefault(c => c.GetParameters().Count() == 1);

            return await Task.FromResult(constructor.Invoke(new object[] {
               TestFactory.GetEmployeeEntity() 
            }) as TableQuerySegment<EmployeeEntity>);
        }

    }

}
