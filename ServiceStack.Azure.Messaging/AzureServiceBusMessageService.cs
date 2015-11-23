using ServiceStack.Logging;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class AzureServiceBusMessageService : IMessageService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AzureServiceBusMessageService));


        public IMessageFactory MessageFactory
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<Type> RegisteredTypes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IMessageHandlerStats GetStats()
        {
            throw new NotImplementedException();
        }

        public string GetStatsDescription()
        {
            throw new NotImplementedException();
        }

        public string GetStatus()
        {
            throw new NotImplementedException();
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            throw new NotImplementedException();
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
