using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.Functions
{
    public class EventHubBatchBindingReceiver
    {
        readonly TelemetryClient _insights;

        public EventHubBatchBindingReceiver(TelemetryConfiguration telemetryConfiguration)
        {
            _insights = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("EventHubBatchBindingReceiver")]
        public async Task Run(
            [EventHubTrigger("numbers1", Connection = "EventHubConnectionString")] EventData[] events,
            ILogger log,
            PartitionContext partitionContext,
            [EventHub("numbers2", Connection = "EventHubConnectionString")]IAsyncCollector<EventData> outputEvents)
        {
            log.LogInformation($"EventHubBatchBindingReceiver: Batch count = {events.Length}, Partition = {partitionContext.PartitionId}");

            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.ToArray());

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"EventHubBatchBindingReceiver: message = {messageBody}");

                    _insights.TrackEvent(
                        "EventHubBatchBindingReceiver/EventProcessed",
                        properties: new Dictionary<string, string>
                        {
                            { "partitionId", partitionContext.PartitionId },
                            { "WEBSITE_INSTANCE_ID", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") },
                            { "COMPUTERNAME", Environment.GetEnvironmentVariable("COMPUTERNAME") },
                            { "Activity.RootId", System.Diagnostics.Activity.Current?.RootId }
                        });

                    // send processed message to next hub
                    var newEventData = new EventData(Encoding.UTF8.GetBytes(messageBody));
                    await outputEvents.AddAsync(newEventData);
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                    _insights.TrackException(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
