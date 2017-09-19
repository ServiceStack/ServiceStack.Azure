#if NETSTANDARD1_6
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System.Threading.Tasks;
using System;
using System.Text;
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
    }
}
#endif
