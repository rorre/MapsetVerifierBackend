using MapsetParser.objects;
using MapsetParser.statics;
using MapsetSnapshotter;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierBackend.renderer;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace MapsetVerifierBackend.server
{
    public class SignalHub : Hub
    {
        public Task ClientMessage(string aKey, string aValue)
        {
            // SignalR hubs buffer handling of requests, meaning they can't be done in parallel.
            // By creating a new thread and forwarding the message in that thread to a background service,
            // multiple requests can be handled at the same time and cancel each other.
            new Thread(new ThreadStart(async () =>
            {
                await Worker.ClientMessage(aKey, aValue);
            })).Start();

            return Task.CompletedTask;
        }
    }
}
