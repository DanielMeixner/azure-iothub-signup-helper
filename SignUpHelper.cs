using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.System.Profile;

namespace Dmx.Helper.Azure.IotHubSignUp
{
    public sealed class SignUpHelper
    {
        public event EventHandler<object> OnIotHubMessageReceived;

        static RegistryManager _registryManager;
                
        static string _connectionString = String.Empty;
        string _deviceId;

        Device _device = null;
        Task _receivingTask;
        DeviceClient _deviceClient;        
        

        public SignUpHelper(string iotHubConnectionString)
        {
            _deviceId = GetUniqueDeviceId();
            _connectionString = iotHubConnectionString;

            RegisterDeviceOnIoTHubAsync();
        }

        public SignUpHelper(string iotHubConnectionString, string deviceId )
        {            
            _deviceId = string.IsNullOrEmpty(deviceId) ? GetUniqueDeviceId() : deviceId;
            _connectionString = iotHubConnectionString;
            RegisterDeviceOnIoTHubAsync();
        }


        private async Task RegisterDeviceOnIoTHubAsync()
        {
            // add to Iot Hub or get Device Info
            _device = await SignUpDeviceOnIotHubAsync(_deviceId);

            //start Listening for Update Triggers
            _deviceClient = await ConnectToIoTSuiteAsync(_device);

        }

        /// <summary>
        ///  Sign up device if it doesn't exist yet. Otherwise just get device.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>

        private static async Task<Device> SignUpDeviceOnIotHubAsync(string deviceId)
        {
            Device device;
            _registryManager = RegistryManager.CreateFromConnectionString(_connectionString);

            try
            {
                device = await _registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException e)
            {
                device = await _registryManager.GetDeviceAsync(deviceId);
            }

            Debug.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
            return device;
        }

   
        private async Task<DeviceClient> ConnectToIoTSuiteAsync(Device dev)
        {
            var deviceConnectionString = $"{ _connectionString};DeviceId={_device.Id}";

            try
            {
                _deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Amqp);
                await _deviceClient.OpenAsync();                

                _receivingTask = Task.Run(() => ReceiveDataFromAzure());
            }
            catch
            {                
                Debug.Write("Error while trying to connect to IoT Hub");
            }
            return _deviceClient;
        }           

        private async void ReceiveDataFromAzure()
        {
            while (true)
            {
                var message = await _deviceClient.ReceiveAsync();
                if (message != null)
                {                    
                    try
                    {
                        OnIotHubMessageReceived?.Invoke(this, message);

                        //OnIotHubMessageReceived?.Invoke(this, null);
                        await _deviceClient.CompleteAsync(message);
                    }
                    catch
                    {
                        await _deviceClient.RejectAsync(message);
                    }
                }
            }
        }
        
        

        private static string GetUniqueDeviceId()
        {
            var hostNames = NetworkInformation.GetHostNames();
            var hostName = hostNames.FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???";

            var hwToken = HardwareIdentification.GetPackageSpecificToken(null);
            var hwTokenId = hwToken.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hwTokenId);
            byte[] bytes = new byte[hwTokenId.Length];
            dataReader.ReadBytes(bytes);
            var id = BitConverter.ToString(bytes).Replace("-", "").Substring(25);

            var deviceId = $"{hostName}_{id}";
            return deviceId;
        }
    }
}
