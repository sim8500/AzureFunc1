using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

            if (IsLatestUpdateCorrupted(updateResults, currentDate))
            {
                if(!ctx.IsReplaying)
                {
                    log.LogError("Latest update of PM data is corrupted - sending warning...");
                }

                await ctx.CallActivityAsync("SendWarning",
                                $"There was a problem with updating PM values at {currentDate.ToString("o")}");
            }
            else if(updateResults.Any(o => o.LastUpdateValue/o.AvgValue > 2.0))
            {
                await ctx.CallActivityAsync("SendWarning",
                                            $"Latest levels PM10/2.5 are significantly higher that 24h avg...");
            }
        }

        private static bool IsLatestUpdateCorrupted(PMDataOutput[] updateResults, DateTime currentDate)
        {
            return (updateResults.Any(o => !o.LastUpdateDt.HasValue)
                    || updateResults.Any(o => currentDate.Subtract(o.LastUpdateDt.Value)
                                                     .TotalHours >= 2.0)
                    || updateResults.Any(o => o.LastUpdateValue == double.NaN
                                          || o.AvgValue == double.NaN));
        }
    }
}
