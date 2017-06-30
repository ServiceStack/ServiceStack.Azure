using System;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using ServiceStack.Messaging;
using ServiceStack.Text;

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
                sbClients[queue].Close();
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
                BrokeredMessage msg = new BrokeredMessage(msgBody);
                msg.MessageId = message.Id.ToString();

                sbClient.Send(msg);
            }
        }

        protected QueueClient GetOrCreateClient(string queueName)
        {
            if (queueName.StartsWith(QueueNames.MqPrefix))
                queueName = queueName.ReplaceFirst(QueueNames.MqPrefix, "");

            if (sbClients.ContainsKey(queueName))
                return sbClients[queueName];

            // Create queue on ServiceBus namespace if it doesn't exist
            QueueDescription qd = new QueueDescription(queueName);
            if (!parentFactory.namespaceManager.QueueExists(queueName))
                parentFactory.namespaceManager.CreateQueue(qd);

            var sbClient = QueueClient.CreateFromConnectionString(parentFactory.address, qd.Path);

            sbClients.Add(queueName, sbClient);
            return sbClient;
        }
    }

}
