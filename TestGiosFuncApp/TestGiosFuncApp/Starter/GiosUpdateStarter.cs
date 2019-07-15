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
        public static async Task Run([TimerTrigger("* 15 * * * *")]TimerInfo myTimer,
                                    [OrchestrationClient] DurableOrchestrationClient starter,
                                    ILogger log)
        {
            var orchestrationId = await starter.StartNewAsync("GiosUpdate", null);

            log.LogTrace($"GiosUpdate Durable Func started at {DateTime.UtcNow} (orchestrationId = {orchestrationId})");
        }
    }
}
