using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapsetVerifierBackend.helper;
using MapsetParser.starrating.standard;

namespace MapsetVerifierBackend.renderer
{
    class ChartRenderer : OverviewRenderer
    {
        private static string GenerateChartData(DifficultySkillStrain diffStrain)
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

            // Start off from 0 seconds for consistent data across difficulties
            StringBuilder sb = new StringBuilder("{x:0,y:0}");
            for (var i = 0; i < diffStrain.strainValue.Count; i++)
            {
                var strainTime = diffStrain.strainTime[i];
                var strainValue = Math.Round(diffStrain.strainValue[i], 2);
                sb.Append(",{x: " + strainTime / 1000 + ", y: " + strainValue + "}");
            }

            var random = new Random();
            int index = random.Next(colors.Count);
            var color = colors[index];
            return String.Concat(
                "{label: '", Encode(diffStrain.beatmap.metadataSettings.version), "',",
                $"backgroundColor: '{color}', borderColor: '{color}', fill: false,",
                $"data: [{sb.ToString()}]", "}"
            );
        }
        private static string CreateChartScript(List<DifficultySkillStrain> diffStrains, string canvasId)
        {
            var datasets = new List<string>();
            foreach (var diff in diffStrains)
                datasets.Add(GenerateChartData(diff));

            List<double> longestX = new List<double>(); ;
            foreach (var diff in diffStrains)
                if (diff.strainTime.Count > longestX.Count)
                    longestX = diff.strainTime;
            longestX = longestX.ConvertAll(x => x / 1000);

            var labels = String.Join("','", longestX);
            var jsonDatasets = String.Join(',', datasets);

            // This is legit atrocious to look and read.
            // TODO: Better code to output Javascript.
            return Script(
                String.Concat(
                    @"options = {
                        spanGaps: true,
                        responsive: true,
                        elements: {point: {radius: 0}},
                        tooltips: {
                            mode: 'label',
                        },
                        hover: {
                            mode: 'nearest',
                            intersect: true
                        },
                        scales: {
                        xAxes: [{
                            display: false
                        }],
                        yAxes: [{
                            display: true,
                            scaleLabel: {
                                display: true,
                                labelString: 'Value'
                            }
                        }]
                        }
                    };",
                    "data= {", $"labels: ['{labels}'],",
                    $"datasets: [{jsonDatasets}]", "};",
                    @"var myLineChart = new Chart('", canvasId, @"', {
                        type: 'line',
                        data: data,
                        options: options
                    });"
                )
            );
        }
        private static string RenderChartCanvas(string canvasId)
        {
            // Div is required for responsiveness
            // https://www.chartjs.org/docs/latest/general/responsive.html#important-note
            return Div("chart-container", $"<canvas id=\"{canvasId}\"></canvas>");
        }
        public static string RenderChart(BeatmapSet aBeatmapSet)
        {
            List<DifficultySkillStrain> mapSkills = new List<DifficultySkillStrain>();

            Skill[] skills = {
                new Aim(),
            };
            foreach (Beatmap map in aBeatmapSet.beatmaps)
                foreach (Skill skill in skills)
                    mapSkills.Add(StrainHelper.CalculateStrain(skill, map));

            var aimDiffs = mapSkills.Where(x => x.skillType == skills[0]).ToList();
            var aimStrainChart = String.Concat(
                RenderContainer("AimStrain", RenderChartCanvas("aimStrainCanvas")),
                CreateChartScript(aimDiffs, "aimStrainCanvas")
            );
            return aimStrainChart;
        }
    }
}