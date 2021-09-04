using controltiempos.Function.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace controltiempos.Function.Functions
{
    public static class ConsolidatedSchedule
    {
        [FunctionName("ConsolidatedSchedule")]
        public static async Task Run(
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable inpOutTable,
            [Table("consolidated", Connection = "AzureWebJobsStorage")] CloudTable consoliTable,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string filter = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);
            TableQuery<InputOutputEntity> query = new TableQuery<InputOutputEntity>().Where(filter);
            TableQuerySegment<InputOutputEntity> completedInpOuts = await inpOutTable.ExecuteQuerySegmentedAsync(query, null);
            List<InputOutputEntity> entryEmployees = completedInpOuts.OrderBy(e => e.EmployeeId).OrderBy(e => e.DateInputOrOutput).ToList();
            //TableQuery<ConsolidatedEntity> queryTwo = new TableQuery<ConsolidatedEntity>();
           // TableQuerySegment<ConsolidatedEntity> completeConso = await consoliTable.ExecuteQuerySegmentedAsync(queryTwo, null);

            DateTime dateInput = DateTime.UtcNow;
            int tempEmploy = 0;
            string tempRowKey;
            int tempType = 1;
            foreach (InputOutputEntity entityTemp in entryEmployees)
            {
                Console.Write("El tipo es"+ entityTemp.Type);
                if (entityTemp.Type == 0)
                {
                    dateInput = entityTemp.DateInputOrOutput;
                    tempEmploy = entityTemp.EmployeeId;
                    tempType = entityTemp.Type;
                    tempRowKey = entityTemp.RowKey;
                }
                else if (entityTemp.Type == 1 && tempEmploy == entityTemp.EmployeeId && tempType == 0)
                {
                    TimeSpan minutes = entityTemp.DateInputOrOutput - dateInput;
                    
                        ConsolidatedEntity consolidatedEntity = new ConsolidatedEntity
                        {
                            EmployeeId = entityTemp.EmployeeId,
                            WorkDate = DateTime.UtcNow,
                            MinutesWorked = Convert.ToInt32(minutes.TotalMinutes),
                            ETag = "*",
                            PartitionKey = "CONSOLIDATED",
                            RowKey = Guid.NewGuid().ToString()
                        };
                    string message = $"Consolidado listo para el epleado: {entityTemp.EmployeeId}.";
                    TableOperation addOperation = TableOperation.Insert(consolidatedEntity);
                    await consoliTable.ExecuteAsync(addOperation);
                    log.LogInformation(message);

                }
            }
        }
    }
}
