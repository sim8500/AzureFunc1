using DurableFanOutFanInApp.Interfaces;
using DurableFanOutFanInApp.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFanOutFanInApp.Activity
{
    class AvgPMLevelActivityFunc
    {
        public AvgPMLevelActivityFunc(ILogger<AvgPMLevelActivityFunc> logger,
                                        IGiosApiClient giosApiClient)
        {
            this.logger = logger;
            this.giosApiClient = giosApiClient;
        }

        [FunctionName("GetAvgPMLevel")]
        public async Task<RankingResult> Run([ActivityTrigger]string trigger)
        {
            var args = trigger.Split('=');
            if(args.Length == 2)
            {            
                logger.LogInformation($"Running AvgPMLevel calculation for city={args[0]}, sensorId={args[1]}...");
                var result = await giosApiClient.GetDataForSensorAsync(args[1]);
                if (result != null)
                {
                    var avgLevel = CalculateAvgLevel(result.Values.Take(12));
                    logger.LogInformation($"Got AvgPMLevel for city={args[0]}: {avgLevel}.");
                    return new RankingResult { City = args[0], PMLevel = avgLevel };
                }

                logger.LogWarning($"No data for AvgPMLevel calculation for city={args[0]}");

                return new RankingResult { City = args[0], PMLevel = -1.0f };
            }
            else
            {
                return null;
            }
        }

        private float CalculateAvgLevel(IEnumerable<Models.PMDataEntry> pmDataEntries)
        {
            return pmDataEntries.Aggregate(Tuple.Create(0.0f, 0),
                                            (sum, entry) =>
                                            {
                                                if (float.TryParse(entry.Value, out var v))
                                                {
                                                    return Tuple.Create(sum.Item1 + v, sum.Item2 + 1);
                                                }
                                                else
                                                {
                                                    //ignore the entry
                                                    return sum;
                                                }
                                            },
                                            (ac) => ac.Item2 > 0 ? (ac.Item1 / ac.Item2) : -1.0f);
        }

        [FunctionName("SendRankingResult")]
        [return: Queue("ranking-results", Connection = "AzureWebJobsStorage")]
        public string SendResult([ActivityTrigger]string trigger)
        {
            if(!string.IsNullOrWhiteSpace(trigger))
            {
                return trigger;
            }

            throw new ArgumentNullException(nameof(trigger));
        }

        private readonly ILogger logger;
        private readonly IGiosApiClient giosApiClient;
    }
}
