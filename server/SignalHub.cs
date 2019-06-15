using MapsetParser.objects;
using MapsetParser.statics;
using MapsetSnapshotter;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifierApp.renderer;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MapsetVerifierApp.server
{
    public class SignalHub : Hub
    {
        public const string relativeCheckPath = "./resources/app/checks";

        /// <summary> Returns whether the message was successfully sent or failed. </summary>
        public async Task<bool> SendMessage(string aKey, string aValue)
        {
            Console.WriteLine("Sending message with key \"" + aKey + "\".");
            try
            {
                await Clients.All.SendAsync("ServerMessage", aKey, aValue);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("error; " + exception.Message);
                return false;
            }
        }

        private async Task LoadStart(string aLoadMessage)
        {
            await SendMessage("AddLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("AddLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        private async Task LoadComplete(string aLoadMessage)
        {
            await SendMessage("RemoveLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("RemoveLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        public async Task ClientMessage(string aKey, string aValue)
        {
            Console.WriteLine("Received message with key \"" + aKey + "\", and value \"" + aValue + "\".");

            switch (aKey)
            {
                case "RequestDocumentation":
                    try
                    {
                        Checker.RelativeDLLDirectory = relativeCheckPath;
                        Checker.LoadCheckDLLs();
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
                        Checker.RelativeDLLDirectory = relativeCheckPath;
                        Checker.LoadCheckDLLs();
                        string html = OverlayRenderer.Render(aValue);
                        await SendMessage("UpdateOverlay", html);
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Overlay:" + html);
                    }
                    break;
                case "RequestChecks":
                    try
                    {
                        LoadBeatmapSet(aValue);

                        await SendMessage("ClearLoad", "");
                        Checker.OnLoadStart = LoadStart;
                        Checker.OnLoadComplete = LoadComplete;

                        Checker.RelativeDLLDirectory = relativeCheckPath;

                        List<Issue> issues = Checker.GetBeatmapSetIssues(State.LoadedBeatmapSet);
                        string html = ChecksRenderer.Render(issues, State.LoadedBeatmapSet);
                        await SendMessage("UpdateChecks", html);

                        // Reset the lazy loading so in case the map changes and is clicked on
                        // again we can provide proper snapshots/checks for that.
                        // Relies on that snapshots are completed before checks, which is currently always the case.
                        State.LoadedBeatmapSetPath = "";
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Checks:" + html);
                    }
                    break;
                case "RequestSnapshots":
                    try
                    {
                        LoadBeatmapSet(aValue);
                        Snapshotter.RelativeDirectory = Path.Combine("resources", "app");
                        Snapshotter.SnapshotBeatmapSet(State.LoadedBeatmapSet);
                        string html = SnapshotsRenderer.Render(State.LoadedBeatmapSet);
                        await SendMessage("UpdateSnapshots", html);
                    }
                    catch (Exception exception)
                    {
                        string html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Snapshots:" + html);
                    }
                    break;
                default:
                    break;
            }
        }

        private void LoadBeatmapSet(string aSongFolderPath)
        {
            if (State.LoadedBeatmapSetPath != aSongFolderPath)
            {
                EventStatic.OnLoadStart = LoadStart;
                EventStatic.OnLoadComplete = LoadComplete;

                State.LoadedBeatmapSet = new BeatmapSet(aSongFolderPath);
                State.LoadedBeatmapSetPath = aSongFolderPath;
            }
        }
    }
}
