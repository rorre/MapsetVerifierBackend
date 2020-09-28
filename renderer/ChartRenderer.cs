using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapsetVerifierBackend.helper;
using MapsetParser.starrating.standard;

namespace MapsetVerifierBackend.renderer
{
    class ChartRenderer : OverviewRenderer
    {
        public struct Point
        {
            public double x { get; set; }
            public double y { get; set; }
        }

        public struct Dataset
        {
            public string label { get; set; }
            [JsonIgnore]
            public string color { get; set; }
            public string backgroundColor => color;
            public string borderColor => color;
            public bool fill { get; set; }
            public List<Point> data { get; set; }
        }

        public struct ChartData
        {
            public List<string> labels { get; set; }
            public List<Dataset> datasets { get; set; }
        }

        private static Dataset GenerateChartData(DifficultySkillStrain diffStrain)
        {
            List<string> colors = new List<string> {
                "rgb(255, 99,  132)", // Red
                "rgb(255, 159, 64)",  // Orange
                "rgb(255, 205, 86)",  // Yellow
                "rgb(75,  192, 192)", // Green
                "rgb(54,  162, 235)", // Blue
                "rgb(153, 102, 255)", // Purple
                "rgb(231, 233, 237)"  // Gray or grey
            };
            var random = new Random();
            int index = random.Next(colors.Count);
            var color = colors[index];

            List<Point> dataPoints = new List<Point>();
            dataPoints.Add(new Point { x = 0, y = 0 });
            for (var i = 0; i < diffStrain.strainValue.Count; i++)
            {
                var strainTime = diffStrain.strainTime[i];
                var strainValue = Math.Round(diffStrain.strainValue[i], 2);
                dataPoints.Add(new Point { x = strainTime / 1000, y = strainValue });
            }

            Dataset dataset = new Dataset
            {
                label = Encode(diffStrain.beatmap.metadataSettings.version),
                color = color,
                fill = false,
                data = dataPoints
            };
            return dataset;
        }

        private static string CreateChartScript(List<DifficultySkillStrain> diffStrains, string canvasId)
        {
            var datasets = new List<Dataset>();
            foreach (var diff in diffStrains)
                datasets.Add(GenerateChartData(diff));

            List<double> longestX = new List<double>(); ;
            foreach (var diff in diffStrains)
                if (diff.strainTime.Count > longestX.Count)
                    longestX = diff.strainTime;
            List<string> longestXStr = longestX.ConvertAll(x => (x / 1000).ToString());

            ChartData chart = new ChartData { labels = longestXStr, datasets = datasets };
            string serealizedChart = JsonSerializer.Serialize(chart);
            return Script($"renderChart('{canvasId}', {serealizedChart})");
        }

        private static string RenderChartCanvas(string canvasId)
        {
            // Div is required for responsiveness
            // https://www.chartjs.org/docs/latest/general/responsive.html#important-note
            return Div("chart-container", $"<canvas id=\"{canvasId}\"></canvas>");
        }

        public static string RenderSkillChart(Type t, string canvasName, string canvasId, BeatmapSet aBeatmapSet)
        {
            if (!(t.IsSubclassOf(typeof(Skill))))
                throw new NotSupportedException("T must be a subclass of Skill.");

            List<DifficultySkillStrain> mapSkills = new List<DifficultySkillStrain>();
            foreach (Beatmap map in aBeatmapSet.beatmaps)
            {
                Object o = Activator.CreateInstance(t);
                Skill skill = (Skill)o;
                mapSkills.Add(StrainHelper.CalculateStrain(skill, map));
            }

            var aimStrainChart = String.Concat(
                RenderContainer(canvasName, RenderChartCanvas(canvasId)),
                CreateChartScript(mapSkills, canvasId)
            );
            return aimStrainChart;
        }

        public static string RenderChart(BeatmapSet aBeatmapSet) =>
            String.Concat(
                RenderSkillChart(typeof(Aim), "AimStrain", "aimStrainCanvas", aBeatmapSet),
                RenderSkillChart(typeof(Speed), "SpeedStrain", "speedStrainCanvas", aBeatmapSet)
            );
    }
}