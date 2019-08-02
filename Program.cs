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
            Checker.RelativeDLLDirectory  = Path.Combine("..", "Mapset Verifier Externals", "checks");
            Snapshotter.RelativeDirectory = Path.Combine("..", "Mapset Verifier Externals");

            // Loads both external check plugins as well as the default auto-updated one.
            Checker.LoadCheckDLLs();
            Checker.LoadCheckDLL(Path.Combine("resources", "app", "checks", "MapsetChecks.dll"));

            Host.Initialize();
        }
    }
}
