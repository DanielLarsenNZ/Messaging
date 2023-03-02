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
    public class EventHubBatchBindingReceiver2
    {
        readonly TelemetryClient _insights;

        public EventHubBatchBindingReceiver2(TelemetryConfiguration telemetryConfiguration)
        {
            _insights = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("EventHubBatchBindingReceiver2")]
        public async Task Run(
            [EventHubTrigger("numbers2", Connection = "EventHubConnectionString")] EventData[] events,
            ILogger log,
            PartitionContext partitionContext)
        {
            log.LogInformation($"EventHubBatchBindingReceiver2: Batch count = {events.Length}, Partition = {partitionContext.PartitionId}");

            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.ToArray());

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"EventHubBatchBindingReceiver2: message = {messageBody}");

                    // Track additional information as a custom event
                    _insights.TrackEvent(
                        "EventHubBatchBindingReceiver2/EventProcessed",
                        properties: new Dictionary<string, string>
                        {
                            { "partitionId", partitionContext.PartitionId },
                            { "WEBSITE_INSTANCE_ID", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") },
                            { "COMPUTERNAME", Environment.GetEnvironmentVariable("COMPUTERNAME") }
                        });

                    await Task.Yield();
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
