using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif


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
        private static readonly ConcurrentDictionary<string, QueueClient> sbClients = new ConcurrentDictionary<string, QueueClient>();

        public ServiceBusMqMessageFactory(string address)
        {
            this.address = address;
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
                }

                AddQueueHandler(type.Name + ".inq");
                AddQueueHandler(type.Name + ".priorityq");
            }
        }

        private void AddQueueHandler(string queueName)
        {
#if NETSTANDARD1_6
            var sbClient = new QueueClient(address, queueName, ReceiveMode.PeekLock);
            var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
            sbClient.RegisterMessageHandler(sbWorker.HandleMessageAsync,
                new MessageHandlerOptions(
                    (eventArgs) =>
                    {
                        return Task.CompletedTask;
                    }
                ) 
                { 
                    MaxConcurrentCalls = 1,
                    AutoComplete = false
                });
#else
            var options = new OnMessageOptions
            {
                // Cannot use AutoComplete because our HandleMessage throws errors into SS's handlers; this would 
                // normally release the BrokeredMessage back to the Azure Service Bus queue, which we don't actually want

                AutoComplete = false,          
                //AutoRenewTimeout = new TimeSpan()
                MaxConcurrentCalls = 1
            };

            var sbClient = QueueClient.CreateFromConnectionString(address, queueName, ReceiveMode.PeekLock);
            var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
            sbClient.OnMessage(sbWorker.HandleMessage, options);
#endif
            sbClients.GetOrAdd(queueName, sbClient);
        }

        protected internal void StopQueues()
        {
#if NETSTANDARD1_6
            sbClients.Each(async kvp => await kvp.Value.CloseAsync());
#else
            sbClients.Each(kvp => kvp.Value.Close());
#endif
        }

        protected internal QueueClient GetOrCreateClient(string queueName)
        {
            if (queueName.StartsWith(QueueNames.MqPrefix))
                queueName = queueName.ReplaceFirst(QueueNames.MqPrefix, "");

            if (sbClients.ContainsKey(queueName))
                return sbClients[queueName];

#if !NETSTANDARD1_6
            // Create queue on ServiceBus namespace if it doesn't exist
            QueueDescription qd = new QueueDescription(queueName);
            if (!namespaceManager.QueueExists(queueName))
                namespaceManager.CreateQueue(qd);
#endif

#if NETSTANDARD1_6
            var sbClient = new QueueClient(address, queueName);
#else
            var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path);
#endif

            sbClient = sbClients.GetOrAdd(queueName, sbClient);
            return sbClient;
        }
    }
}
