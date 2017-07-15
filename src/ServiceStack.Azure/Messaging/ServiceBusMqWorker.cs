using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
#else
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Messaging
{
    class ServiceBusMqWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqWorker));

        private readonly string queueName;
        private readonly IMessageQueueClient mqClient;
        private readonly ServiceBusMqMessageFactory mqMessageFactory;
        private readonly QueueClient sbClient;

        public ServiceBusMqWorker(ServiceBusMqMessageFactory mqMessageFactory, IMessageQueueClient mqClient, string queueName, QueueClient sbClient)
        {
            this.mqMessageFactory = mqMessageFactory;
            this.queueName = queueName;
            this.mqClient = mqClient;
            this.sbClient = sbClient;
        }

#if NETSTANDARD1_6
        public async Task HandleMessageAsync(Microsoft.Azure.ServiceBus.Message msg, CancellationToken token)
        {
            var strMessage = Encoding.UTF8.GetString(msg.Body);
            IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
            Type msgType = iMessage.GetType().GetGenericArguments()[0];
            var messageHandlerFactory = mqMessageFactory.handlerMap[msgType];
            var messageHandler = messageHandlerFactory.CreateMessageHandler();

            messageHandler.ProcessMessage(mqClient, iMessage);

            await sbClient.CompleteAsync(msg.SystemProperties.LockToken);
        }
#else
        public void HandleMessage(BrokeredMessage msg)
        {
            try
            {
                string strMessage = msg.GetBody<string>();
                IMessage iMessage = (IMessage)JsonSerializer.DeserializeFromString(strMessage, typeof(IMessage));
                Type msgType = iMessage.GetType().GetGenericArguments()[0];
                var messageHandlerFactory = mqMessageFactory.handlerMap[msgType];
                var messageHandler = messageHandlerFactory.CreateMessageHandler();

                messageHandler.ProcessMessage(mqClient, iMessage);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                msg.Complete(); // Release message from Azure service-bus; we received it, but internally weren't able to process it.  
                                // Its the handler's fault, not the ServiceBus
            }
        }
#endif
    }
}
