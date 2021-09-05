using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace controltiempos.Tests.Helpers
{
    public class MockCloudTableInputOutput : CloudTable
    {
        public MockCloudTableInputOutput(Uri tableAddress) : base(tableAddress)
        {
        }

        public MockCloudTableInputOutput(Uri tableAbsoluteUri, StorageCredentials credentials) : base(tableAbsoluteUri, credentials)
        {
        }

        public MockCloudTableInputOutput(StorageUri tableAddress, StorageCredentials credentials) : base(tableAddress, credentials)
        {
        }

        public override async Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            return await Task.FromResult(new TableResult
            {
                HttpStatusCode = 200,
                Result = TestFactory.GetInputOutputEntity()
            });
        }
    }
}
