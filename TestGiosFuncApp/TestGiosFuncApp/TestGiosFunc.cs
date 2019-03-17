using System;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestGiosFuncApp
{
    public static class TestGiosFunc
    {
        public class CustomEvent
        {
            /// <summary>
            /// Gets the unique identifier for the event.
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// Gets the publisher defined path to the event subject.
            /// </summary>
            public string Subject { get; set; }

            /// <summary>
            /// Gets the registered event type for this event source.
            /// </summary>
            public string EventType { get; }

            /// <summary>
            /// Gets the time the event is generated based on the provider's UTC time.
            /// </summary>
            public string EventTime { get; }


            /// <summary>
            /// Constructor.
            /// </summary>
            public CustomEvent()
            {
                Id = Guid.NewGuid().ToString();
                EventType = "eventGridEvent";
                EventTime = DateTime.UtcNow.ToString("o");
            }
        }

        class PMDataEntry
        {
            public DateTime Date { get; set; }
            public string Value { get; set; }
        }

        class PMDataSource
        {
            public string Key { get; set; }

            public List<PMDataEntry> Values { get; set; }
        }


        [FunctionName("PM10Function")]
        public static async Task Run([TimerTrigger("0 15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"PM10 function executed at: {DateTime.Now}");
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accepts", "application/json");

                var resp = await httpClient.GetAsync(Environment.GetEnvironmentVariable("PM10Url"));

                if (resp.IsSuccessStatusCode)
                {
                    var content = await resp.Content.ReadAsStringAsync();
                    var giosDataSource = JsonConvert.DeserializeObject<PMDataSource>(content);
                    if(giosDataSource.Values.Any())
                    {
                        log.Info($"Received {giosDataSource.Values.Count} entries from external server.");
                        log.Info($"Last entry is from {giosDataSource.Values.First().Date.ToString("o")}");

                        await MergeGiosDataWithFileStorage(giosDataSource, log);
                        await SendEventToTheGrid(log);
                    }
                }
            }
        }

        private static async Task MergeGiosDataWithFileStorage(PMDataSource giosDataSource, TraceWriter log)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));

            var fileClient = storageAccount.CreateCloudFileClient();
            var share = fileClient.GetShareReference(Environment.GetEnvironmentVariable("AzureShare"));
            try
            {
                if (await share.ExistsAsync())
                {
                    var giosDir = share.GetRootDirectoryReference().GetDirectoryReference(Environment.GetEnvironmentVariable("AzureDir"));
                    var fileRef = giosDir.GetFileReference(Environment.GetEnvironmentVariable("AzureOutputFile"));
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
                        log.Info($"{giosDataSource.Values.Count} entries after merge - saving to {fileRef.Name} file...");
                    }

                    await fileRef.UploadTextAsync(JsonConvert.SerializeObject(giosDataSource));
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error occurred: {ex.Message}");
            }
        }

        private static async Task SendEventToTheGrid(TraceWriter log)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("aeg-sas-key", Environment.GetEnvironmentVariable("EventTopicKey"));

                var cev = new CustomEvent();
                cev.Subject = "test/event";

                var json = JsonConvert.SerializeObject(new List<CustomEvent> { cev });

                // Create request which will be sent to the topic
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send request
                log.Info("Sending event to Event Grid...");
                var result = await httpClient.PostAsync(Environment.GetEnvironmentVariable("EventTopicUrl"), content);

                log.Info($"Event sent with result: {result.ReasonPhrase}");
            }
        }
    }
}
