using MapsetParser.objects;
using MapsetParser.statics;
using MapsetSnapshotter;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifierApp.renderer;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapsetVerifierApp.server
{
    public class SignalHub : Hub
    {
        /// <summary> Returns whether the message was successfully sent or failed. </summary>
        public async Task<bool> SendMessage(string aKey, string aValue)
        {
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
            Console.WriteLine("ADD " + aLoadMessage);
            await SendMessage("AddLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("AddLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        private async Task LoadComplete(string aLoadMessage)
        {
            Console.WriteLine("REMOVE " + aLoadMessage);
            await SendMessage("RemoveLoad", "Checks:" + Renderer.Encode(aLoadMessage));
            await SendMessage("RemoveLoad", "Snapshots:" + Renderer.Encode(aLoadMessage));
        }

        public async Task ClientMessage(string aKey, string aValue)
        {
            Console.WriteLine("Received message with key \"" + aKey + "\", and value \"" + aValue + "\".");
            try
            {
                switch (aKey)
                {
                    case "RequestDocumentation":
                        {
                            Checker.LoadCheckDLLs();
                            string html = DocumentationRenderer.Render();
                            await SendMessage("UpdateDocumentation", html);
                        }
                        break;
                    case "RequestOverlay":
                        {
                            Checker.LoadCheckDLLs();
                            string html = OverlayRenderer.Render(aValue);
                            await SendMessage("UpdateOverlay", html);
                        }
                        break;
                    case "RequestChecks":
                        {
                            LoadBeatmapSet(aValue);

                            Checker.OnLoadStart = LoadStart;
                            Checker.OnLoadComplete = LoadComplete;

                            List<Issue> issues = Checker.GetBeatmapSetIssues(State.LoadedBeatmapSet);
                            string html = ChecksRenderer.Render(issues, State.LoadedBeatmapSet);
                            await SendMessage("UpdateChecks", html);

                            // Reset the lazy loading so in case the map changes and is clicked on
                            // again we can provide proper snapshots/checks for that.
                            // Relies on that snapshots are completed before checks, which is currently always the case.
                            State.LoadedBeatmapSetPath = "";
                        }
                        break;
                    case "RequestSnapshots":
                        {
                            LoadBeatmapSet(aValue);
                            Snapshotter.SnapshotBeatmapSet(State.LoadedBeatmapSet);
                            string html = SnapshotsRenderer.Render(State.LoadedBeatmapSet);
                            await SendMessage("UpdateSnapshots", html);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                string html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", html);
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
