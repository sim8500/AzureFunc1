using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TestGiosFuncApp.Starter
{
    public static class GiosUpdateStarter
    {
        [FunctionName("GiosUpdateStarter")]
        public static async Task Run([TimerTrigger("1 15 * * * *")]TimerInfo myTimer,
                                    [OrchestrationClient] DurableOrchestrationClient starter,
                                    ILogger log)
        {
            var orchestrationId = await starter.StartNewAsync("GiosUpdate", null);

            log.LogTrace($"GiosUpdate Durable Func started at {DateTime.UtcNow} (orchestrationId = {orchestrationId})");
        }

        [FunctionName("TestTimeout")]
        public static async Task<IActionResult> RunTest([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
                                                        HttpRequest req, ILogger log)
        {
            log.LogInformation("Started!!!");
            await Task.Delay(10*3600*1000);
            log.LogInformation("Stopped!!!");
            return new OkResult();
        }
    }
}
