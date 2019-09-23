using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
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
        [NoAutomaticTrigger]
        public static async Task Run(ILogger log)
        {
            log.LogInformation("Started!!!");
            await Task.Delay(3600000);
            log.LogInformation("Stopped!!!");
        }
    }
}
