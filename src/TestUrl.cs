using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net;
using System.Threading;
using static PontifexDevOpsUtils.AzFunctions;

namespace PontifexDevOpsUtils
{
    public static class TestUrl
    {
        public const string TaskResultSucceeded = "succeeded";
        public const string TaskResultFailed = "failed";

        [FunctionName("TestUrl")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            using (var sr = new StreamReader(req.Body))
            using (var jsonReader = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                var release = serializer.Deserialize<AzDoRelease>(jsonReader);

                string instanceId = await starter.StartNewAsync(nameof(MonitorUrl), release);
            }

            return new OkResult();
        }

        [FunctionName(nameof(MonitorUrl))]
        public static async Task MonitorUrl(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var input = context.GetInput<AzDoRelease>();

            try {
                await SendTaskStartedEvent(context, input);

                // send task started message feed and log the message. You will see feed messages in task log UI.
                await SendTaskLogFeeds(context, input, "Task Started");

                for (var i = 1; ; i++) {
                    var urlResponse = await context.CallHttpAsync(HttpMethod.Get, new Uri(input.WebUrl));
                    var logMessage = $"{DateTime.UtcNow:0} Message {i}";
                    await SendTaskLogFeeds(context, input, logMessage);
                    if (urlResponse.StatusCode == HttpStatusCode.OK)
                    {
                        break;   
                    }
                    else
                    {
                        await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(10), CancellationToken.None);
                    }
                }

                await SendTaskCompletedEvent(context, input, TaskResultSucceeded);
            } 
            catch
            {
                await SendTaskCompletedEvent(context, input, TaskResultFailed);
            }
            finally
            {
                // Create task log entry

                // Append task log data

                // Attach task log to timeline record

                // Set task variable
            }
        }
    }
}
