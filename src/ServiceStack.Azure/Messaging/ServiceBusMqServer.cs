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
        private int retryCount = 1;
        public int RetryCount
        {
            get => retryCount;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(retryCount));
                retryCount = value;
            }
        }

        public ServiceBusMqServer(string connectionString)
        {
            MessageFactory = new ServiceBusMqMessageFactory(connectionString);
        }

        public IMessageFactory MessageFactory { get; }

        public Func<string, IOneWayClient> ReplyClientFactory { get; set; }

        /// <summary>
        /// Execute global transformation or custom logic before a request is processed.
        /// Must be thread-safe.
        /// </summary>
        public Func<IMessage, IMessage> RequestFilter { get; set; }

        /// <summary>
        /// Execute global transformation or custom logic on the response.
        /// Must be thread-safe.
        /// </summary>
        public Func<object, object> ResponseFilter { get; set; }

        private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap = new Dictionary<Type, IMessageHandlerFactory>();

        protected internal Dictionary<Type, IMessageHandlerFactory> HandlerMap => handlerMap;

        //private readonly Dictionary<Type, int> handlerThreadCountMap
        //    = new Dictionary<Type, int>();

        public List<Type> RegisteredTypes => handlerMap.Keys.ToList();

        /// <summary>
        /// Opt-in to only publish responses on this white list. 
        /// Publishes all responses by default.
        /// </summary>
        public string[] PublishResponsesWhitelist { get; set; }

        public bool DisablePublishingResponses
        {
            set => PublishResponsesWhitelist = value ? TypeConstants.EmptyStringArray : null;
        }

        public void Dispose()
        {
            (MessageFactory as ServiceBusMqMessageFactory)?.StopQueues();
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
            RegisterHandler(processMessageFn, null, noOfThreads: 1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, int noOfThreads)
        {
            RegisterHandler(processMessageFn, null, noOfThreads);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: 1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx, int noOfThreads)
        {
            if (handlerMap.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
            }

            handlerMap[typeof(T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
            //handlerThreadCountMap[typeof(T)] = noOfThreads;

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, handlerMap.Count);
        }

        protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx)
            {
                RequestFilter = RequestFilter,
                ResponseFilter = ResponseFilter,
                RetryCount = RetryCount,
                PublishResponsesWhitelist = PublishResponsesWhitelist
            };
        }

        public void Start()
        {
            // Create the queues (if they don't exist) and start the listeners
            ((ServiceBusMqMessageFactory)MessageFactory).StartQueues(this.handlerMap);
        }

        public void Stop()
        {
            ((ServiceBusMqMessageFactory)MessageFactory).StopQueues();
        }
    }
}
