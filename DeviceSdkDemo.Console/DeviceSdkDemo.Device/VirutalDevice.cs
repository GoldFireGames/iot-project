using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.ComponentModel.Design;
using System.Net.Mime;
using System.Text;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Threading.Tasks;

namespace DeviceSdkDemo.Device
{
    public class VirutalDevice
    {
        private readonly DeviceClient deviceClient;
        string OPCstring = File.ReadAllText(@"ConnectionOpcUa.txt");
        string DeviceName = File.ReadAllText(@"DeviceName.txt");

        public VirutalDevice(DeviceClient DeviceClient)
        {
            deviceClient = DeviceClient;
        }


        public async Task TimerSendingMessages()
        {
            var client = new OpcClient(OPCstring);
            client.Connect();

            var ProductionStatus = new OpcReadNode($"ns=2;s={DeviceName}/ProductionStatus");
            int RetValues = client.ReadNode(ProductionStatus).As<int>();
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceName}/DeviceError");
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();

            client.Disconnect();
            if (RetValues == 1)
            {
                await SendMessages(1, 1);
            }
            else
            {
                Console.WriteLine("Device Offline");
            }
        }

        #region Sending Messages
        public async Task SendMessages(int nrOfMessages, int delay)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();

            var data = new
            {
                // podstawowe wysylanie wiadomosci            
                ProductionStatus = client.ReadNode($"ns=2;s={DeviceName}/ProductionStatus").Value,
                WorkorderId = client.ReadNode($"ns=2;s={DeviceName}/WorkorderId").Value,
                Temperature = client.ReadNode($"ns=2;s={DeviceName}/Temperature").Value,
                GoodCount = client.ReadNode($"ns=2;s={DeviceName}/GoodCount").Value,
                BadCount = client.ReadNode($"ns=2;s={DeviceName}/BadCount").Value,
            };

            var ProductionRate = new OpcReadNode($"ns=2;s={DeviceName}/ProductionRate");
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceName}/DeviceError");
            int ProductionRateNode = client.ReadNode(ProductionRate).As<int>();
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();

            await UpdateTwinData(ProductionRateNode, DeviceErrorNode);


            var dataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";
            Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Data: [{dataString}]");

            await deviceClient.SendEventAsync(eventMessage);

            client.Disconnect();
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

        /// device twin
        private async Task UpdateTwinData(int ProductionRate, int DeviceError)
        {
            string DeviceErrorString = "";
            if (DeviceError - 8 >= 0)
            {
                DeviceError = DeviceError - 8;
                DeviceErrorString = DeviceErrorString + "Unknown Error ,";
            }
            if (DeviceError - 4 >= 0)
            {
                DeviceError = DeviceError - 4;
                DeviceErrorString = DeviceErrorString + "Sensor Failure ,";
            }
            if (DeviceError - 2 >= 0)
            {
                DeviceError = DeviceError - 2;
                DeviceErrorString = DeviceErrorString + "Power Failure ,";
            }
            if (DeviceError - 1 >= 0)
            {
                DeviceError = DeviceError - 1;
                DeviceErrorString = DeviceErrorString + "Emergency Stop ,";
            }

            var twin = await deviceClient.GetTwinAsync();
            var reportedProperties = new TwinCollection();

            string ReportedErrorStatus = twin.Properties.Reported["ErrorStatus"];
            int ReportedProductionRate = twin.Properties.Reported["ProductionRate"];

            if (!ReportedErrorStatus.Equals(DeviceErrorString))
            {
                reportedProperties["ErrorStatus"] = DeviceErrorString;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            if (ReportedProductionRate != ProductionRate)
            {
                reportedProperties["ProductionRate"] = ProductionRate;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
        }

        private void PrintMessages(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
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
            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ClearErrors", ResetErrors, deviceClient);

        }
        /// emergency
        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            await Task.Delay(1000);
            client.CallMethod($"ns=2;s={DeviceName}", $"ns=2;s={DeviceName}/EmergencyStop");

            client.Disconnect();
            Console.WriteLine("STOP!!!!!!");
            return new MethodResponse(0);
        }
        /// reset errors
        private async Task<MethodResponse> ResetErrors(MethodRequest methodRequest, object userContext)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            await Task.Delay(1000);
            client.CallMethod($"ns=2;s={DeviceName}", $"ns=2;s={DeviceName}/ResetErrorStatus");

            client.Disconnect();
            Console.WriteLine("Errors Reseted =)");
            return new MethodResponse(0);
        }
    }
}
