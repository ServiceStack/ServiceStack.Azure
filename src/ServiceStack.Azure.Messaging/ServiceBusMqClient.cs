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

    public class ServiceBusMqClient : ServiceBusMqMessageProducer, IMessageQueueClient, IOneWayClient
    {

        protected internal ServiceBusMqClient(ServiceBusMqMessageFactory parentFactory)
            : base(parentFactory)
        {
        }


        public void Ack(IMessage message)
        {
            // Message is automatically dequeued at Get<>
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            if (mqResponse is IMessage)
                return (IMessage<T>)mqResponse;
            else
            {
                var msg = mqResponse as BrokeredMessage;
                if (msg == null) return null;
                var msgBody = msg.GetBody<string>();

                IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(msgBody, typeof(IMessage));
                return (IMessage<T>)iMessage;
            }
        }

        public override void Dispose()
        {
            // All dispose done in base class
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = default(TimeSpan?))
        {
            var sbClient = GetOrCreateClient(queueName);

            BrokeredMessage msg = null;
            if (timeOut.HasValue)
            {
                msg = sbClient.Receive(timeOut.Value);
            }
            else
            {
                msg = sbClient.Receive();
            }

            return CreateMessage<T>(msg);
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            throw new NotImplementedException();
        }

        public string GetTempQueueName()
        {
            throw new NotImplementedException();
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            // If don't requeue, post message to DLQ
            var queueName = requeue
                 ? message.ToInQueueName()
                 : message.ToDlqQueueName();

            Publish(queueName, message);
        }

        public void Notify(string queueName, IMessage message)
        {
            Publish(queueName, message);
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            if (requests == null) return;
            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        public void SendOneWay(object requestDto)
        {
            Publish(MessageFactory.Create(requestDto));
        }

        public void SendOneWay(string relativeOrAbsoluteUri, object requestDto)
        {
            throw new NotImplementedException();
        }
    }
}
