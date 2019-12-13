using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DurableFanOutFanInApp.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFanOutFanInApp.Starter
{
    class ClientStarter
    {
        public ClientStarter(ILogger<ClientStarter> logger)
        {
            this.logger = logger;
        }

        [FunctionName("HttpStart")]
        public async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "orchestrators/{functionName}")] HttpRequestMessage req,
                                                            [DurableClient] IDurableClient starter,
                                                            string functionName)
        {
            // Function input comes from the request content.
            object eventData = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(functionName, eventData);

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private readonly ILogger logger;
    }
}