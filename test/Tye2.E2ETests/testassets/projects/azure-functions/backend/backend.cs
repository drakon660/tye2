using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace backend
{
    public class backend
    {
        private readonly ILogger _logger;

        public backend(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<backend>();
        }

        [Function("backend")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new BackendInfo
            {
                IP = "127.0.0.1",
                Hostname = System.Net.Dns.GetHostName(),
            });

            return response;
        }

        class BackendInfo
        {
            public string IP { get; set; } = default!;
            public string Hostname { get; set; } = default!;
        }
    }
}
