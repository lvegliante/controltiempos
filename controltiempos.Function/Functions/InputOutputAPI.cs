using controltiempos.Common.Models;
using controltiempos.Common.Responses;
using controltiempos.Function.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace controltiempos.Function.Functions
{
    public static class InputOutputAPI
    {

        [FunctionName(nameof(ConsolidateInputsAndOutputs))]
        public static async Task<IActionResult> ConsolidateInputsAndOutputs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inputoutput")] HttpRequest req,
            [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable timesTable,
            ILogger log)
        {
            log.LogInformation($"Recibimos Ingreso\\Salida  del documento");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            InputOutput inputOutput = JsonConvert.DeserializeObject<InputOutput>(requestBody);

            if (inputOutput?.EmployeeId == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "El Registro de Entrada/Salida Tener un Id de Empleado."
                });
            }
            string filterOne = TableQuery.GenerateFilterConditionForInt("EmployeeId", QueryComparisons.Equal, inputOutput.EmployeeId);
            string filterTwo = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);
            string filter = TableQuery.CombineFilters(filterOne, TableOperators.And, filterTwo);
            TableQuery<InputOutputEntity> query = new TableQuery<InputOutputEntity>().Where(filter);
            TableQuerySegment<InputOutputEntity> empInpOut = await timesTable.ExecuteQuerySegmentedAsync(query, null);
            DateTime dateTime = DateTime.UtcNow.AddDays(-5);

            int type = 1;

                foreach (InputOutputEntity conInpOut in empInpOut)
                {
                    if (conInpOut.DateInputOrOutput > dateTime 
                        && conInpOut.DateInputOrOutput.Date == DateTime.UtcNow.Date)
                    {
                        dateTime = conInpOut.DateInputOrOutput;
                        type = conInpOut.Type;
                    }
                }
   
            type = (type == 0) ? 1 : 0;
            string sType = (type == 0) ? "Entrada" : "Salida";

            InputOutputEntity inputOutputEntity = new InputOutputEntity
            {
                EmployeeId = inputOutput.EmployeeId,
                DateInputOrOutput = DateTime.UtcNow,
                Type = type,
                IsConsolidated = false,
                ETag = "*",
                PartitionKey = "INPUTOUPUT",
                RowKey = Guid.NewGuid().ToString(),
            };

            string message = $"Nuevo Registro de {sType} para el Empleado: {inputOutput.EmployeeId}.";
            TableOperation addOperation = TableOperation.Insert(inputOutputEntity);
            await timesTable.ExecuteAsync(addOperation);
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = inputOutputEntity
            });
        }
    }
}
