﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Smpp.Events;

namespace Smpp.Tests.Integration
{
    [TestFixture]
    public class BindTransmitterTest
    {
        List<string> logBuffer = new List<string>();

        [Test]
        public void TestBindTransmitterConnection()
        {
            var gate = new Gate(new GateEvents());

            var server = gate.AddServerConnection("TestSrver", "sysId", "sysPass");
            server.use_deliver_sm = false;

            Server.StartAsync("127.0.0.1", 2775);

            gate.Events.ChannelEvent += Events_ChannelEvent;

            var client = gate.AddClientConnection("bg", "127.0.0.1", 2775, "sysId", "sysPass");

            // this will make it only send and will connect as bind_transmitter
            client.direction_in = false;
            client.direction_out = true;

            // turn ON debug to show PDU
            // client.debug = true;

            // make timeout longer for debug
            client.timeout = 30000;

            var c = client.Connect();

            Thread.Sleep(1000);

            Assert.IsTrue(c, "Connection should be established");

            Assert.IsTrue(logBuffer.Contains("Sending [bind_transmitter]"), "Log buffer should contain bind transmitter");
            Assert.IsTrue(logBuffer.Contains("Receiving [bind_transmitter_resp]"), "Log buffer should contain bind transmitter response");

            Thread.Sleep(1000);
            client.Quit();
            Server.Stop();
        }

        void Events_ChannelEvent(string channelName, string description, string pdu)
        {
            Debug.WriteLine(DateTime.Now + ": " + channelName + ": " + description + ". PDU: " + pdu);
            logBuffer.Add(description);
        }
    }
}
