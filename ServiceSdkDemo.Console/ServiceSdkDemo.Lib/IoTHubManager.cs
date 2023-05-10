using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Text;

namespace ServiceSdkDemo.Lib
{
    public class IoTHubManager
    {
        private readonly ServiceClient client;
        public IoTHubManager(ServiceClient client)
        {
            this.client = client;
        }

        // C2D
        public async Task SendMessage(string textMessage, string deviceId)
        {
            var messageBody = new { text = textMessage };
            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)));
            message.MessageId = Guid.NewGuid().ToString();
            await client.SendAsync(deviceId,message);
        }
    }
}