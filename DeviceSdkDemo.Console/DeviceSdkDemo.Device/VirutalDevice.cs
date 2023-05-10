using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.ComponentModel.Design;
using System.Net.Mime;
using System.Text;
using Opc.UaFx;
using Opc.UaFx.Client;

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
            var client = new OpcClient("opc.tcp://localhost:4840/");
            client.Connect();

                var data = new
                {
                    // podstawowe wysylanie wiadomosci            
                    ProductionStatus = client.ReadNode("ns=2;s=Device 1/ProductionStatus").Value,
                    WorkorderId = client.ReadNode("ns=2;s=Device 1/WorkorderId").Value,
                    Temperature = client.ReadNode("ns=2;s=Device 1/Temperature").Value,
                    GoodCount = client.ReadNode("ns=2;s=Device 1/GoodCount").Value,
                    BadCount = client.ReadNode("ns=2;s=Device 1/BadCount").Value,
                };
                
                var dataString = JsonConvert.SerializeObject(data);

                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Data: [{dataString}]");

                await deviceClient.SendEventAsync(eventMessage);

               
            
            client.Disconnect();
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
        #region Device Twin

        public async Task UpdateTwinAsync()
        {
            var twin = await deviceClient.GetTwinAsync();

            Console.WriteLine($"\n Initial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object _)
        {
            Console.WriteLine($"\tDesired property change\n\t {JsonConvert.SerializeObject(desiredProperties)}");
            Console.WriteLine("\tSending current time as repreted property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        #endregion
        public async Task InitializeHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(OnC2MessageReceivedAsync, deviceClient);

            await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("SendMessages", SendMessagesHandler, deviceClient);

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);

        }

        
    }
}