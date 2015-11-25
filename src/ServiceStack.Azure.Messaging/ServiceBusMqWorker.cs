using ServiceStack.Logging;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqWorker));

        private readonly string connectionString;
        private readonly string queueName;
        private readonly IMessageHandler messageHandler;

        private IMessageQueueClient mqClient;

        public ServiceBusMqWorker(string connectionString, string queueName, IMessageHandler messageHandler)
        {
            
        }


    }
}
