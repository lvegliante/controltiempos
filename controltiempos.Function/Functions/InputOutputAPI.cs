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
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                if(JsonConvert.DeserializeObject<InputOutput>(requestBody)== null){
                    return new BadRequestObjectResult(new Response
                    {
                        IsSuccess = false,
                        Message = "Id de Empleado invalido."
                    });
                }
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Id de Empleado invalido."
                });
            }
            InputOutput inputOutput = JsonConvert.DeserializeObject<InputOutput>(requestBody);
           
            log.LogInformation($"Recibimos Ingreso o Salida del documento: {inputOutput.EmployeeId}");

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

        [FunctionName(nameof(EditedInputsAndOutputs))]
        public static async Task<IActionResult> EditedInputsAndOutputs(
                [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "inputoutput/{id}")] HttpRequest req,
                [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable timesTable, string id,
                ILogger log)
        {
            log.LogInformation($"Update for todo: {id}, recived.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                if (JsonConvert.DeserializeObject<InputOutput>(requestBody) == null)
                {
                    return new BadRequestObjectResult(new Response
                    {
                        IsSuccess = false,
                        Message = "Id de Empleado invalido."
                    });
                }
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Edited Input or Ouput not found."
                });
            }
            InputOutput inputOutput = JsonConvert.DeserializeObject<InputOutput>(requestBody);
            TableOperation findOperation = TableOperation.Retrieve<InputOutputEntity>("INPUTOUPUT", id);
            TableResult findfResult = await timesTable.ExecuteAsync(findOperation);

            if (findfResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Input or Ouput not found."
                });
            }

            InputOutputEntity inputOutputEntity = (InputOutputEntity)findfResult.Result;
            inputOutputEntity.DateInputOrOutput = inputOutput.DateInputOrOutput;
            inputOutputEntity.IsConsolidated = inputOutput.IsConsolidated;

            TableOperation addOperation = TableOperation.Replace(inputOutputEntity);
            await timesTable.ExecuteAsync(addOperation);
            string sType = (inputOutputEntity.Type == 0) ? "Input" : "Ouput";
            string message = $"{sType}: {id}, updated in table.";
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
