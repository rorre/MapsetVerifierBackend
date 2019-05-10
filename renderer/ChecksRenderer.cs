using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierApp.renderer
{
    public class ChecksRenderer : BeatmapInfoRenderer
    {
        public static string Render(List<Issue> anIssues, BeatmapSet aBeatmapSet)
        {
            return String.Concat(
                    RenderBeatmapInfo(aBeatmapSet),
                    RenderBeatmapDifficulties(anIssues, aBeatmapSet),
                    RenderBeatmapChecks(anIssues, aBeatmapSet)
                );
        }

        private static string RenderBeatmapDifficulties(List<Issue> anIssues, BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            IEnumerable<Issue> generalIssues = anIssues.Where(anIssue => anIssue.CheckOrigin is GeneralCheck);

            return
                Div("beatmap-difficulties",

                    DivAttr("beatmap-difficulty noselect",
                        DataAttr("difficulty", "General"),
                        Div("medium-icon " + GetIcon(generalIssues) + "-icon"),
                        Div("difficulty-name",
                            "General"
                        )
                    ) +

                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        IEnumerable<Issue> issues = anIssues.Where(anIssue => anIssue.beatmap == aBeatmap).Except(generalIssues);
                        string version = Encode(aBeatmap.metadataSettings.version);
                        return
                            DivAttr("beatmap-difficulty noselect" + (aBeatmap == refBeatmap ? " beatmap-difficulty-selected" : ""),
                                DataAttr("difficulty", version),
                                Div("medium-icon " + GetIcon(issues) + "-icon"),
                                Div("difficulty-name",
                                    version
                                )
                            );
                    }))
                );
        }

        private static string RenderBeatmapChecks(List<Issue> anIssues, BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            IEnumerable<Issue> generalIssues = anIssues.Where(anIssue => anIssue.CheckOrigin is GeneralCheck);

            return
                Div("paste-separator") +
                Div("card-container-unselected",
                    
                    DivAttr("card-difficulty",
                        DataAttr("difficulty", "General"),
                        RenderBeatmapCategories(generalIssues, "General", true)
                    ) +

                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        IEnumerable<Issue> issues = anIssues.Where(anIssue => anIssue.beatmap == aBeatmap).Except(generalIssues);
                        string version = Encode(aBeatmap.metadataSettings.version);
                        return
                            DivAttr("card-difficulty",
                                DataAttr("difficulty", version),
                                RenderBeatmapInterpretation(aBeatmap, aBeatmap.GetDifficulty()),
                                RenderBeatmapCategories(issues, version)
                            );
                    }))
                ) +
                Div("paste-separator select-separator") +
                Div("card-container-selected");
        }

        private static string RenderBeatmapInterpretation(Beatmap aBeatmap, Beatmap.Difficulty aDefaultInterpretation)
        {
            if (aBeatmap == null)
                return "";

            return
                Div("",
                    DivAttr("interpret-container",
                        DataAttr("interpret", "difficulty"),
                        ((int[])Enum.GetValues(typeof(Beatmap.Difficulty))).Select(anEnum =>
                        {
                            // Expert and Ultra are considered the same interpretation
                            if (anEnum == (int)Beatmap.Difficulty.Ultra)
                                return "";

                            bool shouldSubstituteUltra =
                                anEnum == (int)Beatmap.Difficulty.Expert &&
                                aDefaultInterpretation == Beatmap.Difficulty.Ultra;

                            return
                                DivAttr("interpret" +
                                (anEnum == (int)aDefaultInterpretation || shouldSubstituteUltra ?
                                " interpret-selected interpret-default" : ""),
                                    DataAttr("interpret-severity", anEnum),
                                    Enum.GetName(typeof(Beatmap.Difficulty), anEnum)
                                );
                        }).ToArray()
                    )
                );
        }

        private static string RenderBeatmapCategories(IEnumerable<Issue> aBeatmapIssues, string aVersion, bool aGeneral = false)
        {
            return
                Div("card-difficulty-checks",
                    CheckerRegistry.GetChecks()
                        .Where(aCheck => aGeneral == aCheck is GeneralCheck)
                        .GroupBy(aCheck => aCheck.GetMetadata().Category)
                        .Select(aGroup =>
                    {
                        string category = aGroup.Key;
                        IEnumerable<Issue> issues = aBeatmapIssues.Where(anIssue => anIssue.CheckOrigin.GetMetadata().Category == category);

                        return
                            DivAttr("card",
                                DataAttr("difficulty", aVersion),
                                Div("card-box shadow noselect",
                                    Div("large-icon " + GetIcon(issues) + "-icon"),
                                    Div("card-title",
                                        Encode(category)
                                    )
                                ),
                                Div("card-details-container",
                                    Div("card-details",
                                        RenderBeatmapIssues(issues, category, aGeneral)
                                    )
                                )
                            );
                    }).ToArray()
                );
        }

        private static string RenderBeatmapIssues(IEnumerable<Issue> aCategoryIssues, string aCategory, bool aGeneral = false)
        {
            return
                String.Concat(
                CheckerRegistry.GetChecks()
                    .Where(aCheck =>
                        aCheck.GetMetadata().Category == aCategory &&
                        aGeneral == aCheck is GeneralCheck)
                    .Select(aCheck =>
                {
                    IEnumerable<Issue> issues = aCategoryIssues.Where(anIssue => anIssue.CheckOrigin == aCheck).ToList();
                    BeatmapCheckMetadata metadata = aCheck.GetMetadata() as BeatmapCheckMetadata;
                    
                    string message = aCheck.GetMetadata().Message;

                    return
                        DivAttr("card-detail",
                            metadata != null ? DifficultiesDataAttr(metadata.Difficulties) : "",
                            Div("card-detail-icon " + GetIcon(issues) + "-icon"),
                            (issues.Count() > 0 ?
                            Div("",
                                Div("card-detail-text",
                                    message
                                ),
                                Div("vertical-arrow card-detail-toggle")
                            ) :
                            Div("card-detail-text",
                                message
                            ))
                        ) +
                        RenderBeatmapDetails(issues);
                }));
        }

        private static string RenderBeatmapDetails(IEnumerable<Issue> aCheckIssues)
        {
            if (aCheckIssues.Count() == 0)
                return "";

            return
                Div("card-detail-instances",
                    aCheckIssues.Select(anIssue =>
                    {
                        string icon = GetIcon(anIssue.level);
                        string timestampedMessage = FormatTimestamps(anIssue.message);
                        if (timestampedMessage.Length == 0)
                            return "";

                        BeatmapCheckMetadata metadata = anIssue.CheckOrigin.GetMetadata() as BeatmapCheckMetadata;

                        return
                            DivAttr("card-detail",
                                (anIssue.Template != null ? DataAttr("template", anIssue.Template) : "") +
                                (metadata != null ? DifficultiesDataAttr(metadata.Difficulties) : ""),
                                Div("card-detail-icon " + icon + "-icon"),
                                Div("",
                                    timestampedMessage
                                )
                            );
                    }).ToArray()
                );
        }
    }
}
