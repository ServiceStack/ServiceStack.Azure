using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMessageQueueClientFactory : IMessageQueueClientFactory
    {
        // Since Service Bus clients need to declare the namespace and queue name at creation,
        // the factory is where we consolidate all queues.

        public IMessageQueueClient CreateMessageQueueClient()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
