//using Azure.Messaging.EventHubs;
//using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.ServiceBus;
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
    public class ServiceBusBatchBindingReceiver2
    {
        readonly TelemetryClient _insights;
        private readonly ILogger _log;

        public ServiceBusBatchBindingReceiver2(TelemetryConfiguration telemetryConfiguration, ILogger<ServiceBusBatchBindingReceiver2> log)
        {
            _insights = new TelemetryClient(telemetryConfiguration);
            _log = log;
        }

        [FunctionName(nameof(ServiceBusBatchBindingReceiver2))]
        public async Task Run(
            [ServiceBusTrigger("numbers2", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage[] messages)
        {
            _log.LogInformation($"{nameof(ServiceBusBatchBindingReceiver2)}: Batch count = {messages.Length}");

            var exceptions = new List<Exception>();

            int count = 0;
            foreach (ServiceBusReceivedMessage messageData in messages)
            {
                count++;

                try
                {
                    string messageBody = Encoding.UTF8.GetString(messageData.Body.ToArray());

                    // Replace these two lines with your processing logic.
                    _log.LogInformation($"{nameof(ServiceBusBatchBindingReceiver2)}: message = {messageBody}, total seconds since enqueue time = {DateTimeOffset.UtcNow.Subtract(messageData.EnqueuedTime).TotalSeconds}.");

                    double nowMinusMessageBodyTimeInSeconds = DateTimeOffset.TryParse(messageBody, out DateTimeOffset messageBodyDateTime) 
                        ? DateTimeOffset.UtcNow.Subtract(messageBodyDateTime).TotalSeconds 
                        : 0d;

                    // Track additional information as a custom event
                    _insights.TrackEvent(
                        $"{nameof(ServiceBusBatchBindingReceiver2)}/MessageProcessed",
                        properties: new Dictionary<string, string>
                        {
                            { "MessageId", messageData.MessageId },
                            { "Message_CorrelationId", messageData.CorrelationId },
                            { "EnqueuedTime", messageData.EnqueuedTime.ToString() },
                            { "NowMinusEnqueuedTimeInSeconds", DateTimeOffset.UtcNow.Subtract(messageData.EnqueuedTime).TotalSeconds.ToString() },
                            { "NowMinusMessageBodyTimeInSeconds", nowMinusMessageBodyTimeInSeconds.ToString() },
                            { "WEBSITE_INSTANCE_ID", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") },
                            { "COMPUTERNAME", Environment.GetEnvironmentVariable("COMPUTERNAME") },
                            { "Activity_RootId", System.Diagnostics.Activity.Current?.RootId },
                            { "MessageNumberOfBatch", $"{count}/{messages.Length}" }
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
