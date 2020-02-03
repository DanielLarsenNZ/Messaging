using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.ServiceBusFunctions
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [ServiceBus("queue1", Connection = "ServiceBusConnectionString")]IAsyncCollector<Message> messages,
            ILogger log)
        {
            log.LogInformation($"Function1: executed at: {DateTime.Now}");

            const int mpm = 10;  // messages per minute
            const int dataSizeBytes = 500;
            var now = DateTime.UtcNow;

            string data = "";
            var random = new Random();
            for (int i = 0; i < dataSizeBytes; i++)
            {
                data += (char)random.Next(65, 90);
            }

            int count = 0;
            for (int i = 1; i <= mpm; i++)
            {
                var item = new Item
                {
                    Data = data,
                    DateTime = now,
                    ItemCount = i,
                    Id = Guid.NewGuid().ToString("N")
                };

                var message = new Message(
                        Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(item)));

                // then send the message
                await messages.AddAsync(message);
                count++;
            }

            log.LogInformation($"Function1: Sending batch of {count} messages.");
        }
    }

    class Item
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "pk")]
        public string PK { get; set; }

        public string Colour { get; set; }
        public int ItemCount { get; set; }
        public DateTime DateTime { get; set; }
        public string Data { get; set; }
    }
}
