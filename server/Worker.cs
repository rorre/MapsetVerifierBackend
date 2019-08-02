using MapsetParser.objects;
using MapsetParser.statics;
using MapsetSnapshotter;
using MapsetVerifierBackend.renderer;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapsetVerifierBackend.server
{
    public class Worker : BackgroundService
    {
        private static IHubContext<SignalHub> hub;

        public Worker(IHubContext<SignalHub> aHub)
        {
            // Looks messy but since we only create one connection this is fine.
            // Other ways of obtaining a hub context are pretty complicated.
            hub = aHub;
        }

        private static async Task LoadStart(string aLoadMessage)
        {
            await SendMessage("AddLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("AddLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        private static async Task LoadComplete(string aLoadMessage)
        {
            await SendMessage("RemoveLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("RemoveLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        public static async Task SendMessage(string aKey, string aValue)
        {
            Console.WriteLine("Sending message with key \"" + aKey + "\".");
            try
            {
                await hub.Clients.All.SendAsync("ServerMessage", aKey, aValue);
            }
            catch (Exception exception)
            {
                Console.WriteLine("error; " + exception.Message);
            }
        }

        public static async Task ClientMessage(string aKey, string aValue)
        {
            Console.WriteLine("Received message with key \"" + aKey + "\", and value \"" + aValue + "\".");

            switch (aKey)
            {
                case "RequestDocumentation":
                    try
                    {
                        string html = DocumentationRenderer.Render();
                        await SendMessage("UpdateDocumentation", html);
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Documentation:" + html);
                    }
                    break;
                case "RequestOverlay":
                    try
                    {
                        string html = OverlayRenderer.Render(aValue);
                        await SendMessage("UpdateOverlay", html);
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Overlay:" + html);
                    }
                    break;
                case "RequestBeatmapset":
                    await LoadBeatmapSet(aValue);

                    Func<string, Task>[] actions = new Func<string, Task>[]
                    {
                        RequestSnapshots,
                        RequestChecks
                    };

                    if (State.LoadedBeatmapSetPath != aValue)
                        return;

                    Parallel.ForEach(actions, anAction =>
                    {
                        anAction(aValue);
                    });

                    // Reset the lazy loading so in case the map changes and is clicked on
                    // again we can provide proper snapshots/checks for that.
                    // Relies on that snapshots are completed before checks, which is currently always the case.
                    State.LoadedBeatmapSetPath = "";

                    break;
                default:
                    break;
            }
        }

        private static async Task<bool> LoadBeatmapSet(string aSongFolderPath)
        {
            if (State.LoadedBeatmapSetPath != aSongFolderPath)
            {
                State.LoadedBeatmapSetPath = aSongFolderPath;

                Checker.OnLoadStart = LoadStart;
                Checker.OnLoadComplete = LoadComplete;

                EventStatic.OnLoadStart = LoadStart;
                EventStatic.OnLoadComplete = LoadComplete;
                
                State.LoadedBeatmapSet = new BeatmapSet(aSongFolderPath);

                return true;
            }

            return false;
        }

        private static async Task RequestSnapshots(string aBeatmapSetPath)
        {
            try
            {
                Snapshotter.SnapshotBeatmapSet(State.LoadedBeatmapSet);
                if (State.LoadedBeatmapSetPath != aBeatmapSetPath)
                    return;

                string html = SnapshotsRenderer.Render(State.LoadedBeatmapSet);
                await SendMessage("UpdateSnapshots", html);
            }
            catch (Exception exception)
            {
                string html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Snapshots:" + html);
            }
        }

        private static async Task RequestChecks(string aBeatmapSetPath)
        {
            try
            {
                List<Issue> issues = Checker.GetBeatmapSetIssues(State.LoadedBeatmapSet);
                if (State.LoadedBeatmapSetPath != aBeatmapSetPath)
                    return;

                string html = ChecksRenderer.Render(issues, State.LoadedBeatmapSet);
                await SendMessage("UpdateChecks", html);
            }
            catch (Exception exception)
            {
                string html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Checks:" + html);
            }
        }

        protected override Task ExecuteAsync(CancellationToken aStoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
