﻿using Azure.Messaging.ServiceBus;

const string sbConnectionString = "Endpoint=sb://qxjejfwmvtqvv.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=F3R1HBgh6Ek/SKZJvNRexEtEeI4Lc8A+G+ASbDjCCcg=";
const string queueName = "myqueues";

await using ServiceBusClient client = new ServiceBusClient(sbConnectionString);
await using ServiceBusSender sender = client.CreateSender(queueName);

for(int i = 0; i < 10; i++)
{
    var messageText = $"This is message nr {i} created on {DateTime.UtcNow}";
    var message = new ServiceBusMessage(messageText);
    Console.WriteLine($"Sending message: \n\t{messageText}");
    await sender.SendMessageAsync(message);
    await Task.Delay(200);
}

Console.WriteLine("\nFINISHED");
Console.ReadLine();