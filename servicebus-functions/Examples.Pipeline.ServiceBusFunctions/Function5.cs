using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Pipeline.ServiceBusFunctions
{
    public static class Function5
    {
        [FunctionName("Function5")]
        public static async Task Run([ServiceBusTrigger(
            "queue3-session",
            Connection = "ServiceBusConnectionString",
            IsSessionsEnabled = true)]Message message,
            [ServiceBus("queue4-session", Connection = "ServiceBusConnectionString")]IAsyncCollector<Message> messages,
            ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);

            // Replace these two lines with your processing logic.
            log.LogInformation($"Function5: message = {messageBody}");

            // send processed message to next hub
            var newMessage = new Message(Encoding.UTF8.GetBytes(messageBody))
            {
                SessionId = message.SessionId
            };
            await messages.AddAsync(newMessage);
        }
    }
}
