using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqMessageFactory : IMessageFactory
    {
        protected internal readonly string address;
        protected internal readonly NamespaceManager namespaceManager;

        internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
        Dictionary<string, Type> queueMap;

        // A list of all Service Bus QueueClients - one per type & queue (priorityq, inq, outq, and dlq)
        private readonly Dictionary<string, QueueClient> sbClients;

        public ServiceBusMqMessageFactory(string address)
        {
            this.address = address;
            this.sbClients = new Dictionary<string, QueueClient>();
            this.namespaceManager = NamespaceManager.CreateFromConnectionString(address);
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

            this.queueMap = new Dictionary<string, Type>();

            string[] queues = new string[] { ".inq", ".outq", ".priorityq", ".dlq" };
            foreach (var type in this.handlerMap.Keys)
            {
                foreach (string q in queues)
                {
                    string queueName = type.Name + q;

                    if (!queueMap.ContainsKey(queueName))
                        queueMap.Add(queueName, type);

                    QueueDescription qd = new QueueDescription(queueName);
                    if (!namespaceManager.QueueExists(queueName))
                        namespaceManager.CreateQueue(qd);

                    var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path, ReceiveMode.PeekLock);
                    var sbWorker = new ServiceBusMqWorker(this, this.CreateMessageQueueClient(), queueName);

                    OnMessageOptions options = new OnMessageOptions
                    {
                        // Cannot use AutoComplete because our HandleMessage throws errors into SS's handlers; this would 
                        // normally release the BrokeredMessage back to the Azure Service Bus queue, which we don't actually want

                        //AutoComplete = true,          
                        //AutoRenewTimeout = new TimeSpan()
                        MaxConcurrentCalls = 1
                    };
                    sbClient.OnMessage(sbWorker.HandleMessage, options);
                    sbClients.Add(qd.Path, sbClient);
                }

            }
        }

        protected internal void StopQueues()
        {
            sbClients.Each(kvp =>
            {
                kvp.Value.Close();
            });
        }

    }
}
