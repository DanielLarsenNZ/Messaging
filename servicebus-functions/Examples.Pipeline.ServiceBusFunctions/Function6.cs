using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Examples.Pipeline.ServiceBusFunctions
{
    public static class Function6
    {
        [FunctionName("Function6")]
        public static void Run([ServiceBusTrigger(
            "queue4-session",
            Connection = "ServiceBusConnectionString",
            IsSessionsEnabled = true)]Message message,
            ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"Function6: message = {messageBody}");
        }
    }
}
