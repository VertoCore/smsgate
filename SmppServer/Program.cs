// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Smpp.Events;
using Smpp;
using System.Diagnostics;

var servers = new Dictionary<string, (string systemId, string password)>()
{
    { "TestSrver", ("sysId", "sysPass") },
    { "AnotherTestSrver", ("sysId2", "sysPass2") }
};

List<string> messages = new List<string>();
List<string> deliveredMessagesIdList = new List<string>();

int totalMessages = 0;

IConfiguration Configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var gate = new Gate(new GateEvents());

var AddServer = (string name, string systemId, string password) =>
{
    if (!Gate.Servers.TryGetValue(name, out var server))
    {
        server = gate.AddServerConnection(name, systemId, password);
        server.use_deliver_sm = false;
        server.registered_delivery = 1;
    }

    return server;
};

string? q = "";
do
{
    try
    {

        foreach (var server in servers)
            AddServer(server.Key, server.Value.systemId, server.Value.password);

        Server.StartAsync("127.0.0.1", 2775);

        gate.Events.Event += Events_Event;
        gate.Events.ChannelEvent += Events_ChannelEvent;
        gate.Events.NewMessageEvent += Events_NewMessageEvent;
        gate.Events.MessageDeliveryReportEvent += Events_MessageDeliveryReportEvent;

        Console.WriteLine("Write q and Press <ENTER> to exit!");
        q = Console.ReadLine();
    }
    catch (Exception) { }
    finally { Server.Stop(); }

} while (q != "q");

void Events_MessageDeliveryReportEvent(string responseMessageId, Common.MessageStatus status)
{
    if (status == Common.MessageStatus.delivered_ACK_received)
    {
        deliveredMessagesIdList.Add(responseMessageId);
        Console.WriteLine($"deliveredMessages {responseMessageId}: {status}");
    }
}

void Events_NewMessageEvent(string channelName, string messageId, string sender, string recipient, string body, string bodyFormat, int registeredDelivery)
{
    Console.WriteLine(DateTime.Now + ": New Message Received on " + channelName + ". From " + sender + " to " + recipient);
    messages.Add(body);
    totalMessages++;
}

void Events_ChannelEvent(string channelName, string description, string pdu)
{
    Console.WriteLine(DateTime.Now + ": " + channelName + ": " + description + ". PDU: " + pdu);

    if ((description == "Disconnected" || description.StartsWith("Disconnecting")) && servers.TryGetValue(channelName, out var server))
    {
        Console.WriteLine(DateTime.Now + ": " + channelName + ": re-add");

        AddServer(channelName, server.systemId, server.password);
    }
}

void Events_Event(LogEvent.Level level, string description)
{
    Console.WriteLine(DateTime.Now + ": " + level + ": " + description);
}