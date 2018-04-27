using System;
using System.Collections.Generic;
using ServiceStack.Messaging;
using ServiceStack.Text;
#if NETSTANDARD2_0
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqMessageProducer : IMessageProducer
    {
        private readonly Dictionary<string, MessageReceiver> sbReceivers = new Dictionary<string, MessageReceiver>();
        protected readonly ServiceBusMqMessageFactory parentFactory;

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
            //parentFactory.StopQueues();
        }

        public void Publish<T>(T messageBody)
        {
            // Ensure we're publishing an IMessage
            if (messageBody is IMessage message)
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
            queueName = queueName.SafeQueueName();
            message.ReplyTo = message.ReplyTo.SafeQueueName();

            var sbClient = parentFactory.GetOrCreateClient(queueName);
            using (JsConfig.With(includeTypeInfo: true))
            {
                var msgBody = JsonSerializer.SerializeToString(message, typeof(IMessage));
#if NETSTANDARD2_0
                var msg = new Microsoft.Azure.ServiceBus.Message()
                {
                    Body = msgBody.ToUtf8Bytes(),
                    MessageId = message.Id.ToString()
                };
                sbClient.SendAsync(msg).Wait();
#else
                var msg = new BrokeredMessage(msgBody) {MessageId = message.Id.ToString()};

                sbClient.Send(msg);
#endif
            }
        }

#if NETSTANDARD2_0
        protected MessageReceiver GetOrCreateMessageReceiver(string queueName)
        {
            queueName = queueName.SafeQueueName();

            if (sbReceivers.ContainsKey(queueName))
                return sbReceivers[queueName];

            var messageReceiver = new MessageReceiver(
             parentFactory.address,
             queueName,
             ReceiveMode.ReceiveAndDelete);  //should be ReceiveMode.PeekLock, but it does not delete messages from queue on CompleteAsync()
             
            sbReceivers.Add(queueName, messageReceiver);
            return messageReceiver;
        }
#endif
    }

}
