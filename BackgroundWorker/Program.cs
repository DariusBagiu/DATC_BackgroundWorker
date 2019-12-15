using AnimalDangerApi.Models;
using AnimalDangerApi.Repositories;
using BackgroundWorker.Worker;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackgroundWorker
{
    class Program
    {
        static readonly ICalculate _calculateSpeed;
        static readonly IAnimalRepo _animalRepo;


        static private CloudTable animalsTable;
        const string ServiceBusConnectionString = "";
        const string QueueName = "queue-3";
        static IQueueClient queueClient;

        static async Task Main(string[] args)
        {
            InitializeTable();
            MainAsync().GetAwaiter().GetResult();

        }
        static async Task InitializeTable()
        {
            string storageConnectionString = "DefaultEndpointsProtocol=https;"
                            + "AccountName="
                            + ";AccountKey="
                            + ";EndpointSuffix=core.windows.net";

            var account = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = account.CreateCloudTableClient();

            animalsTable = tableClient.GetTableReference("Animals");
            await animalsTable.CreateIfNotExistsAsync();
        }

        static async Task MainAsync()
        {

            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after sending all the messages.");
            Console.WriteLine("======================================================");

            // Receive Messages
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await queueClient.CloseAsync();
        }
        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            // Complete the message so that it is not received again.
            // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);

            Animal animal = JsonConvert.DeserializeObject<Animal>(Encoding.UTF8.GetString(message.Body));
            //-------------------------------------------------------------------------------------------------------------------
            //aici am incercat sa adaug verificarea in functie de ce exista in tabel cu RetrieveSingleEntity si ce o sa primeasca 
            //doar ca daca iau din tabel sub forma de Animal nu stiu cum sa iau time-stamp-ul
            //am modificat functia sa primeasca si timestampul si sa returneze TRUE daca ii buna viteza sau FALSE daca nu

            Animal initialPosition = await _animalRepo.RetrieveSingleEntity("1");

            if (_calculateSpeed.AnimalSpeed(initialPosition.Lat, initialPosition.Long, animal.Lat, animal.Long))
            {
                Console.WriteLine($"Valid coordonates Lat:{animal.Lat} Long: {animal.Long}");
                await AddAnimal(animal);
            }

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
            // to avoid unnecessary exceptions.
        }
        static public async Task<TableResult> AddAnimal(Animal animal)
        {
            if (animalsTable == null)
            {
                throw new Exception();
            }

            var insertOperation = TableOperation.Insert(animal);
            return await animalsTable.ExecuteAsync(insertOperation);
        }


        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

    }
}
