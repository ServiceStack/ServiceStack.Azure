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

        //public static string ToInQueueName(IMessage message)
        //{
        //    string queueName = message.ToInQueueName();
        //    return queueName.Substring(QueueNames.MqPrefix.Length);
        //}
    }

    public class ServiceBusMqClient : ServiceBusMqMessageProducer, IMessageQueueClient
    {

        protected internal ServiceBusMqClient(ServiceBusMqMessageFactory parentFactory)
            : base(parentFactory)
        {
        }


        public void Ack(IMessage message)
        {
            throw new NotImplementedException();
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            BrokeredMessage msg = mqResponse as BrokeredMessage;
            IMessage<T> message = msg.GetBody<IMessage<T>>();
            //message.Id = Guid.Parse(msg.MessageId);
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Notify(string queueName, IMessage message)
        {
            throw new NotImplementedException();
        }

        //public void Publish(string queueName, IMessage message)
        //{
        //    throw new NotImplementedException();
        //}

    }
}
