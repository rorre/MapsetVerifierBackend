using MapsetParser.objects;
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
        private BeatmapSet loadedBeatmapSet = null;
        private string loadedBeatmapSetPath = null;

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
                            List<Issue> issues = Checker.GetBeatmapSetIssues(loadedBeatmapSet);
                            string html = ChecksRenderer.Render(issues, loadedBeatmapSet);
                            await SendMessage("UpdateChecks", html);

                            // Reset the lazy loading so in case the map changes and is clicked on
                            // again we can provide proper snapshots/checks for that.
                            // Relies on that snapshots are completed before checks, which is currently always the case.
                            loadedBeatmapSetPath = "";
                        }
                        break;
                    case "RequestSnapshots":
                        {
                            LoadBeatmapSet(aValue);
                            Snapshotter.SnapshotBeatmapSet(loadedBeatmapSet);
                            string html = SnapshotsRenderer.Render(loadedBeatmapSet);
                            await SendMessage("UpdateSnapshots", html);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                ;
                //await SendMessage("UpdateErrors", html); TODO
            }
        }

        private void LoadBeatmapSet(string aSongFolderPath)
        {
            if (loadedBeatmapSetPath != aSongFolderPath)
            {
                loadedBeatmapSet = new BeatmapSet(aSongFolderPath);
                loadedBeatmapSetPath = aSongFolderPath;
            }
        }
    }
}
