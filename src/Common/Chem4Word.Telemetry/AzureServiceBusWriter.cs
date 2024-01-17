// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Azure.Messaging.ServiceBus;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chem4Word.Telemetry
{
    public class AzureServiceBusWriter
    {
        // The Service Bus client types are safe to cache and use as a singleton for the lifetime
        //  of the application, which is best practice when messages are being published or read regularly.

        // The client that owns the connection and can be used to create senders and receivers
        private readonly ServiceBusClient _client;

        // The sender used to publish messages to the queue
        private readonly ServiceBusSender _sender;

        private AzureSettings _settings;

        private static readonly object QueueLock = Guid.NewGuid();

        private readonly Queue<OutputMessage> _buffer1 = new Queue<OutputMessage>();
        private bool _running = false;

        public AzureServiceBusWriter(AzureSettings settings)
        {
            _settings = settings;

            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            if (!string.IsNullOrEmpty(_settings.ServiceBusQueue))
            {
                try
                {
                    // Set the transport type to AmqpWebSockets so that the ServiceBusClient uses the port 443.
                    // If you use the default AmqpTcp, you will need to make sure that the ports 5671 and 5672 are open.
                    var clientOptions = new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets };

                    _client = new ServiceBusClient($"{_settings.ServiceBusEndPoint};{_settings.ServiceBusToken}", clientOptions);
                    _sender = _client.CreateSender(_settings.ServiceBusQueue);
                }
                catch
                {
                    Debugger.Break();
                    // Do nothing
                }
            }
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
            var securityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = securityProtocol | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            try
            {
                if (_sender != null)
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
            }
            catch (Exception exception)
            {
                Debugger.Break();
                Debug.WriteLine($"Exception in WriteMessage: {exception.Message}");

                try
                {
                    var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                $@"Chem4Word.V3\Telemetry\{SafeDate.ToIsoShortDate(DateTime.UtcNow)}.log");
                    using (var streamWriter = File.AppendText(fileName))
                    {
                        await streamWriter.WriteLineAsync($"[{SafeDate.ToShortTime(DateTime.UtcNow)}] Exception in WriteMessage: {exception.Message}");
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }
    }
}