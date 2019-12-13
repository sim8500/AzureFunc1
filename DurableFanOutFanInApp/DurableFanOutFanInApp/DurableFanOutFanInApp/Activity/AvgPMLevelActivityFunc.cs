using DurableFanOutFanInApp.Interfaces;
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
        public async Task<Tuple<string,float>> Run([ActivityTrigger]string trigger)
        {
            var args = trigger.Split('=');
            if(args.Length == 2)
            {
                
                logger.LogInformation($"Running AvgPMLevel calculation for city={args[0]}, sensorId={args[1]}...");
                var result = await giosApiClient.GetDataForSensorAsync(args[1]);
                if (result != null)
                {
                    var avgLevel = CalculateAvgLevel(result.Values);
                    logger.LogInformation($"Got AvgPMLevel for city={args[0]}: {avgLevel}.");
                    return Tuple.Create(args[0], avgLevel);
                }

                logger.LogWarning($"No data for AvgPMLevel calculation for city={args[0]}");

                return Tuple.Create(args[0],-1.0f);
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

        private readonly ILogger logger;
        private readonly IGiosApiClient giosApiClient;
    }
}
