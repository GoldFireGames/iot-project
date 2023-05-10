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
        #region Receive Message
        private async Task OnC2MessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"\t{DateTime.Now} > C2D message callback - message received with Id={receivedMessage.MessageId}");
            PrintMessages(receivedMessage);
            await deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"\t{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        private void PrintMessages(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach(var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++} > Key={prop.Key} : Value={prop.Value}");
            }
        }
        #endregion
        #region Direct Methods

        private async Task<MethodResponse> SendMessagesHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { nrOfMessages = default(int), delay = default(int) });
            await SendMessages(payload.nrOfMessages, payload.delay);

            return new MethodResponse(0);
        }

        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            await Task.Delay(1000);

            return new MethodResponse(0);
        }

        #endregion

        public async Task InitializeHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(OnC2MessageReceivedAsync, deviceClient);

            await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("SendMessages", SendMessagesHandler, deviceClient);

        }

        
    }
}