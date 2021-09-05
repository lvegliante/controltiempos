using controltiempos.Common.Models;
using controltiempos.Function.Functions;
using controltiempos.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Xunit;

namespace controltiempos.Tests.Tests
{
    public class InputOutputAPITest
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void CreateImputOutput_Should_Return_200()
        {
            //Arrenge
            MockCloudTableInputOutput mockImputOutPut = new MockCloudTableInputOutput(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            InputOutput inputOutputReq = TestFactory.GetInputOutputRequest();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(inputOutputReq);
            //Act
            IActionResult response = await InputOutputAPI.CreateInputsAndOutputs(request, mockImputOutPut, logger);
            //Assert

            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        }

        [Fact]
        public async void EditedImputOutput_Should_Return_200()
        {
            //Arrenge
            MockCloudTableInputOutput mockImputOutPut = new MockCloudTableInputOutput(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            InputOutput inputOutputReq = TestFactory.GetInputOutputRequest();
            Guid intputOutputId = Guid.NewGuid();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(intputOutputId, inputOutputReq);
            //Act
            IActionResult response = await InputOutputAPI
                .EditedInputsAndOutputs(request, mockImputOutPut, intputOutputId.ToString(), logger);
            //Assert

            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        }

        [Fact]
        public async void GetAllImputOutput_Should_Return_200()
        {
            //Arrenge
            MockCloudTableInputOutput mockImputOutPut = new MockCloudTableInputOutput(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            InputOutput inputOutputReq = TestFactory.GetInputOutputRequest();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(inputOutputReq);
            //Act
            IActionResult response = await InputOutputAPI.CreateInputsAndOutputs(request, mockImputOutPut, logger);
            //Assert

            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        }
    }
}
