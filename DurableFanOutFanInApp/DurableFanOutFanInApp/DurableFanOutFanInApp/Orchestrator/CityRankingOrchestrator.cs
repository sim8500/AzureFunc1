using DurableFanOutFanInApp.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

            var tasks = codes.Select(c => context.CallActivityAsync<RankingResult>("GetAvgPMLevel", c));

            var results = (await Task.WhenAll(tasks)).Where(r => r != null);

            if(results.Any())
            {
                var maxEntry = new RankingResult { PMLevel = -1.0f };

                foreach (var r in results)
                {
                    if(r.PMLevel> maxEntry.PMLevel)
                    {
                        maxEntry = r;
                    }
                }

                await context.CallActivityAsync("SendRankingResult", JsonConvert.SerializeObject(maxEntry));
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
