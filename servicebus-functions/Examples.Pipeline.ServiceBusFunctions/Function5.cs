using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Examples.Pipeline.ServiceBusFunctions
{
    public static class Function5
    {
        [FunctionName("Function5")]
        public static void Run([ServiceBusTrigger(
            "queue3-session",
            Connection = "ServiceBusConnectionString",
            IsSessionsEnabled = true)]Message message,
            ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"Function5: message = {messageBody}");
        }
    }
}
