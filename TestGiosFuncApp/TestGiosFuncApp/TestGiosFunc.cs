using System;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace TestGiosFuncApp
{
    public static class TestGiosFunc
    {
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
                        await MergeGiosDataWithFileStorage(giosDataSource, log);
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
                    if(fileDataSrc != null && fileDataSrc.Values.Any())
                    {
                        foreach(var fdv in fileDataSrc.Values)
                        {
                            if(!giosDataSource.Values.Any(x => x.Date.Equals(fdv.Date)))
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
    }
}
