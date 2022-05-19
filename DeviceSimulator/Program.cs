using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DPS
{
    class Program
    {
        private static string s_idScope = "DPS_ID_SCOPE";
        private const string GlobalDeviceEndpoint = "DPS_GLOBAL_DEVICE_ENDPOINT";
        private const string enrollmentGroupPrimaryKey = "GROUP_ENROLLMENT_PRIMARY_KEY";
        private const string enrollmentGroupSecondaryKey = "GROUP_ENROLLMENT_SECONDARY_KEY";
        private static string s_registrationID = "will-become-device-id-123";

        private static DeviceClient _deviceClient;
        // Default telemetry delay 10 sec.
        private static int _telemetryDelay = 10;

        public static async Task Main(string[] args)
        {
            string primaryKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(enrollmentGroupPrimaryKey), s_registrationID);
            string secondaryKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(enrollmentGroupSecondaryKey), s_registrationID);
                
            using (var security = new SecurityProviderSymmetricKey(s_registrationID, primaryKey, secondaryKey))
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, s_idScope, security, transport);
                DeviceRegistrationResult result = await provClient.RegisterAsync();

                Console.WriteLine($"Registration status: {result.Status}.");
                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    Console.WriteLine($"Registration status did not assign a hub, so exiting this sample.");
                    return;
                }
                Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");





                Console.WriteLine("Creating symmetric key authentication for IoT Hub...");
                IAuthenticationMethod auth = new DeviceAuthenticationWithRegistrySymmetricKey(
                    result.DeviceId,
                    security.GetPrimaryKey());

                Console.WriteLine("Connecting to hub");
                _deviceClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt);
                Console.WriteLine("Connection established");

                // Read device twin properties to do initial setup
                InitialSetupFromDeviceTwin();

                // Registering direct methods
                await _deviceClient.SetMethodHandlerAsync("OpenDevice", OpenDevice, null);

                // Send some telemetry data to iothub
                SendDeviceToCloudMessagesAsync();

                Console.ReadLine();

            }
        }

        public static string ComputeDerivedSymmetricKey(byte[] masterKey, string s_registrationID)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(s_registrationID)));
            }
        }


        private static async void InitialSetupFromDeviceTwin()
        {
            Console.WriteLine("Reading device twin for inital setup");
            // Get Device Twin Properties below here
            var twin = await _deviceClient.GetTwinAsync().ConfigureAwait(false);
            if (twin.Properties.Desired.Contains("telemetryDelay"))
            {
                _telemetryDelay = twin.Properties.Desired["telemetryDelay"];
            }
        }

        private static async Task<MethodResponse> OpenDevice(MethodRequest methodRequest, object userContext)
        {
            try
            {
                var data = JObject.Parse(methodRequest.DataAsJson);
                Console.WriteLine("Opening device for " + data.GetValue("name"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new MethodResponse(200);
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            var limit = 1;
            while (limit-- > 0)
            {
                // Prepare the telemetry data.
                var telemetryDataPoint = new
                {
                    deviceId = s_registrationID
                };

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // The following property will be used in the iot-hub message routing.
                message.Properties.Add("route", "DeviceSimulator");

                await _deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} {1}: {2}", DateTime.Now, s_registrationID, messageString);

                // telemetry delay
                await Task.Delay(_telemetryDelay * 1000);
            }
        }
    }
}