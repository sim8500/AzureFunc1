using System;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.Queue;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestGiosFuncApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Storage.Auth;

namespace TestGiosFuncApp.Activities
{
    public static class TestGiosFunc
    {
        [FunctionName("PM10Function")]
        public static async Task<PMDataOutput> RunPM10([ActivityTrigger]string trigger, ILogger log)
        {
            log.LogTrace($"PM10 function executed at: {DateTime.Now}");

            var pm10Url = Environment.GetEnvironmentVariable("PM10Url");
            var pm10StorageFile = Environment.GetEnvironmentVariable("PM10StorageFile");
            var pm10PlotName = Environment.GetEnvironmentVariable("PM10PlotName");

            return await GetGiosDataAndProcess(pm10Url, pm10StorageFile, pm10PlotName, log);
        }

        [FunctionName("PM25Function")]
        public static async Task<PMDataOutput> RunPM25([ActivityTrigger]string trigger, ILogger log)
        {
            log.LogTrace($"PM2.5 function executed at: {DateTime.Now}");

            var pm25Url = Environment.GetEnvironmentVariable("PM25Url");
            var pm25StorageFile = Environment.GetEnvironmentVariable("PM25StorageFile");
            var pm25PlotName = Environment.GetEnvironmentVariable("PM25PlotName");

            return await GetGiosDataAndProcess(pm25Url, pm25StorageFile, pm25PlotName, log);
        }

        [FunctionName("CheckPMDataOutput")]
        public static string CheckResult([ActivityTrigger] PMDataOutput result, ILogger log)
        {
            if (IsLatestUpdateCorrupted(result))
            {
               log.LogError("Latest update of PM data is corrupted - sending warning...");

                return $"There was a problem with updating PM values of {result.Name} at {DateTime.UtcNow}";
            }
            else
            {
                var lastUpdateThreshold = Double.Parse(Environment.GetEnvironmentVariable("LastUpdateToAvgRatioThreshold"));
                if(result.LastUpdateValue/result.AvgValue > lastUpdateThreshold)
                {
                    log.LogInformation("Threshold for last update value has been exceeded...");
                    return $"Latest levels of {result.Name} are significantly higher that 24h avg...";
                }

            }
            return null;
        }

        [FunctionName("SendWarning")]
        public static async Task SendWarning([ActivityTrigger] string warningText, ILogger log)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var token = await GetTokenAsync("https://storage.azure.com/");
            TokenCredential tokenCredential = new TokenCredential(token);

            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);
            var queueClient = new CloudQueueClient(new Uri("https://giosplotlystorage.queue.core.windows.net/"), storageCredentials);
            var queueRef = queueClient.GetQueueReference("tst-queue");

            await queueRef.AddMessageAsync(new CloudQueueMessage(warningText));
        }

        private static async Task<PMDataOutput> GetGiosDataAndProcess(string giosUrl, string storageFile, string plotName, ILogger log)
        {
            PMDataOutput lastUpdate = null;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accepts", "application/json");

                var resp = await httpClient.GetAsync(giosUrl);

                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync();
                    var giosDataSource = JsonConvert.DeserializeObject<PMDataSource>(content);
                    if (giosDataSource.Values.Any())
                    {
                        lastUpdate = GetCurrentDataOutput(giosDataSource);
                        await MergeGiosDataWithFileStorage(giosDataSource, storageFile, log);
                        //await SendEventToTheGrid(storageFile, plotName, log);
                    }
                }
            }

            return lastUpdate;
        }

        private static PMDataOutput GetCurrentDataOutput(PMDataSource source)
        {
            var lastEntry = source.Values.First();
            double v = Double.NaN;
            Double.TryParse(lastEntry?.Value, out v);

            var avg = Double.NaN;

            if(source.Values.All(x => !string.IsNullOrEmpty(x.Value)))
            {
                avg = source.Values
                            .Take(24)
                            .Select(x => Double.Parse(x?.Value))
                            .Average();
            }


            return new PMDataOutput
            {
                Name = source.Key,
                LastUpdateDt = lastEntry?.Date.ToUniversalTime(),
                LastUpdateValue = v,
                AvgValue = avg
            };
        }

        private static async Task MergeGiosDataWithFileStorage(PMDataSource giosDataSource, string storageFile, ILogger log)
        {
            log.LogInformation("Before GetTokenAsync()...");
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var token = await GetTokenAsync("https://storage.azure.com/");
            TokenCredential tokenCredential = new TokenCredential(token);

            log.LogInformation($"Token acquired and is not null: {!string.IsNullOrEmpty(token)}");

            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);
            var fileClient = new CloudFileClient(new Uri("https://giosplotlystorage.file.core.windows.net/"), storageCredentials);
            log.LogInformation($"FileClient created");

            var share = fileClient.GetShareReference(Environment.GetEnvironmentVariable("AzureShare"));
            try
            {
                if (await share.ExistsAsync())
                {
                    var giosDir = share.GetRootDirectoryReference().GetDirectoryReference(Environment.GetEnvironmentVariable("AzureDir"));
                    var fileRef = giosDir.GetFileReference(storageFile);
                    var fileDataSrc = JsonConvert.DeserializeObject<PMDataSource>(await fileRef.DownloadTextAsync());
                    if (fileDataSrc != null && fileDataSrc.Values.Any())
                    {
                        foreach (var fdv in fileDataSrc.Values)
                        {
                            if (!giosDataSource.Values.Any(x => x.Date.Equals(fdv.Date)))
                            {
                                giosDataSource.Values.Add(fdv);
                            }
                        }
                        log.LogInformation($"{giosDataSource.Values.Count} entries after merge - saving to {fileRef.Name} file...");
                    }

                    await fileRef.UploadTextAsync(JsonConvert.SerializeObject(giosDataSource));
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error occurred: {ex.Message}");
            }
        }

        private static async Task SendEventToTheGrid(string storageFile, string plotName, ILogger log)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("aeg-sas-key", Environment.GetEnvironmentVariable("EventTopicKey"));

                var cev = new CustomEvent();

                cev.Subject = "PM-measurement-updated";
                cev.Data = new GiosDataEvent { StorageFileName = storageFile, PlotName = plotName };

                var json = JsonConvert.SerializeObject(new List<CustomEvent> { cev });

                // Create request which will be sent to the topic
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send request
                log.LogTrace("Sending event to Event Grid...");
                var result = await httpClient.PostAsync(Environment.GetEnvironmentVariable("EventTopicUrl"), content);

                log.LogTrace($"Event sent with result: {result.ReasonPhrase}");
            }
        }

        private static async Task<string> GetTokenAsync(string resId)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(resId);
        }
        private static bool IsLatestUpdateCorrupted(PMDataOutput updateResult)
        {
            var currentDate = DateTime.UtcNow;
            var threshold = Double.Parse(Environment.GetEnvironmentVariable("UpdateDelayThresholdMinutes"));

            return (!updateResult.LastUpdateDt.HasValue
                    || currentDate.Subtract(updateResult.LastUpdateDt.Value)
                                  .TotalMinutes >= threshold
                    || updateResult.LastUpdateValue == double.NaN                                      
                    || updateResult.AvgValue == double.NaN);
        }
    }
}
