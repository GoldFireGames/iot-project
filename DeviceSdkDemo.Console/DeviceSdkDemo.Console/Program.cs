using DeviceSdkDemo.Device;
using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Threading;

string deviceConnectionString = File.ReadAllText(@"ConnectionString.txt");

using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
await deviceClient.OpenAsync();
var device = new VirutalDevice(deviceClient);
Console.WriteLine("Connection success");
await device.InitializeHandlers();
await device.UpdateTwinAsync();

var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

while (await periodicTimer.WaitForNextTickAsync())
{
    await device.TimerSendingMessages();
}

Console.WriteLine("Finished! Press key to close...");
Console.ReadLine();