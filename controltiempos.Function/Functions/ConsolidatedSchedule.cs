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
            List<InputOutputEntity> entryEmployees = completedInpOuts.OrderBy(e => e.EmployeeId).ThenBy(e => e.DateInputOrOutput).ToList();

            DateTime dateInput = DateTime.UtcNow;
            int tempEmploy = 0;
            string tempRowKey="";
            int tempType = 1;

            foreach (InputOutputEntity entityTemp in entryEmployees)
            {
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

                    TableQuery<ConsolidatedEntity> queryTwo = new TableQuery<ConsolidatedEntity>();
                    TableQuerySegment<ConsolidatedEntity> completeConso = await consoliTable.ExecuteQuerySegmentedAsync(queryTwo, null);
                    ConsolidatedEntity consolited = completeConso
                        .Where(c => c.EmployeeId == tempEmploy && c.WorkDate.Date == DateTime.UtcNow.Date).FirstOrDefault();
                    if (consolited != null)
                    {
                        TableOperation findOperation = TableOperation.Retrieve<ConsolidatedEntity>("CONSOLIDATED", consolited.RowKey);
                        TableResult findfResult = await consoliTable.ExecuteAsync(findOperation);
                        ConsolidatedEntity consolidatedEntityResult = (ConsolidatedEntity)findfResult.Result;
                        consolidatedEntityResult.MinutesWorked += Convert.ToInt32(minutes.TotalMinutes);

                        string message = $"Consolidado Actualizado para el empleado: {entityTemp.EmployeeId}.";
                        TableOperation updateOperation = TableOperation.Replace(consolidatedEntityResult);
                        await consoliTable.ExecuteAsync(updateOperation);

                        findOperation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", tempRowKey);
                        findfResult = await inpOutTable.ExecuteAsync(findOperation);
                        InputOutputEntity inputEntityResult = (InputOutputEntity)findfResult.Result;
                        inputEntityResult.IsConsolidated = true;

                        updateOperation = TableOperation.Replace(inputEntityResult);
                        await inpOutTable.ExecuteAsync(updateOperation);

                        findOperation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", entityTemp.RowKey);
                        findfResult = await inpOutTable.ExecuteAsync(findOperation);
                        inputEntityResult = (InputOutputEntity)findfResult.Result;
                        inputEntityResult.IsConsolidated = true;

                        updateOperation = TableOperation.Replace(inputEntityResult);
                        await inpOutTable.ExecuteAsync(updateOperation);

                        log.LogInformation(message);

                    }
                    else
                    {
                        ConsolidatedEntity consolidatedEntity = new ConsolidatedEntity
                        {
                            EmployeeId = entityTemp.EmployeeId,
                            WorkDate = DateTime.UtcNow,
                            MinutesWorked = Convert.ToInt32(minutes.TotalMinutes),
                            ETag = "*",
                            PartitionKey = "CONSOLIDATED",
                            RowKey = Guid.NewGuid().ToString()
                        };
                        string message = $"Consolidado listo para el empleado: {entityTemp.EmployeeId}.";
                        TableOperation addOperation = TableOperation.Insert(consolidatedEntity);
                        await consoliTable.ExecuteAsync(addOperation);
                        TableOperation operation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", tempRowKey);
                        TableResult inputResult = await inpOutTable.ExecuteAsync(operation);

                        operation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", tempRowKey);
                        inputResult = await inpOutTable.ExecuteAsync(operation);
                        InputOutputEntity inputEntityResult = (InputOutputEntity)inputResult.Result;
                        inputEntityResult.IsConsolidated = true;

                        operation = TableOperation.Replace(inputEntityResult);
                        await inpOutTable.ExecuteAsync(operation);

                        operation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", entityTemp.RowKey);
                        inputResult = await inpOutTable.ExecuteAsync(operation);
                        inputEntityResult = (InputOutputEntity)inputResult.Result;
                        inputEntityResult.IsConsolidated = true;

                        operation = TableOperation.Replace(inputEntityResult);
                        await inpOutTable.ExecuteAsync(operation);
                        log.LogInformation(message);
                    }
                    

                }
            }
        }
    }
}
