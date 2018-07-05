using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
#if NETSTANDARD2_0
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
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
#else
        protected internal readonly ManagementClient managementClient;
#endif

        internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
        Dictionary<string, Type> queueMap;

        // A list of all Service Bus QueueClients - one per type & queue (priorityq, inq, outq, and dlq)
        private static readonly ConcurrentDictionary<string, QueueClient> sbClients = new ConcurrentDictionary<string, QueueClient>();

        public ServiceBusMqServer MqServer { get; }

        public ServiceBusMqMessageFactory(ServiceBusMqServer mqServer, string address)
        {
            this.MqServer = mqServer;
            this.address = address;
#if !NETSTANDARD2_0
            this.namespaceManager = NamespaceManager.CreateFromConnectionString(address);
#else
            this.managementClient = new ManagementClient(address);
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

            var mqSuffixes = new[] { ".inq", ".outq", ".priorityq", ".dlq" };
            foreach (var type in this.handlerMap.Keys)
            {
                foreach (var mqSuffix in mqSuffixes)
                {
                    var queueName = QueueNames.ResolveQueueNameFn(type.Name, mqSuffix);
                    queueName = queueName.SafeQueueName();

                    if (!queueMap.ContainsKey(queueName))
                        queueMap.Add(queueName, type);
                    RegisterQueueByName(queueName);
                }

                var mqNames = new QueueNames(type);
                AddQueueHandler(mqNames.In);
                AddQueueHandler(mqNames.Priority);
            }
        }

        private void AddQueueHandler(string queueName)
        {
            queueName = queueName.SafeQueueName();
            var sbClient = GetOrCreateClient(queueName);
#if NETSTANDARD2_0
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

        protected internal QueueClient GetOrCreateClient(string queueName, ReceiveMode receiveMode = ReceiveMode.PeekLock)
        {
            queueName = queueName.SafeQueueName();

            if (sbClients.ContainsKey(queueName))
                return sbClients[queueName];

            var qd = RegisterQueueByName(queueName);

#if NETSTANDARD2_0
            var sbClient = new QueueClient(address, qd.Path, receiveMode);
#else
            var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path, receiveMode);
#endif
            sbClient = sbClients.GetOrAdd(queueName, sbClient);
            return sbClient;
        }

        protected internal QueueDescription RegisterQueueByName(string queueName)
        {
            var mqDesc = new QueueDescription(queueName);
#if !NETSTANDARD2_0
                    if (!namespaceManager.QueueExists(queueName))
                        namespaceManager.CreateQueue(mqDesc);
#else
            try
            {
                managementClient.QueueExistsAsync(queueName)
                    .ContinueWith(async asc =>
                    {
                        if (!asc.Result)
                        {
                            await managementClient.CreateQueueAsync(mqDesc)
                                .ConfigureAwait(continueOnCapturedContext: true);
                        }
                    });
            }
            catch (AggregateException aex)
            {
                throw aex.Flatten();
            }
#endif
            return mqDesc;
        }
    }
}
