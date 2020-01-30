using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.Functions
{
    public class EventHubSender
    {
        private static IConfiguration _config = null;

        // lazy pattern for reusing client
        private static readonly Lazy<EventHubClient> _lazyClient = new Lazy<EventHubClient>(InitializeEventHubClient);
        private static EventHubClient EventHubClient => _lazyClient.Value;

        private static int _i = 0;

        [FunctionName("EventHubSender")]
        public async Task Run(
            [TimerTrigger("*/10 * * * * *")]TimerInfo timer,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"EventHubSender function executed at: {DateTime.Now}");

            _config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            _i++;

            var eventData = new EventData(
                    Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(
                            new { Number = _i, DateTime = DateTime.UtcNow })));

            log.LogInformation($"Sending event number {_i}.");
            await EventHubClient.SendAsync(eventData);
        }

        private static EventHubClient InitializeEventHubClient()
        {
            string eventHubConnectionString = _config["EventHubConnectionString"];

            if (string.IsNullOrEmpty(eventHubConnectionString))
                throw new InvalidOperationException("App Setting EventHubConnectionString is missing.");

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
            {
                EntityPath = "numbers"
            };

            return EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
        }
    }
}
