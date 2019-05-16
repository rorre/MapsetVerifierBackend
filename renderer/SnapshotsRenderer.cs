using MapsetParser.objects;
using MapsetSnapshotter;
using MapsetSnapshotter.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MapsetVerifierApp.renderer
{
    public class SnapshotsRenderer : BeatmapInfoRenderer
    {
        private static List<DateTime> snapshotDates = null;

        public static string Render(BeatmapSet aBeatmapSet)
        {
            InitSnapshotDates(aBeatmapSet);
            
            return String.Concat(
                    RenderBeatmapInfo(aBeatmapSet),
                    RenderSnapshotInterpretation(),
                    RenderSnapshotDifficulties(aBeatmapSet),
                    RenderBeatmapSnapshots(aBeatmapSet)
                );
        }

        private static string RenderSnapshotInterpretation()
        {
            return
                Div("",
                    DivAttr("interpret-container",
                        DataAttr("interpret", "difficulty"),
                        snapshotDates.Select((aDate, anIndex) =>
                        {
                            return
                                DivAttr("interpret" + (
                                anIndex == snapshotDates.Count - 2 ? " interpret-selected" :
                                anIndex == snapshotDates.Count - 1 ? " interpret-default" : ""),
                                    DataAttr("interpret-severity", anIndex),
                                    aDate.ToString("yyyy-MM-dd HH:mm:ss")
                                );
                        }).ToArray()
                    )
                );
        }

        private static string RenderSnapshotDifficulties(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            string defaultIcon = "gear-gray";

            return
                Div("beatmap-difficulties",

                    DivAttr("beatmap-difficulty noselect",
                        DataAttr("difficulty", "Files"),
                        Div("medium-icon " + defaultIcon + "-icon"),
                        Div("difficulty-name",
                            "General"
                        )
                    ) +

                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        string version = Encode(aBeatmap.metadataSettings.version);
                        return
                            DivAttr("beatmap-difficulty noselect" + (aBeatmap == refBeatmap ? " beatmap-difficulty-selected" : ""),
                                DataAttr("difficulty", version),
                                Div("medium-icon " + defaultIcon + "-icon"),
                                Div("difficulty-name",
                                    version
                                )
                            );
                    }))
                );
        }

        private static string RenderBeatmapSnapshots(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];

            // Just need the beatmapset id to know in which snapshot folder to look for the files.
            IEnumerable<Snapshotter.Snapshot> refSnapshots =
                Snapshotter.GetSnapshots(
                    refBeatmap.metadataSettings.beatmapSetId.ToString(),
                    "files");

            Snapshotter.Snapshot lastSnapshot =
                refSnapshots.First(aSnapshot =>
                    aSnapshot.creationTime == refSnapshots.Max(anOtherSnapshot => anOtherSnapshot.creationTime));

            List<DiffInstance> refDiffs = new List<DiffInstance>();
            foreach (Snapshotter.Snapshot refSnapshot in refSnapshots)
            {
                IEnumerable<DiffInstance> refDiffsCompare = Snapshotter.Compare(refSnapshot, lastSnapshot.code).ToList();
                refDiffs.AddRange(Snapshotter.TranslateComparison(refDiffsCompare));
            }

            return
                Div("paste-separator") +
                Div("card-container-unselected",

                    DivAttr("card-difficulty",
                        DataAttr("difficulty", "Files"),
                        RenderSnapshotSections(refDiffs, refSnapshots, "Files", true)
                    ) +

                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        // Comparing current to all previous snapshots for this beatmap so the user
                        // can pick interpretation without loading in-between.
                        IEnumerable<Snapshotter.Snapshot> snapshots = Snapshotter.GetSnapshots(aBeatmap).ToList();
                        List<DiffInstance> diffs = new List<DiffInstance>();
                        foreach (Snapshotter.Snapshot snapshot in snapshots)
                        {
                            IEnumerable<DiffInstance> diffsCompare = Snapshotter.Compare(snapshot, aBeatmap.code).ToList();
                            diffs.AddRange(Snapshotter.TranslateComparison(diffsCompare));
                        }
                        
                        string version = Encode(aBeatmap.metadataSettings.version);
                        return
                            DivAttr("card-difficulty",
                                DataAttr("difficulty", version),
                                RenderSnapshotSections(diffs, snapshots, version)
                            );
                    }))
                ) +
                Div("paste-separator select-separator") +
                Div("card-container-selected");
        }

        private static string RenderSnapshotSections(
            IEnumerable<DiffInstance> aBeatmapDiffs,
            IEnumerable<Snapshotter.Snapshot> aSnapshots,
            string aVersion, bool aFiles = false)
        {
            return
                Div("card-difficulty-checks",
                    aBeatmapDiffs
                        .Where(aDiff => aFiles ?
                            aDiff.section == "Files" :
                            aDiff.section != "Files")
                        .GroupBy(aCheck => aCheck.section)
                        .Select(aSectionDiffs =>
                        {
                            return
                                DivAttr("card",
                                    DataAttr("difficulty", aVersion),
                                    Div("card-box shadow noselect",
                                        Div("large-icon " + GetIcon(aSectionDiffs) + "-icon"),
                                        Div("card-title",
                                            Encode(aSectionDiffs.Key)
                                        )
                                    ),
                                    Div("card-details-container",
                                        Div("card-details",
                                            RenderSnapshotDiffs(aSectionDiffs, aSnapshots)
                                        )
                                    )
                                );
                        }).ToArray()
                );
        }

        private static string RenderSnapshotDiffs(
            IEnumerable<DiffInstance> aSectionDiffs,
            IEnumerable<Snapshotter.Snapshot> aSnapshots)
        {
            return
                String.Concat(
                aSectionDiffs.Select(aDiff =>
                {
                    string message = FormatTimestamps(aDiff.difference);
                    string condition = GetDiffCondition(aDiff, aSnapshots);

                    return
                        DivAttr("card-detail",
                            DataAttr("condition", "difficulty=" + condition),
                            Div("card-detail-icon " + GetIcon(aDiff) + "-icon"),
                            (aDiff.details.Any() ?
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
                        RenderDiffDetails(aDiff.details, condition);
                }));
        }

        private static string RenderDiffDetails(List<string> aDetails, string aCondition)
        {
            string detailIcon = "gear-blue";
            return
                Div("card-detail-instances",
                    aDetails.Select(aDetail =>
                    {
                        string timestampedMessage = FormatTimestamps(aDetail);
                        if (timestampedMessage.Length == 0)
                            return "";

                        return
                            DivAttr("card-detail",
                                DataAttr("condition", "difficulty=" + aCondition),
                                Div("card-detail-icon " + detailIcon + "-icon"),
                                Div("",
                                    timestampedMessage
                                )
                            );
                    }).ToArray()
                );
        }

        private static string GetDiffCondition(DiffInstance aDiff, IEnumerable<Snapshotter.Snapshot> aSnapshots)
        {
            int myNextIndex =
                aSnapshots.ToList().FindLastIndex(aSnapshot =>
                    aSnapshot.creationTime == aDiff.snapshotCreationDate) + 1;

            DateTime myNextDate =
                myNextIndex >= aSnapshots.Count() ?
                    aSnapshots.Last().creationTime :
                    aSnapshots.ElementAt(myNextIndex).creationTime;

            List<int> indexes = new List<int>();
            for (int i = 0; i < snapshotDates.Count; ++i)
                if (snapshotDates.ElementAt(i) < myNextDate &&
                    snapshotDates.ElementAt(i) >= aDiff.snapshotCreationDate)
                    indexes.Add(i);

            return String.Join(",", indexes);
        }

        private static void InitSnapshotDates(BeatmapSet aBeatmapSet)
        {
            snapshotDates =
               aBeatmapSet.beatmaps.SelectMany(aBeatmap =>
                   Snapshotter.GetSnapshots(aBeatmap)
                       .Select(aSnapshot => aSnapshot.creationTime))
                   .OrderBy(aDate => aDate).Distinct().ToList();

            snapshotDates.AddRange(Snapshotter.GetSnapshots(
                aBeatmapSet.beatmaps.First().metadataSettings.beatmapSetId.ToString(), "files")
                    .Select(aSnapshot => aSnapshot.creationTime));

            snapshotDates = snapshotDates.Distinct().ToList();
        }

        private static string GetIcon(DiffInstance aDiff)
        {
            return
                aDiff.diffType == Snapshotter.DiffType.Changed ? "gear-blue" :
                aDiff.diffType == Snapshotter.DiffType.Added   ? "plus" :
                aDiff.diffType == Snapshotter.DiffType.Removed ? "minus" :
                "gear-gray";
        }

        private static string GetIcon(IEnumerable<DiffInstance> aDiffs)
        {
            return
                aDiffs.Any(aDiff => aDiff.diffType == Snapshotter.DiffType.Added) &&
                aDiffs.Any(aDiff => aDiff.diffType == Snapshotter.DiffType.Removed) ||
                aDiffs.Any(aDiff => aDiff.diffType == Snapshotter.DiffType.Changed) ? "gear-blue" :

                aDiffs.Any(aDiff => aDiff.diffType == Snapshotter.DiffType.Added)   ? "plus" :
                aDiffs.Any(aDiff => aDiff.diffType == Snapshotter.DiffType.Removed) ? "minus" :
                "gear-gray";
        }
    }
}
