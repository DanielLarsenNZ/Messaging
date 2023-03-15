using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.Functions
{
    public class EventHubBatchBindingSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger _log;
        //readonly TelemetryClient _insights;

        public EventHubBatchBindingSender(
            //TelemetryConfiguration telemetryConfiguration,
            IConfiguration config, 
            ILogger<EventHubBatchBindingSender> log)
        {
            //_insights = new TelemetryClient(telemetryConfiguration);
            _config = config;
            _log = log;
        }

        // Timer trigger every 1 minute
        //[Disable]
        [FunctionName("EventHubBatchBindingSender")]
        public async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [EventHub("numbers1", Connection = "EventHubConnectionString")]IAsyncCollector<EventData> outputEvents)
        {
            _log.LogInformation($"EventHubBatchBindingSender function executed at: {DateTime.Now}");

            int count = 0;
            if (!int.TryParse(_config["NumberOfEventsToSend"], out int numberOfEventsToSend)) numberOfEventsToSend = 100;
            
            for (int i = 1; i < numberOfEventsToSend + 1; i++)
            {
                var eventData = new EventData(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new { Number = i, DateTime = DateTime.UtcNow })));

                // then send the message
                await outputEvents.AddAsync(eventData);
                count++;
            }

            //_insights.TrackEvent(
            //    "EventHubBatchBindingSender/EventBatchSend",
            //            properties: new Dictionary<string, string>
            //            {
            //                            { "EventsInBatch", count.ToString() },
            //                            { "WEBSITE_INSTANCE_ID", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") },
            //                            { "COMPUTERNAME", Environment.GetEnvironmentVariable("COMPUTERNAME") },
            //                            { "Activity.RootId", System.Diagnostics.Activity.Current?.RootId }
            //    });

            _log.LogInformation($"Sending batch of {count} events.");
        }
    }
}
