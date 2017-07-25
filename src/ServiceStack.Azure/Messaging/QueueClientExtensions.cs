#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System.Threading.Tasks;
using System;
using ServiceStack.Text;
using System.Reflection;

namespace ServiceStack.Azure.Messaging
{
    public static class QueueClientExtensions
    {
        static readonly PropertyInfo innerReceiverProperty = typeof(QueueClient).GetProperty("InnerReceiver");

        public static async Task<Microsoft.Azure.ServiceBus.Message> ReceiveAsync(this QueueClient sbClient, TimeSpan? timeout)
        {
            var receiver = (MessageReceiver)innerReceiverProperty.GetValue(sbClient);

            var msg = timeout.HasValue
                ? await receiver.ReceiveAsync(timeout.Value)
                : await receiver.ReceiveAsync();

            return msg;
        }
    }
}
#endif
