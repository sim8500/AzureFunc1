using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestGiosFuncApp.Models;

namespace TestGiosFuncApp.Orchestrators
{
    public static class GiosUpdateOrchestrator
    {
        [FunctionName("GiosUpdate")]
        public static async Task RunUpdate([OrchestrationTrigger] DurableOrchestrationContext ctx,
                                           ILogger log)
        {
            var currentDate = ctx.CurrentUtcDateTime;
            if (!ctx.IsReplaying)
            {
                log.LogTrace($"Started GiosUpdate function at {currentDate.ToString("o")}...");
            }

            var updateResults = await Task.WhenAll(ctx.CallActivityAsync<PMDataOutput>("PM10Function", "pm10"),
                                                   ctx.CallActivityAsync<PMDataOutput>("PM25Function", "pm25"));

            await HandleResults(updateResults, ctx, log);
        }

        private static async Task HandleResults(PMDataOutput[] results,
                                                DurableOrchestrationContext ctx,
                                                ILogger log)
        {
            var checkTasks = new List<Task<string>>();
            foreach (var res in results)
            {
                checkTasks.Add(ctx.CallActivityAsync<string>("CheckPMDataOutput", res));
            }

            var checkResults = await Task.WhenAll(checkTasks);

            var sendTasks = new List<Task>();
            if(checkResults.Any(c => !string.IsNullOrEmpty(c)))
            {
                var strBuilder = new StringBuilder();
                strBuilder.AppendLine("Warnings:");

                foreach (var cr in checkResults.Where(c => !string.IsNullOrEmpty(c)))
                {
                    strBuilder.AppendLine(cr);
                }

                await ctx.CallActivityAsync("SendWarning", strBuilder.ToString() );
            }

        }
    }
}
