// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Chem4Word.Core.Helpers;

namespace Chem4Word.Telemetry
{
    // Azure.Messaging.ServiceBus
    //  .ServiceBusMessage
    //  .ServiceBusMessageBatch
    //  .ServiceBusClient
    //  .ServiceBusSender

    public class AzureServiceBusWriter
    {
        // The Service Bus client types are safe to cache and use as a singleton for the lifetime
        //  of the application, which is best practice when messages are being published or read regularly.

        // The client that owns the connection and can be used to create senders and receivers
        private readonly ServiceBusClient _client;

        // The sender used to publish messages to the queue
        private readonly ServiceBusSender _sender;

        // Make sure this is a Send Only Access key
        private const string ServiceBus = "Endpoint=sb://c4w-telemetry.servicebus.windows.net/;SharedAccessKeyName=TelemetrySender;SharedAccessKey=J8tkibrh5CHc2vZJgn1gbynZRmMLUf0mz/WZtmcjH6Q=";
        private const string QueueName = "telemetry";

        private static readonly object QueueLock = Guid.NewGuid();

        private readonly Queue<OutputMessage> _buffer1 = new Queue<OutputMessage>();
        private bool _running = false;

        public AzureServiceBusWriter()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            // Set the transport type to AmqpWebSockets so that the ServiceBusClient uses the port 443.
            // If you use the default AmqpTcp, you will need to make sure that the ports 5671 and 5672 are open.
            var clientOptions = new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets };

            _client = new ServiceBusClient(ServiceBus, clientOptions);
            _sender = _client.CreateSender(QueueName);
        }

        public void QueueMessage(OutputMessage message)
        {
            lock (QueueLock)
            {
                _buffer1.Enqueue(message);
                Monitor.PulseAll(QueueLock);
            }

            if (!_running)
            {
                var t = new Thread(WriteOnThread);
                t.SetApartmentState(ApartmentState.STA);
                _running = true;
                t.Start();
            }
        }

        private void WriteOnThread()
        {
            // Small sleep before we start
            Thread.Sleep(25);

            var buffer2 = new Queue<OutputMessage>();

            while (_running)
            {
                // Move messages from 1st stage buffer to 2nd stage buffer
                lock (QueueLock)
                {
                    while (_buffer1.Count > 0)
                    {
                        buffer2.Enqueue(_buffer1.Dequeue());
                    }
                    Monitor.PulseAll(QueueLock);
                }

                while (buffer2.Count > 0)
                {
                    var task = WriteMessage(buffer2.Dequeue());
                    task.Wait();

                    // Small micro sleep between each message
                    Thread.Sleep(5);
                }

                lock (QueueLock)
                {
                    if (_buffer1.Count == 0)
                    {
                        _running = false;
                    }
                    Monitor.PulseAll(QueueLock);
                }
            }
        }

        private async Task WriteMessage(OutputMessage message)
        {
            try
            {
                using (var messageBatch = await _sender.CreateMessageBatchAsync())
                {
                    var msg = new ServiceBusMessage(message.Message);
                    msg.ApplicationProperties.Add("PartitionKey", message.PartitionKey);
                    msg.ApplicationProperties.Add("RowKey", message.RowKey);
                    msg.ApplicationProperties.Add("Chem4WordVersion", message.AssemblyVersionNumber);
                    msg.ApplicationProperties.Add("MachineId", message.MachineId);
                    msg.ApplicationProperties.Add("Operation", message.Operation);
                    msg.ApplicationProperties.Add("Level", message.Level);
#if DEBUG
                    msg.ApplicationProperties.Add("IsDebug", "True");
#endif

                    if (!messageBatch.TryAddMessage(msg))
                    {
                        Debugger.Break();
                    }

                    await _sender.SendMessagesAsync(messageBatch);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Exception in WriteMessage: {exception.Message}");

                try
                {
                    var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                $@"Chem4Word.V3\Telemetry\{SafeDate.ToIsoShortDate(DateTime.Now)}.log");
                    using (var streamWriter = File.AppendText(fileName))
                    {
                        await streamWriter.WriteLineAsync($"[{SafeDate.ToShortTime(DateTime.Now)}] Exception in WriteMessage: {exception.Message}");
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
        }
    }
}