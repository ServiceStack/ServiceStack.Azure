using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Azure.Messaging
{
    public class ServiceBusMqServer : IMessageService
    {
        private readonly string connectionString;

        public ServiceBusMqServer(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public IMessageFactory MessageFactory { get; private set; }

        private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap
            = new Dictionary<Type, IMessageHandlerFactory>();

        private readonly Dictionary<Type, int> handlerThreadCountMap
            = new Dictionary<Type, int>();

        private ServiceBusMqWorker[] workers;
        private Dictionary<string, int[]> queueWorkerIndexMap;


        public List<Type> RegisteredTypes { get { return handlerMap.Keys.ToList(); } }


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
