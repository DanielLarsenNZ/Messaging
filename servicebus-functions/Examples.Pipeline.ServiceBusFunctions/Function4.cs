using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.ServiceBusFunctions
{
    public static class Function4
    {
        [FunctionName("Function4")]
        public static async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [ServiceBus("queue3-session", Connection = "ServiceBusConnectionString")]IAsyncCollector<Message> messages,
            ILogger log)
        {
            log.LogInformation($"Function1: executed at: {DateTime.Now}");

            const int dataSizeBytes = 500;
            const int sessionCount = 10;
            const int messagesPerSession = 4;
            var now = DateTime.UtcNow;

            string data = "";
            var random = new Random();
            for (int i = 0; i < dataSizeBytes; i++)
            {
                data += (char)random.Next(65, 90);
            }

            for (int j = 0; j < sessionCount; j++)
            {
                string sessionId = Guid.NewGuid().ToString("N");
                int count = 0;
                for (int i = 1; i <= messagesPerSession; i++)
                {
                    var item = new Item
                    {
                        Data = data,
                        DateTime = now,
                        ItemCount = i,
                        Id = Guid.NewGuid().ToString("N")
                    };

                    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)))
                    {
                        SessionId = sessionId
                    };

                    // then send the message
                    await messages.AddAsync(message);
                    count++;
                }

                log.LogInformation($"Function4: Sending batch of {count} messages.");
            }
        }
    }
}
