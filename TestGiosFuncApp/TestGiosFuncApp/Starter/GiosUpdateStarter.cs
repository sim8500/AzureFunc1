using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;
using TestGiosFuncApp.Models;

namespace TestGiosFuncApp.Starter
{
    public static class GiosUpdateStarter
    {
        [FunctionName("GiosUpdateStarter")]
        public static async Task Run([TimerTrigger("1 15 * * * *")]TimerInfo myTimer,
                                    [OrchestrationClient] DurableOrchestrationClient starter,
                                    ILogger log)
        {
            var orchestrationId = await starter.StartNewAsync("GiosUpdate", null);

            log.LogTrace($"GiosUpdate Durable Func started at {DateTime.UtcNow} (orchestrationId = {orchestrationId})");
        }

        [FunctionName("TestTimeout")]
        public static async Task<IActionResult> RunTest([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
                                                        HttpRequest req, ILogger log)
        {
            log.LogInformation("Started!!!");
            await Task.Delay(10*3600*1000);
            log.LogInformation("Stopped!!!");
            return new OkResult();
        }

        [FunctionName("WarningsConverter")]
        public static async Task RunOnQueueMsg([QueueTrigger("tst-queue-poison")]string itemText,
            DateTimeOffset insertionTime,
            [Table("GiosWarningEntriesTable", Connection ="AzureWebJobsStorage")] CloudTable warningsTable,
            ILogger log)
        {
            var entity = new WarningEntity(insertionTime.ToString("dd-MM-yyyy"), insertionTime.ToString("HH-mm-ss"))
            {
                Text = itemText
            };
            try
            {
                var res = await warningsTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            }

            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
