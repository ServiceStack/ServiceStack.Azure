using ServiceStack.Azure.Storage;
using ServiceStack.Script;

namespace ServiceStack.Azure
{
    public class AzureScriptPlugin : IScriptPlugin
    {
        public void Register(ScriptContext context)
        {
            context.ScriptMethods.Add(new AzureScripts());
        }
    }
    
    public class AzureScripts : ScriptMethods
    {
        public AzureBlobVirtualFiles azureBlobVirtualFiles(string connectionString, string containerName) => 
            new AzureBlobVirtualFiles(connectionString, containerName);

        public AzureAppendBlobVirtualFiles azureAppendBlobVirtualFiles(string connectionString, string containerName) => 
            new AzureAppendBlobVirtualFiles(connectionString, containerName);
    }
}
