using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Function.Functions
{
    public class ServiceBusQueueTigger
    {
        private static readonly string _connectionString = "IOT_HUB_SHARED_ACCESS_POLICY_SERVICE_CONNECTION";

        [FunctionName("ServiceBusQueueTigger")]
        public async Task Run([ServiceBusTrigger("DeviceSimulator", Connection = "ServiceBusConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            var data = (JObject)JsonConvert.DeserializeObject(myQueueItem);

            // Invoke direct method (OpenDevice) to the device with a payload
            var serviceClient = ServiceClient.CreateFromConnectionString(_connectionString);
            var cloudToDeviceMethod = new CloudToDeviceMethod("OpenDevice");
            cloudToDeviceMethod.SetPayloadJson("{name:\"test\"}");

            var response = await serviceClient.InvokeDeviceMethodAsync(data["deviceId"].Value<string>(), cloudToDeviceMethod);
            response.GetPayloadAsJson();
        }
    }
}
