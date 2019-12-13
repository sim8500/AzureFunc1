using DurableFanOutFanInApp.Interfaces;
using DurableFanOutFanInApp.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DurableFanOutFanInApp
{
    class GiosApiClient : IGiosApiClient
    {
        public GiosApiClient(HttpClient httpClient,
                             ILogger<GiosApiClient> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<PMDataSource> GetDataForSensorAsync(string sensorId)
        {
            var resp = await httpClient.GetAsync(DataUrl + sensorId);
            if(resp.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<PMDataSource>(await resp.Content.ReadAsStringAsync());
            }

            return null;
        }

        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private const string DataUrl = "http://api.gios.gov.pl/pjp-api/rest/data/getData/";
    }
}
