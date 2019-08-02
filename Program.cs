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

            Checker.LoadCheckDLLs();

            Host.Initialize();
        }
    }
}
