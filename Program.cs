using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierBackend.server;
using System;
using System.Collections.Generic;
using MapsetSnapshotter;
using System.IO;

namespace MapsetVerifierBackend
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Checker.RelativeDLLDirectory  = Path.Combine(appdataPath, "Mapset Verifier Externals", "checks");
            Snapshotter.RelativeDirectory = Path.Combine(appdataPath, "Mapset Verifier Externals");

            // Loads both external check plugins as well as the default auto-updated one.
            Checker.LoadCheckDLLs();
            Checker.LoadCheckDLL(Path.Combine("resources", "app", "checks", "MapsetChecks.dll"));

            Host.Initialize();
        }
    }
}
