using controltiempos.Common.Models;
using controltiempos.Function.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.IO;

namespace controltiempos.Tests.Helpers
{
    public class TestFactory
    {
        public static InputOutputEntity GetInputOutputEntity()
        {
            return new InputOutputEntity
            {
                ETag = "*",
                PartitionKey = "INPUTOUTPU",
                RowKey = Guid.NewGuid().ToString(),
                EmployeeId = 12345,
                DateInputOrOutput = DateTime.UtcNow,
                Type = 0,
                IsConsolidated = true,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ConsolidatedEntity GetConsolidatedEntity()
        {
            return new ConsolidatedEntity
            {
                ETag = "*",
                PartitionKey = "CONSOLIDATED",
                RowKey = Guid.NewGuid().ToString(),
                EmployeeId = 12345,
                WorkDate = DateTime.UtcNow,
                MinutesWorked = 0,
                Timestamp = DateTime.UtcNow
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid inputOutputID, InputOutput inputOutputRequest)
        {
            string request = JsonConvert.SerializeObject(inputOutputRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{inputOutputID}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(DateTime date, Consolidated consolidated)
        { 
            string request = JsonConvert.SerializeObject(consolidated);
             
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{date.Date.ToString("dd-MM-yyyy")}"

            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid date, Consolidated consolidatedRequest)
        {
            string request = JsonConvert.SerializeObject(consolidatedRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{date}"
            };
        }


        public static DefaultHttpRequest CreateHttpRequest(Guid dateOrInpOupID)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{dateOrInpOupID}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(InputOutput inputOutputRequest)
        {
            string request = JsonConvert.SerializeObject(inputOutputRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Consolidated consolidatedRequest)
        {
            string request = JsonConvert.SerializeObject(consolidatedRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest()
        {
            return new DefaultHttpRequest(new DefaultHttpContext());
        }

        public static InputOutput GetInputOutputRequest()
        {
            return new InputOutput
            {
                EmployeeId = 12345,
                DateInputOrOutput = DateTime.UtcNow,
                IsConsolidated = false,
                Type = 0,
            };
        }

        public static Consolidated GetConsolidatedRequest()
        {
            return new Consolidated
            {
                EmployeeId = 12345,
                MinutesWorked = 5,
                WorkDate = DateTime.UtcNow
            };
        }

        public static Stream GenerateStreamFromString(string stringToconvert)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(stringToconvert);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;
            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }
            return logger;
        }

    }
}
