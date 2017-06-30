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
