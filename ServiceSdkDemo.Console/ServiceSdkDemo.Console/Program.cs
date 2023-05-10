using Microsoft.Azure.Devices;
using ServiceSdkDemo.Console;
using ServiceSdkDemo.Lib;

string serviceConnectionString = "HostName=name-test-ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+27eb4tHmdTqbAMc3sMuvUe9cX/xJj96L6vd9/6YeFI=";

using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);

var manager = new IoTHubManager(serviceClient, registryManager);

int input;
do
{
    FeatureSelector.PrintMenu();
    input = FeatureSelector.ReadInput();
    await FeatureSelector.Execute(input, manager);
} while (input != 0);
