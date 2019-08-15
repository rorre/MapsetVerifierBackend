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
            await SendMessage("AddLoad", "Overview:" + Renderer.Encode(aLoadMessage));
        }

        private static async Task LoadComplete(string aLoadMessage)
        {
            await SendMessage("RemoveLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("RemoveLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
            await SendMessage("RemoveLoad", "Overview:" + Renderer.Encode(aLoadMessage));
        }

        public static async Task SendMessage(string aKey, string aValue)
        {
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
                    try
                    {
                        LoadBeatmapSet(aValue);

                        Func<string, Task>[] actions = new Func<string, Task>[]
                        {
                        RequestSnapshots,
                        RequestChecks,
                        RequestOverview
                        };

                        if (State.LoadedBeatmapSetPath != aValue)
                            return;

                        Parallel.ForEach(actions, anAction =>
                        {
                            anAction(aValue);
                        });
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Checks:" + html);
                        await SendMessage("UpdateException", "Snapshots:" + html);
                        await SendMessage("UpdateException", "Overview:" + html);
                    }

                    break;
                default:
                    break;
            }
        }

        private static void LoadBeatmapSet(string aSongFolderPath)
        {
            State.LoadedBeatmapSetPath = aSongFolderPath;

            Checker.OnLoadStart = LoadStart;
            Checker.OnLoadComplete = LoadComplete;

            EventStatic.OnLoadStart = LoadStart;
            EventStatic.OnLoadComplete = LoadComplete;
                
            State.LoadedBeatmapSet = new BeatmapSet(aSongFolderPath);
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

        private static async Task RequestOverview(string aBeatmapSetPath)
        {
            try
            {
                if (State.LoadedBeatmapSetPath != aBeatmapSetPath)
                    return;

                string html = OverviewRenderer.Render(State.LoadedBeatmapSet);
                await SendMessage("UpdateOverview", html);
            }
            catch (Exception exception)
            {
                string html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Overview:" + html);
            }
        }

        protected override Task ExecuteAsync(CancellationToken aStoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
