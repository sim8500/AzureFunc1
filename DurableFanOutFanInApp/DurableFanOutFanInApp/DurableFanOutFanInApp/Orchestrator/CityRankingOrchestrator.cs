using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFanOutFanInApp.Orchestrator
{
    class CityRankingOrchestrator
    {
        public CityRankingOrchestrator(ILogger<CityRankingOrchestrator> logger)
        {
            this.logger = logger;
        }

        [FunctionName("CityRanking")]
        public async Task RunRanking([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if(!context.IsReplaying)
            {
                logger.LogInformation("Starting CityRanking Durable Function...");
            }

            var codes = Environment.GetEnvironmentVariable("CitySensorCodes").Split(',');

            var tasks = codes.Select(c => context.CallActivityAsync<Tuple<string, float>>("GetAvgPMLevel", c));

            var results = (await Task.WhenAll(tasks)).Where(r => r != null);

            if(results.Any())
            {
                var maxAvg = -1.0f;
                var maxEntry = results.First();

                foreach (var r in results)
                {
                    if(r.Item2 > maxAvg)
                    {
                        maxAvg = r.Item2;
                        maxEntry = r;
                    }
                }

                if (!context.IsReplaying)
                {
                    logger.LogInformation($"City with the highest PM avg. level is {maxEntry.Item1}");
                }
            }
            else
            {
                if (!context.IsReplaying)
                {
                    logger.LogWarning($"No results were returned by GiosApiClient...");
                }
            }
        }


        private readonly ILogger logger;
    }
}
