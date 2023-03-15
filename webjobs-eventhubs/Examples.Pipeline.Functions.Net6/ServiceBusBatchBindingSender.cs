//using Azure.Messaging.EventHubs;
//using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Amqp.Framing;
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
    public class ServiceBusBatchBindingSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger _log;
        readonly TelemetryClient _insights;

        public ServiceBusBatchBindingSender(
            TelemetryConfiguration telemetryConfiguration,
            IConfiguration config, 
            ILogger<ServiceBusBatchBindingSender> log)
        {
            _insights = new TelemetryClient(telemetryConfiguration);
            _config = config;
            _log = log;
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}00")]
        public async Task Run1(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")]IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}10")]
        public async Task Run2(
            [TimerTrigger("10 */1 * * * *")] TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")] IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}20")]
        public async Task Run3(
            [TimerTrigger("20 */1 * * * *")] TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")] IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}30")]
        public async Task Run4(
            [TimerTrigger("30 */1 * * * *")] TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")] IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}40")]
        public async Task Run5(
            [TimerTrigger("40 */1 * * * *")] TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")] IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        // Timer trigger every 1 minute
        [FunctionName($"{nameof(ServiceBusBatchBindingSender)}50")]
        public async Task Run6(
            [TimerTrigger("50 */1 * * * *")] TimerInfo timer,
            //[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            //ILogger log,
            [ServiceBus("numbers1", Connection = "ServiceBusConnectionString")] IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            await RunServiceBusBatchBindingSender(outputMessages);
        }

        private async Task RunServiceBusBatchBindingSender(IAsyncCollector<ServiceBusMessage> outputMessages)
        {
            _log.LogInformation($"{nameof(ServiceBusBatchBindingSender)} function executed at: {DateTime.Now}");

            int count = 0;
            if (!int.TryParse(_config["NumberOfEventsToSend"], out int numberOfMessagesToSend)) numberOfMessagesToSend = 100;

            for (int i = 1; i < numberOfMessagesToSend + 1; i++)
            {
                var messageData = new ServiceBusMessage(
                    Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("R")))
                {
                    MessageId = Guid.NewGuid().ToString()
                };

                // then send the message
                await outputMessages.AddAsync(messageData);
                count++;

                _insights.TrackEvent(
                    $"{nameof(ServiceBusBatchBindingSender)}/MessageBatchSend",
                    properties: new Dictionary<string, string>
                    {
                        { "WEBSITE_INSTANCE_ID", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") },
                        { "COMPUTERNAME", Environment.GetEnvironmentVariable("COMPUTERNAME") },
                        { "Activity_RootId", System.Diagnostics.Activity.Current?.RootId },
                        { "MessageId", messageData.MessageId },
                        { "Message_CorrelationId", messageData.CorrelationId },
                        { "MessageNumberOfBatch", $"{count}/{numberOfMessagesToSend}" }
                    });
            }

            _log.LogInformation($"Sending batch of {count} messages.");
        }
    }
}
