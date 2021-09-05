using inazurefunction.Common.Models;
using inazurefunction.Functions.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.IO;

namespace inazurefunction.Tests.Helpers
{
    public class TestFactory
    {
        public static EmployeeEntity GetEmployeeEntity()
        {
            return new EmployeeEntity
            {
                ETag = "*",
                PartitionKey = "EmployeeReg",
                RowKey = Guid.NewGuid().ToString(),
                DateReg = DateTime.UtcNow,
                IsConsolidated = false,
                IdEmployee = 2,
                Type = 0
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid entryEmp, EmployeeReg empRequest)
        {
            string request = JsonConvert.SerializeObject(empRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{entryEmp}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid entryEmp)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{entryEmp}"
            };
        }

        //Create a new record in EmployeeReg
        public static DefaultHttpRequest CreateHttpRequest(EmployeeReg empRequest)
        {
            string request = JsonConvert.SerializeObject(empRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest()
        {
            return new DefaultHttpRequest(new DefaultHttpContext());
        }

        public static EmployeeReg GetEmployeeRegRequest()
        {
            return new EmployeeReg
            {
                IdEmployee = 2,
                DateReg = DateTime.UtcNow,
                Type = 0,
                IsConsolidated = false,
            };
        }

        public static ConsolidatedEntity GetConsolidatedEntity()
        {
            return new ConsolidatedEntity
            {
                ETag = "*",
                PartitionKey = "ConsolidatedEntity",
                RowKey = Guid.NewGuid().ToString(),
                WorkedDate = DateTime.UtcNow,
                IdEmployee = 2,
                WorkedMinutes = 234
            };
        }

        public static ConsolidatedReg GetConsolidatedRegRequest()
        {
            return new ConsolidatedReg
            {
                idEmployee = 2,
                WorkedDate = DateTime.UtcNow,
                WorkedMinutes = 234,
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(string entryDate, ConsolidatedReg consolRequest)
        {
            string request = JsonConvert.SerializeObject(consolRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{entryDate}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(string entryDate)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{entryDate}"
            };
        }

        private static Stream GenerateStreamFromString(string stringToConvert)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(stringToConvert);
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
