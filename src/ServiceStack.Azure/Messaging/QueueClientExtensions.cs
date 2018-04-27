using System.Threading.Tasks;
using System;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Azure.Messaging
{

    public static class QueueClientExtensions
    {
#if NETSTANDARD2_0

        static readonly System.Reflection.PropertyInfo innerReceiverProperty = typeof(Microsoft.Azure.ServiceBus.QueueClient).GetProperty("InnerReceiver");

        public static async Task<Microsoft.Azure.ServiceBus.Message> ReceiveAsync(this Microsoft.Azure.ServiceBus.QueueClient sbClient, TimeSpan? timeout)
        {
            var receiver = (Microsoft.Azure.ServiceBus.Core.MessageReceiver)innerReceiverProperty.GetValue(sbClient);

            var msg = timeout.HasValue
                ? await receiver.ReceiveAsync(timeout.Value)
                : await receiver.ReceiveAsync();

            return msg;
        }

        public static string GetBodyString(this Microsoft.Azure.ServiceBus.Message message)
        {
            var strMessage = Encoding.UTF8.GetString(message.Body);
            
            //Windows Azure Client is not wire-compatible with .NET Core client
            //we check if the message comes from Windows client and cut off 
            //64 header chars and 2 footer chars
            //see https://github.com/Azure/azure-service-bus-dotnet/issues/239  
            if (strMessage.StartsWith("@\u0006string", StringComparison.Ordinal))
            {
                strMessage = strMessage.Substring(64, strMessage.Length - 66);
            }

            return strMessage;
        }
#endif

        internal static string SafeQueueName(this string queueName) =>
            queueName?.Replace(":", ".");
    }
    
}
