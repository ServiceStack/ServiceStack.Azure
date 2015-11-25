using Microsoft.ServiceBus.Messaging;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqClient : IMessageQueueClient
    {
        private readonly string connectionString;
        private readonly string queueName;

        private QueueClient client;


        public void Ack(IMessage message)
        {
            throw new NotImplementedException();
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = default(TimeSpan?))
        {
            throw new NotImplementedException();
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

        public void Publish(string queueName, IMessage message)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(IMessage<T> message)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T messageBody)
        {
            throw new NotImplementedException();
        }
    }
}
