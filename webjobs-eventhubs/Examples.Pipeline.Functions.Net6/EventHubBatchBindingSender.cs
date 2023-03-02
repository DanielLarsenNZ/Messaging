using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.Functions
{
    public class EventHubBatchBindingSender
    {
        private readonly IConfiguration _config;
        public EventHubBatchBindingSender(IConfiguration config)
        {
            _config = config;
        }

        // Timer trigger every 1 minute
        [FunctionName("EventHubBatchBindingSender")]
        public async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer,
            ILogger log,
            [EventHub("numbers1", Connection = "EventHubConnectionString")]IAsyncCollector<EventData> outputEvents)
        {
            log.LogInformation($"EventHubBatchBindingSender function executed at: {DateTime.Now}");

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

            log.LogInformation($"Sending batch of {count} events.");
        }
    }
}
