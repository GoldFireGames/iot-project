using DeviceSdkDemo.Device;
using Microsoft.Azure.Devices.Client;

string deviceConnectionString = "HostName=name-test-ul.azure-devices.net;DeviceId=test;SharedAccessKey=xwI4UvkgfIoEZIeaRJFOISUyWNZah4p9MX57qClxJ5g=";

using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
await deviceClient.OpenAsync();
var device = new VirutalDevice(deviceClient);
Console.WriteLine("Connection success");
Console.ReadLine();