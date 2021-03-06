using Examples.Pipeline.Commands;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Examples.Pipeline.WebJobs
{
    public static class NewFile
    {
        private static IConfiguration _config = null;

        // lazy pattern for reusing client
        private static readonly Lazy<EventHubClient> _lazyClient = new Lazy<EventHubClient>(InitializeEventHubClient);
        private static EventHubClient EventHubClient => _lazyClient.Value;

        /// <summary>
        /// Process every Transactions file that is created/modified in the Data Storage Account, 
        /// data container. Parse each line to a Command and send to Event Hubs as batches of EventData.
        /// </summary>
        /// <remarks>Note that the Event Hubs output binding is not used - the client is used directly
        /// for more control and logging.</remarks>
        [FunctionName("NewFile")]
        [Singleton]
        public static async Task Run(
            [BlobTrigger("data/{filename}", Connection = "DataStorageConnectionString")]Stream blob,
            string filename,
            ILogger log)
        {
            const int MaxErrorCount = 5;    // Maximum number of line parse errors before exception thrown

            log.LogInformation($"NewFile Blob trigger function Processed blob\n Name:{filename} \n Size: {blob.Length} Bytes");

            // Can't get DI to work in WebJobs SDK :|
            _config = Program.Services.GetService<IConfiguration>();
            
            // Each line in the CSV is a transaction. Create Command as Event Data for each transaction.
            // Batches cannot be larger than 1MB so split the events into batches.
            var batches = new List<EventDataBatch>();
            batches.Add(EventHubClient.CreateBatch());
            int batchNo = 0;
            decimal checksum = 0;
            int i = 0;
            int errorCount = 0;

            using (StreamReader reader = new StreamReader(blob))
            {
                while (reader.Peek() >= 0)
                {
                    i++;

                    if (i == 1)
                    {
                        // ignore header
                        reader.ReadLine();
                        continue;
                    }

                    // Parse line and create Command
                    TransactionCommand command = null;
                    try
                    {
                        command = ParseLineToCommand(log, filename, i, reader.ReadLine());
                    }
                    catch (InvalidOperationException ex)
                    {
                        errorCount++;

                        log.LogError(ex, $"errorCount = {errorCount}. {ex.Message}");

                        if (errorCount > MaxErrorCount) throw;
                    }

                    if (!batches[batchNo].TryAdd(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command)))))
                    {
                        // If TryAdd() fails, batch is full
                        log.LogInformation($"Batch {batchNo} is full at line {i}. Adding new batch");
                        // Create a new batch and add event
                        batches.Add(EventHubClient.CreateBatch());
                        batchNo++;
                        if (!batches[batchNo].TryAdd(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command)))))
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    // Taking a running checksum to compare with other parts of the pipeline
                    checksum += command.Amount;
                }
            }

            // Send all transaction events in batched operations
            // https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send
            int eventCount = 0;
            foreach (EventDataBatch batch in batches)
            {
                log.LogInformation($"Sending batch of {batch.Count} events.");
                await EventHubClient.SendAsync(batch);
                eventCount += batch.Count;
            }

            log.LogInformation($"Filename: {filename}");
            log.LogInformation($"Lines (incl. header): {i}");
            log.LogInformation($"Events: {eventCount}");
            log.LogInformation($"Batches: {batches.Count}");
            log.LogInformation($"Sum of amount: {checksum}");
        }

        private static TransactionCommand ParseLineToCommand(ILogger log, string filename, int lineNumber, string line)
        {
            // id,acc_number,date_time,amount,merchant,authorization
            const int IdField = 0;
            const int AccountNumberField = 1;
            const int DateTimeField = 2;
            const int AmountField = 3;
            const int MerchantField = 4;
            const int AuthorizationField = 5;

            string[] fields = line.Split(',');

            if (!decimal.TryParse(fields[AmountField].Replace("$", ""), out decimal amount))
            {
                string errorMessage = $"Could not parse amount to decimal: line #{lineNumber} field #{AmountField} \"{fields[AmountField]}\"";
                log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            if (!DateTime.TryParse(fields[DateTimeField], out DateTime dateTime))
            {
                string errorMessage = $"Could not parse date_time to DateTime: line #{lineNumber} field #{DateTimeField} \"{fields[DateTimeField]}\"";
                log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            TransactionCommand command;

            if (amount >= 0)
            {
                // Credit
                command = new CreditAccountCommand
                {
                    CreditAmount = amount
                };
            }
            else
            {
                // Debit
                command = new DebitAccountCommand
                {
                    DebitAmount = Math.Abs(amount)
                };
            }

            command.Id = Guid.NewGuid();
            command.Filename = filename;
            command.AccountNumber = fields[AccountNumberField];
            command.AuthorizationCode = fields[AuthorizationField];
            command.MerchantId = fields[MerchantField];
            command.TransactionDateTime = dateTime;
            command.Amount = amount;
            command.TransactionId = fields[IdField];

            return command;
        }

        private static EventHubClient InitializeEventHubClient()
        {
            string eventHubConnectionString = _config["EventHubConnectionString"];

            if (string.IsNullOrEmpty(eventHubConnectionString))
                throw new InvalidOperationException("App Setting EventHubConnectionString is missing.");

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
            {
                EntityPath = Common.EventHubName
            };

            return EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
        }
    }
}
