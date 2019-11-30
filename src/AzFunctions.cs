using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace PontifexDevOpsUtils
{
    internal class AzDoRelease
    {
        public string PlanUrl { get; set; }
        public string ProjectId { get; set; }
        public string HubName { get; set; }
        public string PlanId { get; set; }
        public string JobId { get; set; }
        public string TimelineId { get; set; }
        public string TaskInstanceId { get; set; }
        public string AuthToken { get; set; }
        public string WebUrl { get; set; }
    }

    internal static class AzFunctions
    {
        internal static Task<DurableHttpResponse> SendTaskLogFeeds(IDurableOrchestrationContext context, AzDoRelease release, string message)
        {
            // Task feed example:
            // url : {planUri}/{projectId}/_apis/distributedtask/hubs/{hubName}/plans/{planId}/timelines/{timelineId}/records/{jobId}/feed?api-version=4.1
            // body : {"value":["2019-01-04T12:32:42.2042287Z Task started."],"count":1}

            var taskFeedUrl = $"{release.PlanUrl}/{release.ProjectId}/_apis/distributedtask/hubs/{release.HubName}/plans/{release.PlanId}/timelines/{release.TimelineId}/records/{release.JobId}/feed?api-version=4.1";

            return PostData(context, taskFeedUrl, new 
            {
                value = new[] { message },
                count = 1
            }, release.AuthToken);
        }

        internal static Task<DurableHttpResponse> SendTaskStartedEvent(IDurableOrchestrationContext context, AzDoRelease release)
        {
            // Task Event example: 
            // url: {planUri}/{projectId}/_apis/distributedtask/hubs/{hubName}/plans/{planId}/events?api-version=2.0-preview.1 
            // body : { "name": "TaskStarted", "taskId": "taskInstanceId", "jobId": "jobId" }

            const string TaskEventsUrl = "{0}/{1}/_apis/distributedtask/hubs/{2}/plans/{3}/events?api-version=2.0-preview.1";
            string taskStartedEventUrl = string.Format(TaskEventsUrl, release.PlanUrl, release.ProjectId, release.HubName, release.PlanId);
            
            return PostData(context, TaskEventsUrl, new 
            {
                name = "TaskStarted",
                jobId = release.JobId,
                taskId = release.TaskInstanceId
            }, release.AuthToken);
        }

        internal static Task<DurableHttpResponse> SendTaskCompletedEvent(IDurableOrchestrationContext context, AzDoRelease release, string result)
        {
            // Task Event example: 
            // url: {planUri}/{projectId}/_apis/distributedtask/hubs/{hubName}/plans/{planId}/events?api-version=2.0-preview.1 
            // body : { "name": "TaskStarted", "taskId": "taskInstanceId", "jobId": "jobId" }

            var taskStartedEventUrl = $"{release.PlanUrl}/{release.ProjectId}/_apis/distributedtask/hubs/{release.HubName}/plans/{release.PlanId}/events?api-version=2.0-preview.1";

            return PostData(context, taskStartedEventUrl, new 
            {
                name = "TaskCompleted",
                result = result,
                jobId = release.JobId,
                taskId = release.TaskInstanceId
            }, release.AuthToken);
        }

        internal static Task<DurableHttpResponse> PostData(IDurableOrchestrationContext context, string url, object body, string authToken)
        {
            var authHeaders = new Dictionary<string, StringValues>
            { 
                ["Authorization"] = new StringValues($"Bearer {authToken}")
            };

            var request = new DurableHttpRequest(
                HttpMethod.Post, new Uri(url), authHeaders, JsonConvert.SerializeObject(body));
            return context.CallHttpAsync(request);
        }
    }
}