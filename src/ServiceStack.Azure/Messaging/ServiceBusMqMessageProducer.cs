using System;
using System.Collections.Generic;
using ServiceStack.Messaging;
using ServiceStack.Text;
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

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqMessageProducer : IMessageProducer
    {
        private readonly Dictionary<string, QueueClient> sbClients = new Dictionary<string, QueueClient>();
        private readonly ServiceBusMqMessageFactory parentFactory;

        protected internal ServiceBusMqMessageProducer(ServiceBusMqMessageFactory parentFactory)
        {
            this.parentFactory = parentFactory;
        }

        public virtual void Dispose()
        {
            StopClients();
        }

        public void StopClients()
        {
            foreach (string queue in sbClients.Keys)
            {
#if NETSTANDARD1_6
                sbClients[queue].CloseAsync();
#else
                sbClients[queue].Close();
#endif
            }
        }

        public void Publish<T>(T messageBody)
        {
            // Ensure we're publishing an IMessage
            var message = messageBody as IMessage;
            if (message != null)
            {
                Publish(message.ToInQueueName(), message);
            }
            else
            {
                Publish(new Message<T>(messageBody));
            }
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
        }


        public virtual void Publish(string queueName, IMessage message)
        {
            var sbClient = GetOrCreateClient(queueName);
            using (JsConfig.With(includeTypeInfo: true))
            {
                var msgBody = JsonSerializer.SerializeToString(message, typeof(IMessage));
#if NETSTANDARD1_6
                var msg = new Microsoft.Azure.ServiceBus.Message()
                {
                    Body = msgBody.ToUtf8Bytes(),
                    MessageId = message.Id.ToString()
                };
                sbClient.SendAsync(msg);
#else
                BrokeredMessage msg = new BrokeredMessage(msgBody);
                msg.MessageId = message.Id.ToString();

                sbClient.Send(msg);
#endif
            }
        }

        protected QueueClient GetOrCreateClient(string queueName)
        {
            if (queueName.StartsWith(QueueNames.MqPrefix))
                queueName = queueName.ReplaceFirst(QueueNames.MqPrefix, "");

            if (sbClients.ContainsKey(queueName))
                return sbClients[queueName];

#if !NETSTANDARD1_6
            // Create queue on ServiceBus namespace if it doesn't exist
            QueueDescription qd = new QueueDescription(queueName);
            if (!parentFactory.namespaceManager.QueueExists(queueName))
                parentFactory.namespaceManager.CreateQueue(qd);
#endif

#if NETSTANDARD1_6
            //should be qd.Path
            var sbClient = new QueueClient(parentFactory.address, null);
#else
            var sbClient = QueueClient.CreateFromConnectionString(parentFactory.address, qd.Path);
#endif

            sbClients.Add(queueName, sbClient);
            return sbClient;
        }
    }

}
