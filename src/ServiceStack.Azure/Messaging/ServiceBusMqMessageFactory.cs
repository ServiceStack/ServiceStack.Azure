using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
#if NETSTANDARD2_0
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
#if !NETSTANDARD2_0
        protected internal readonly NamespaceManager namespaceManager;
#endif

        internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
        Dictionary<string, Type> queueMap;

        // A list of all Service Bus QueueClients - one per type & queue (priorityq, inq, outq, and dlq)
        private static readonly ConcurrentDictionary<string, QueueClient> sbClients = new ConcurrentDictionary<string, QueueClient>();

        public ServiceBusMqMessageFactory(string address)
        {
            this.address = address;
#if !NETSTANDARD2_0
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

            var mqSuffixes = new [] { ".inq", ".outq", ".priorityq", ".dlq" };
            foreach (var type in this.handlerMap.Keys)
            {
                foreach (var mqSuffix in mqSuffixes)
                {
                    var queueName = QueueNames.ResolveQueueNameFn(type.Name, mqSuffix);
                    queueName = queueName.SafeQueueName();

                    if (!queueMap.ContainsKey(queueName))
                        queueMap.Add(queueName, type);
#if !NETSTANDARD2_0
                    var mqDesc = new QueueDescription(queueName);
                    if (!namespaceManager.QueueExists(queueName))
                        namespaceManager.CreateQueue(mqDesc);
#endif
                }

                var mqNames = new QueueNames(type);
                AddQueueHandler(mqNames.In);
                AddQueueHandler(mqNames.Priority);
            }
        }

        private void AddQueueHandler(string queueName)
        {
            queueName = queueName.SafeQueueName();

#if NETSTANDARD2_0
            var sbClient = new QueueClient(address, queueName, ReceiveMode.PeekLock);
            var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
            sbClient.RegisterMessageHandler(sbWorker.HandleMessageAsync,
                new MessageHandlerOptions(
                    (eventArgs) => Task.CompletedTask) 
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
#if NETSTANDARD2_0
            sbClients.Each(async kvp => await kvp.Value.CloseAsync());
#else
            sbClients.Each(kvp => kvp.Value.Close());
#endif
            sbClients.Clear();
        }

        protected internal QueueClient GetOrCreateClient(string queueName)
        {
            queueName = queueName.SafeQueueName();
            
            if (sbClients.ContainsKey(queueName))
                return sbClients[queueName];

#if !NETSTANDARD2_0
            // Create queue on ServiceBus namespace if it doesn't exist
            var qd = new QueueDescription(queueName);
            if (!namespaceManager.QueueExists(queueName))
                namespaceManager.CreateQueue(qd);
#endif

#if NETSTANDARD2_0
            var sbClient = new QueueClient(address, queueName);
#else
            var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path);
#endif

            sbClient = sbClients.GetOrAdd(queueName, sbClient);
            return sbClient;
        }
    }
}
