using Microsoft.Azure.Devices.Client;

namespace DeviceSdkDemo.Device
{
    public class VirutalDevice
    {
        private readonly DeviceClient deviceClient;

        public VirutalDevice(DeviceClient DeviceClient)
        {
            deviceClient = DeviceClient;
        }
    }
}