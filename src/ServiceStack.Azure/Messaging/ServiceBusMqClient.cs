using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
#else
using Microsoft.ServiceBus.Messaging;
#endif

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

#if NETSTANDARD1_6
            var msg = mqResponse as Microsoft.Azure.ServiceBus.Message;
            if (msg == null) return null;
            var msgBody = Encoding.UTF8.GetString(msg.Body);
#else
            var msg = mqResponse as BrokeredMessage;
            if (msg == null) return null;
            var msgBody = msg.GetBody<string>();
#endif

            IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(msgBody, typeof(IMessage));
            return (IMessage<T>)iMessage;
        }

        public override void Dispose()
        {
            // All dispose done in base class
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = default(TimeSpan?))
        {
            var sbClient = GetOrCreateClient(queueName);
#if NETSTANDARD1_6
            var msg = sbClient.ReceiveAsync(timeout).Result;
            if (msg != null)
                sbClient.CompleteAsync(msg.SystemProperties.LockToken).Wait();
#else
            var msg = timeout.HasValue
                ? sbClient.Receive(timeout.Value)
                : sbClient.Receive();
#endif


            return CreateMessage<T>(msg);
        }

#if NETSTANDARD1_6
        private async Task<Microsoft.Azure.ServiceBus.Message> GetMessageFromReceiver(MessageReceiver messageReceiver, TimeSpan? timeout)
        {
            var msg = timeout.HasValue
                ? await messageReceiver.ReceiveAsync(timeout.Value)
                : await messageReceiver.ReceiveAsync();

            await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
            return msg;
        }

        private async Task<Microsoft.Azure.ServiceBus.Message> GetMessageFromClient(QueueClient sbClient, TimeSpan? timeout)
        {
            var tcs = new TaskCompletionSource<Microsoft.Azure.ServiceBus.Message>();
            var task = tcs.Task;

            sbClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    tcs.SetResult(message);
                    await sbClient.CompleteAsync(message.SystemProperties.LockToken);
                },
                (eventArgs) =>
                {
                    return Task.CompletedTask;
                }
            );

            if (timeout.HasValue)
            {
                await Task.WhenAny(task, Task.Delay((int)timeout.Value.TotalMilliseconds));

                if (!task.IsCompleted)
                    throw new TimeoutException("Reached timeout while getting message from client");
            } else 
            {
                await task;
            }

            return task.Result;
        }
#endif

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
