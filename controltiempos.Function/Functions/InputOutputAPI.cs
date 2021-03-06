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
using System.Linq;
using System.Threading.Tasks;

namespace controltiempos.Function.Functions
{
    public static class InputOutputAPI
    {

        [FunctionName(nameof(CreateInputsAndOutputs))]
        public static async Task<IActionResult> CreateInputsAndOutputs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inputoutput")] HttpRequest req,
            [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable timesTable,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                if (JsonConvert.DeserializeObject<InputOutput>(requestBody) == null)
                {
                    return new BadRequestObjectResult(new Response
                    {
                        IsSuccess = false,
                        Message = "Employee Id not found."
                    });
                }
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Employee Id not found."
                });
            }
            InputOutput inputOutput = JsonConvert.DeserializeObject<InputOutput>(requestBody);
            //log.LogInformation($"Recibimos Ingreso o Salida del documento: {inputOutput.EmployeeId}");

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
            string sType = (type == 0) ? "Input" : "Output";

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

            string message = $"New Registration of {sType} for  Employe: {inputOutput.EmployeeId}.";
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
                        Message = "Id de Empleado not found."
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

        [FunctionName(nameof(GetAllInputsAndOutputs))]
        public static async Task<IActionResult> GetAllInputsAndOutputs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "inputoutput")] HttpRequest req,
            [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable timesTable,
            ILogger log)
        {
            log.LogInformation("Get all inputs and outputs recived.");

            TableQuery<InputOutputEntity> query = new TableQuery<InputOutputEntity>();
            TableQuerySegment<InputOutputEntity> inputOutputs = await timesTable.ExecuteQuerySegmentedAsync(query, null);
            string message = "Retrieved all inputs and outputs.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = inputOutputs
            });
        }

        [FunctionName(nameof(GetInputAndOutputById))]
        public static IActionResult GetInputAndOutputById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "inputoutput/{id}")] HttpRequest req,
            [Table("inputoutput", "INPUTOUPUT", "{id}", Connection = "AzureWebJobsStorage")] InputOutputEntity inputOutputEntity,
            string id,
            ILogger log)
        {

            if (inputOutputEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Input or utput not found."
                });
            }
            string sType = (inputOutputEntity.Type == 0) ? "Input" : "Output";
            log.LogInformation($"Get {sType} by id:{id} recived.");

            string message = $"{sType}: {id} retrieved";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = inputOutputEntity
            });
        }

        [FunctionName(nameof(DeleteInputAndOutputById))]
        public static async Task<IActionResult> DeleteInputAndOutputById(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "inputoutput/{id}")] HttpRequest req,
           [Table("inputoutput", "INPUTOUPUT", "{id}", Connection = "AzureWebJobsStorage")] InputOutputEntity inputOutputEntity,
           [Table("inputoutput", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
           string id,
           ILogger log)
        {
            if (inputOutputEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Todo not found."
                });
            }
            string sType = (inputOutputEntity.Type == 0) ? "Input" : "Output";
            log.LogInformation($"Delete {sType}: {id}, recived.");

            await todoTable.ExecuteAsync(TableOperation.Delete(inputOutputEntity));
            string message = $"{sType}: {inputOutputEntity.RowKey}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = inputOutputEntity
            });
        }

        [FunctionName(nameof(ConsolitedDate))]
        public static async Task<IActionResult> ConsolitedDate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consolidated/{dateTime}")] HttpRequest req,
            [Table("consolidated", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable, DateTime dateTime,
            ILogger log)

        {
        DateTime date2 = Convert.ToDateTime(dateTime.Date.ToString("dd-MM-yyyy"));
            Console.WriteLine(date2.Date.AddHours(23).AddMinutes(59)+ "\n"+ Convert.ToDateTime(dateTime.Date.ToString("dd-MM-yyyy")));
            if (consolidatedTable == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Consolited not found."
                });
            }
            string filter1 = TableQuery.GenerateFilterConditionForDate("WorkDate", QueryComparisons.GreaterThanOrEqual, Convert.ToDateTime(dateTime.Date.ToString("dd-MM-yyyy")));
            string filter2 = TableQuery.GenerateFilterConditionForDate("WorkDate", QueryComparisons.LessThanOrEqual, date2.Date.AddHours(23).AddMinutes(59));
            String filter = TableQuery.CombineFilters(filter1, TableOperators.And, filter2);
            TableQuery<ConsolidatedEntity> query = new TableQuery<ConsolidatedEntity>().Where(filter);
            TableQuerySegment<ConsolidatedEntity> consolitedMinutes = await consolidatedTable.ExecuteQuerySegmentedAsync(query, null);

            if (consolitedMinutes.Results.Count() == 0 || consolitedMinutes == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Consolidated not found."
                });
            }
            string message = "Retrieved all consolidated of minutes.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = consolitedMinutes
            });
        }
    }

}
