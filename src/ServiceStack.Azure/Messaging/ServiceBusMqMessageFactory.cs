#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
using QueueClient = Microsoft.Azure.ServiceBus.QueueClient;
using ReceiveMode = Microsoft.Azure.ServiceBus.ReceiveMode;
//using QueueDescription = Microsoft.ServiceBus.Messaging.QueueDescription;
//using NamespaceManager = Microsoft.ServiceBus.NamespaceManager;
//using OnMessageOptions = Microsoft.ServiceBus.Messaging.OnMessageOptions;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;


namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqMessageFactory : IMessageFactory
    {
        protected internal readonly string address;
#if !NETSTANDARD1_6
        protected internal readonly NamespaceManager namespaceManager;
#endif

        internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
        Dictionary<string, Type> queueMap;

        // A list of all Service Bus QueueClients - one per type & queue (priorityq, inq, outq, and dlq)
        private readonly Dictionary<string, QueueClient> sbClients;

        public ServiceBusMqMessageFactory(string address)
        {
            this.address = address;
            this.sbClients = new Dictionary<string, QueueClient>();
#if !NETSTANDARD1_6
            this.namespaceManager = NamespaceManager.CreateFromConnectionString(address);
#endif
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new ServiceBusMqMessageProducer(this);
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new ServiceBusMqClient(this);
        }

        public void Dispose()
        {

        }

        protected internal void StartQueues(Dictionary<Type, IMessageHandlerFactory> handlerMap)
        {
            // Create queues for each registered type
            this.handlerMap = handlerMap;

            queueMap = new Dictionary<string, Type>();

            var queues = new [] { ".inq", ".outq", ".priorityq", ".dlq" };
            foreach (var type in this.handlerMap.Keys)
            {
                foreach (string q in queues)
                {
                    string queueName = type.Name + q;

                    if (!queueMap.ContainsKey(queueName))
                        queueMap.Add(queueName, type);
#if !NETSTANDARD1_6
                    QueueDescription qd = new QueueDescription(queueName);
                    if (!namespaceManager.QueueExists(queueName))
                        namespaceManager.CreateQueue(qd);
#endif

#if NETSTANDARD1_6
                    //should be qd.Path
                    string path = null;
                    var sbClient = new QueueClient(address, path, ReceiveMode.PeekLock);
                    var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
                    sbClient.RegisterMessageHandler(sbWorker.HandleMessageAsync,
                        new MessageHandlerOptions() { MaxConcurrentCalls = 1}
                     );

                    sbClients.Add(path, sbClient);

#else
                    var options = new OnMessageOptions
                    {
                        // Cannot use AutoComplete because our HandleMessage throws errors into SS's handlers; this would 
                        // normally release the BrokeredMessage back to the Azure Service Bus queue, which we don't actually want

                        //AutoComplete = true,          
                        //AutoRenewTimeout = new TimeSpan()
                        MaxConcurrentCalls = 1
                    };

                    var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path, ReceiveMode.PeekLock);
                    var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
                    sbClient.OnMessage(sbWorker.HandleMessage, options);
                    sbClients.Add(qd.Path, sbClient);
#endif
                }

            }
        }

        protected internal void StopQueues()
        {
#if NETSTANDARD1_6
            sbClients.Each(async kvp => await kvp.Value.CloseAsync());
#else
            sbClients.Each(kvp => kvp.Value.Close());
#endif
        }

    }
}
