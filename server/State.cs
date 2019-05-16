using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetVerifierApp.server
{
    public static class State
    {
        public static BeatmapSet LoadedBeatmapSet { get; set; }
        public static string LoadedBeatmapSetPath { get; set; }
    }
}
