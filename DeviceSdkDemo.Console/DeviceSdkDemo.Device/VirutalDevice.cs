using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;

namespace DeviceSdkDemo.Device
{
    public class VirutalDevice
    {
        private readonly DeviceClient deviceClient;

        public VirutalDevice(DeviceClient DeviceClient)
        {
            deviceClient = DeviceClient;
        }

        #region Sending Messages
        public async Task SendMessages(int nrOfMessages, int delay)
        {
            var rnd = new Random();

            Console.WriteLine($"Device sending {nrOfMessages} messages to IotHub...\n");

            for(int i=0; i < nrOfMessages; i++) 
            {
                var data = new
                {
                    temperature = rnd.Next(20, 35),
                    humidity = rnd.Next(60, 80),
                    msgCount = i,
                };

                var dataString = JsonConvert.SerializeObject(data);

                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                eventMessage.Properties.Add("temperatureAlert", (data.temperature > 30) ? "true" : "false");
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {i}, Data: [{dataString}]");

                await deviceClient.SendEventAsync(eventMessage);

                if(i < nrOfMessages - 1)
                    await Task.Delay(delay);
                
            }
            Console.WriteLine();
        }
        #endregion
    }
}