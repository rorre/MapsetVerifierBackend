using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.settings;
using MapsetParser.statics;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MapsetVerifierBackend.renderer
{
    class OverviewRenderer : BeatmapInfoRenderer
    {
        public static string Render(BeatmapSet aBeatmapSet)
        {
            return String.Concat(
                    RenderBeatmapInfo(aBeatmapSet),
                    Div("paste-separator"),
                    RenderTimelineComparison(aBeatmapSet),
                    RenderMetadata(aBeatmapSet),
                    RenderGeneralSettings(aBeatmapSet),
                    RenderDifficultySettings(aBeatmapSet),
                    RenderStatistics(aBeatmapSet),
                    RenderResources(aBeatmapSet),
                    RenderColourSettings(aBeatmapSet),
                    Div("overview-footer")
                );
        }

        private static string RenderTimelineComparison(BeatmapSet aBeatmapSet)
        {
            StringBuilder topHtml = new StringBuilder();
            StringBuilder contentHTML = new StringBuilder();
            StringBuilder footerHTML = new StringBuilder();

            double zoomFactor = 8;

            topHtml.Append(
                Div("overview-timeline-difficulties",
                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        return
                            Div("overview-timeline-difficulty noselect",
                                aBeatmap.metadataSettings.version
                            );
                    }))
                )
            );

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                // Mania can have multiple notes at the same time so we'll need to do that differently.
                if (beatmap.generalSettings.mode == Beatmap.Mode.Mania)
                    continue;

                double startTimeObjects =
                    aBeatmapSet.beatmaps.Min(aBeatmap =>
                        aBeatmap.hitObjects.Min(anObject =>
                            anObject.time));
                double startTimeLines =
                    aBeatmapSet.beatmaps.Min(aBeatmap =>
                        aBeatmap.timingLines.Min(aLine =>
                            aLine.offset));

                double startTime =
                    (startTimeObjects < startTimeLines ?
                        startTimeObjects :
                        startTimeLines)
                    - 2000;
                double prevObjectTime = startTime;

                double sampleTime     = startTime;
                double prevSampleTime = sampleTime;

                double endTimeObjects =
                    aBeatmapSet.beatmaps.Max(aBeatmap =>
                        aBeatmap.hitObjects.Last()?.GetEndTime() ?? 0);
                double endTimeLines =
                    aBeatmapSet.beatmaps.Min(aBeatmap =>
                        aBeatmap.timingLines.Last()?.offset ?? 0);

                double endTime =
                    (endTimeObjects < endTimeLines ?
                        endTimeObjects :
                        endTimeLines)
                    + 2000;

                contentHTML.Append(
                    Div("overview-timeline locked noselect",
                        Div("overview-timeline-right",
                            Div("overview-timeline-right-options",
                                DivAttr("overview-timeline-right-option overview-timeline-right-option-remove",
                                    Tooltip("Removes the timeline.")),
                                DivAttr("overview-timeline-right-option overview-timeline-right-option-lock",
                                    Tooltip("Locks or unlocks scrolling. All locked timelines scroll together."))
                            ),
                            Div("overview-timeline-right-title",
                                beatmap.metadataSettings.version
                            )
                        ),
                        Div("overview-timeline-ticks",
                            String.Concat(
                            beatmap.timingLines.OfType<UninheritedLine>().Select(aLine =>
                            {
                                UninheritedLine nextLine = beatmap.GetNextTimingLine<UninheritedLine>(aLine.offset);
                                double nextSwap = nextLine?.offset ?? endTime;

                                StringBuilder tickDivs = new StringBuilder();

                                // To get precision down to both 1/16th and 1/12th of a beat we need to sample...
                                // 16 = 2^4, 12 = 2^2*3, 2^4*3 = 48 times per beat.
                                int samplesPerBeat = 48;
                                for (int i = 0; i < (nextSwap - aLine.offset) / aLine.msPerBeat * samplesPerBeat; ++i)
                                {
                                    // Add the practical unsnap to avoid things getting unsnapped the further into the map you go.
                                    sampleTime =
                                        aLine.offset + i * aLine.msPerBeat / samplesPerBeat +
                                        beatmap.GetPracticalUnsnap(aLine.offset + i * aLine.msPerBeat / samplesPerBeat);

                                    bool hasEdge =
                                        beatmap.GetHitObject(sampleTime)?.GetEdgeTimes().Any(anEdgeTime =>
                                            Math.Abs(anEdgeTime - sampleTime) < 2) ?? false;

                                    if (i % (samplesPerBeat / 4) == 0 ||
                                        hasEdge && (
                                            i % (samplesPerBeat / 12) == 0 ||
                                            i % (samplesPerBeat / 16) == 0))
                                    {
                                        tickDivs.Append(
                                            DivAttr("overview-timeline-tick",
                                                " style=\"margin-left:" + ((sampleTime - prevSampleTime) / zoomFactor) + "px\"",
                                                Div("overview-timeline-ticks-base " + (hasEdge ? " hasobject " : "") + (
                                                    i % (samplesPerBeat * 4)  == 0 ? "overview-timeline-ticks-largewhite" :
                                                    i % (samplesPerBeat * 1)  == 0 ? "overview-timeline-ticks-white" :
                                                    i % (samplesPerBeat / 2)  == 0 ? "overview-timeline-ticks-red" :
                                                    i % (samplesPerBeat / 3)  == 0 ? "overview-timeline-ticks-magenta" :
                                                    i % (samplesPerBeat / 4)  == 0 ? "overview-timeline-ticks-blue" :
                                                    i % (samplesPerBeat / 6)  == 0 ? "overview-timeline-ticks-purple" :
                                                    i % (samplesPerBeat / 8)  == 0 ? "overview-timeline-ticks-yellow" :
                                                    i % (samplesPerBeat / 12) == 0 ? "overview-timeline-ticks-gray" :
                                                    i % (samplesPerBeat / 16) == 0 ? "overview-timeline-ticks-gray" :
                                                                                     "overview-timeline-ticks-unsnapped") // these last ones shouldn't appear
                                                )
                                            ));

                                        prevSampleTime = sampleTime;
                                    }
                                }

                                return tickDivs.ToString();
                            }))),
                        String.Concat(
                        beatmap.hitObjects.Select(aHitObject =>
                        {
                            double prevTime = startTime;
                            startTime = aHitObject.time;

                            if (aHitObject is Circle circle)
                                return
                                    DivAttr("overview-timeline-object",
                                        " style=\"margin-left:" + ((circle.time - prevTime) / zoomFactor) + "px;\"",
                                        DivAttr("overview-timeline-circle",
                                            DataAttr("timestamp", Timestamp.Get(circle.time)) +
                                            " style=\"" +
                                                RenderHitObjectBackgroundStyle(circle, beatmap) +
                                                RenderHitObjectSizeStyle(circle, beatmap) +
                                            "\"")
                                    );
                            else if (aHitObject is Slider slider)
                            {
                                // Big drumrolls need additional length due to the increased radius.
                                // In taiko regular notes are smaller so it needs a reduced radius otherwise.
                                int addedWidth =
                                    beatmap.generalSettings.mode == Beatmap.Mode.Taiko ?
                                        slider.HasHitSound(HitObject.HitSound.Finish) ?
                                            32 :
                                            22 :
                                        23;
                                return
                                    DivAttr("overview-timeline-object",
                                        DataAttr("timestamp", Timestamp.Get(slider.time)) +
                                        " style=\"margin-left:" + ((slider.time - prevTime) / zoomFactor) + "px;\"",
                                        String.Concat(
                                        slider.GetEdgeTimes().Select((anEdgeTime, anIndex) =>
                                        {
                                            return
                                                DivAttr("overview-timeline-object edge",
                                                    "style=\"margin-left:" + ((anEdgeTime - slider.time) / zoomFactor) + "px;\"",
                                                    DivAttr("overview-timeline-edge" +
                                                        (anIndex > 0 && anIndex < slider.edgeAmount ?
                                                            " overview-timeline-edge-reverse" : ""),
                                                        " style=\"" +
                                                            RenderHitObjectSizeStyle(slider, beatmap) +
                                                        "\"")
                                                );
                                        })),
                                        DivAttr("overview-timeline-path",
                                            " style=\"" +
                                                "width:" + ((slider.endTime - slider.time) / zoomFactor) + "px;" +
                                                "padding-right:" + addedWidth + "px;" +
                                                RenderHitObjectBackgroundStyle(slider, beatmap) +
                                                RenderHitObjectSizeStyle(slider, beatmap, true) + "\"")
                                    );
                            }
                            else if (aHitObject is Spinner spinner)
                            {
                                int addedWidth = beatmap.generalSettings.mode == Beatmap.Mode.Taiko ? 22 : 23;
                                return
                                    DivAttr("overview-timeline-object",
                                        DataAttr("timestamp", Timestamp.Get(spinner.time)) +
                                        " style=\"margin-left:" + ((spinner.time - prevTime) / zoomFactor) + "px;\"",
                                        Div("overview-timeline-object edge",
                                            Div("overview-timeline-edge")
                                        ),
                                        DivAttr("overview-timeline-object edge",
                                            " style=\"margin-left:" + ((spinner.endTime - spinner.time) / zoomFactor) + "px;\"",
                                            Div("overview-timeline-edge")
                                        ),
                                        DivAttr("overview-timeline-path",
                                            " style=\"" +
                                                "width:" + ((spinner.endTime - spinner.time) / zoomFactor) + "px;" +
                                                "padding-right:" + addedWidth + "px;\"")
                                    );
                            }

                            return
                                DivAttr("overview-timeline-object",
                                    " style=\"margin-left:" + ((aHitObject.time - prevTime) / zoomFactor) + "px;\""
                                );
                        }))
                    )
                );
            }

            footerHTML.Append(
                Div("overview-timeline-slider") +
                Div("overview-timeline-buttons",
                    DivAttr("overview-timeline-button plus-icon",
                        Tooltip("Zooms in timelines.")
                    ),
                    DivAttr("overview-timeline-button minus-icon",
                        Tooltip("Zooms out timelines.")
                    ),
                    Div("overview-timeline-buttons-zoomamount",
                        "1×"
                    )
                ));

            return
                RenderContainer("Timeline Comparison (Beta)",
                    Div("overview-timeline-top",
                        Div("overview-timeline-hints",
                            Div("overview-timeline-hint",
                                "Move timeline: Click & drag"
                            ),
                            Div("overview-timeline-hint",
                                "Speed: Shift / Ctrl"
                            ),
                            Div("overview-timeline-hint",
                                "Timestamp: Alt + Click"
                            )
                        ),
                        topHtml.ToString()
                    ),
                    Div("overview-timeline-content",
                        contentHTML.ToString()
                    ),
                    Div("overview-timeline-footer",
                        footerHTML.ToString()
                    )
                ); ;
        }

        private static string RenderHitObjectBackgroundStyle(HitObject aHitObject, Beatmap aBeatmap)
        {
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko)
            {
                // drumroll
                if (aHitObject is Slider)
                    return "background-color:rgba(252,191,31,0.5);";
                // kat
                if (aHitObject.HasHitSound(HitObject.HitSound.Clap) || aHitObject.HasHitSound(HitObject.HitSound.Whistle))
                    return "background-color:rgba(68,141,171,0.5);";
                // don
                return "background-color:rgba(235,69,44,0.5);";
            }

            int colourIndex = aBeatmap.GetComboColourIndex(aHitObject.time);
            if(aBeatmap.colourSettings.combos.Count() > colourIndex)
                return
                "background-color:rgba(" +
                    aBeatmap.colourSettings.combos[colourIndex].X + "," +
                    aBeatmap.colourSettings.combos[colourIndex].Y + "," +
                    aBeatmap.colourSettings.combos[colourIndex].Z + ", 0.5);";
            else
                // Should no custom combo colours exist, objects will simply be gray.
                return
                "background-color:rgba(125,125,125, 0.5);";
        }

        /*private static string RenderHitObjectBorderStyle(HitObject aHitObject, Beatmap aBeatmap)
        {
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko)
            {
                // Hit sounds are already covered by the background style.
                return "";
            }

            int colourIndex = aBeatmap.GetComboColourIndex(aHitObject.time);
            return
                "border-top:1px solid rgba(" +
                    aBeatmap.colourSettings.combos[colourIndex].X + "," +
                    aBeatmap.colourSettings.combos[colourIndex].Y + "," +
                    aBeatmap.colourSettings.combos[colourIndex].Z + ", 0.125);";
        }*/

        private static string RenderHitObjectSizeStyle(HitObject aHitObject, Beatmap aBeatmap, bool aIsSliderPath = false)
        {
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko)
            {
                if(aHitObject.HasHitSound(HitObject.HitSound.Finish))
                    // big don/kat
                    return
                        "height:30px;" +
                        (aIsSliderPath ?
                            "border-radius:15px;" :
                            "width:30px;") +
                        "margin-left:-15.5px;";

                return
                    "height:20px;" +
                    (aIsSliderPath ?
                        "border-radius:10px;" :
                        "width:20px;") +
                    "margin-left:-10.5px;" +
                    "margin-bottom:-2px;";
            }

            return "";
        }

        private static string RenderMetadata(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("Metadata",
                    (aBeatmapSet.beatmaps.Any(aBeatmap =>
                        aBeatmap.metadataSettings.artist?.ToString() !=
                        aBeatmap.metadataSettings.artistUnicode?.ToString()) ?

                            // If the romanised field is always the same as the unicode field, we don't need to display them separately.
                            RenderField("Artist",
                                RenderBeatmapContent(aBeatmapSet, "Romanised", aBeatmap =>
                                    aBeatmap.metadataSettings.artist?.ToString()) +
                                RenderBeatmapContent(aBeatmapSet, "Unicode", aBeatmap =>
                                    aBeatmap.metadataSettings.artistUnicode?.ToString())
                            ) :
                            RenderBeatmapContent(aBeatmapSet, "Artist", aBeatmap =>
                                aBeatmap.metadataSettings.artist?.ToString())

                    ),
                    (aBeatmapSet.beatmaps.Any(aBeatmap =>
                        aBeatmap.metadataSettings.title?.ToString() !=
                        aBeatmap.metadataSettings.titleUnicode?.ToString()) ?

                            RenderField("Title",
                                RenderBeatmapContent(aBeatmapSet, "Romanised", aBeatmap =>
                                    aBeatmap.metadataSettings.title?.ToString()) +
                                RenderBeatmapContent(aBeatmapSet, "Unicode", aBeatmap =>
                                    aBeatmap.metadataSettings.titleUnicode?.ToString())
                            ) :
                            RenderBeatmapContent(aBeatmapSet, "Title", aBeatmap =>
                                aBeatmap.metadataSettings.title?.ToString())

                    ),
                    RenderBeatmapContent(aBeatmapSet, "Creator", aBeatmap =>
                        aBeatmap.metadataSettings.creator?.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Source", aBeatmap =>
                        aBeatmap.metadataSettings.source?.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Tags", aBeatmap =>
                        aBeatmap.metadataSettings.tags?.ToString())
                );
        }

        private static string RenderGeneralSettings(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("General Settings",
                    RenderBeatmapContent(aBeatmapSet, "Audio Filename", aBeatmap =>
                        aBeatmap.generalSettings.audioFileName?.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Audio Lead-in", aBeatmap =>
                        aBeatmap.generalSettings.audioLeadIn.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Mode", aBeatmap =>
                        aBeatmap.generalSettings.mode.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Stack Leniency", aBeatmap =>
                    {
                        // Stack leniency only does stuff for standard.
                        if (aBeatmap.generalSettings.mode == Beatmap.Mode.Standard)
                            return aBeatmap.generalSettings.stackLeniency.ToString();
                        else
                            return "N/A";
                    }),
                    RenderField("Countdown Settings",
                        RenderBeatmapContent(aBeatmapSet, "Has Countdown", aBeatmap =>
                        {
                            return
                                (aBeatmap.GetCountdownStartBeat() >= 0 &&
                                aBeatmap.generalSettings.countdown != GeneralSettings.Countdown.None)
                                .ToString();
                        }),
                        RenderBeatmapContent(aBeatmapSet, "Countdown Speed", aBeatmap =>
                        {
                            if (aBeatmap.GetCountdownStartBeat() >= 0 && aBeatmap.generalSettings.countdown != GeneralSettings.Countdown.None)
                                return aBeatmap.generalSettings.countdown.ToString();
                            else
                                return "N/A";
                        }),
                        RenderBeatmapContent(aBeatmapSet, "Countdown Offset", aBeatmap =>
                        {
                            if (aBeatmap.GetCountdownStartBeat() >= 0 && aBeatmap.generalSettings.countdown != GeneralSettings.Countdown.None)
                                return aBeatmap.generalSettings.countdownBeatOffset.ToString();
                            else
                                return "N/A";
                        })
                    ),
                    RenderBeatmapContent(aBeatmapSet, "Epilepsy Warning", aBeatmap =>
                    {
                        if (aBeatmap.videos.Any() || aBeatmap.HasDifficultySpecificStoryboard() || (aBeatmapSet.osb?.IsUsed() ?? false))
                            return aBeatmap.generalSettings.epilepsyWarning.ToString();
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Letterbox During Breaks", aBeatmap =>
                    {
                        if(aBeatmap.breaks.Any() || !aBeatmap.generalSettings.letterbox)
                            return aBeatmap.generalSettings.letterbox.ToString();
                        else
                            return "N/A";
                    }),
                    RenderField("Storyboard Settings",
                        RenderBeatmapContent(aBeatmapSet, "Has Storyboard", aBeatmap =>
                        {
                            return (aBeatmap.HasDifficultySpecificStoryboard() || (aBeatmapSet.osb?.IsUsed() ?? false)).ToString();
                        }),
                        RenderBeatmapContent(aBeatmapSet, "Widescreen Support", aBeatmap =>
                        {
                            if (aBeatmap.HasDifficultySpecificStoryboard() || (aBeatmapSet.osb?.IsUsed() ?? false))
                                return aBeatmap.generalSettings.widescreenSupport.ToString();
                            else
                                return "N/A";
                        }),
                        RenderBeatmapContent(aBeatmapSet, "In Front Of Combo Fire", aBeatmap =>
                        {
                            if (aBeatmap.HasDifficultySpecificStoryboard() || (aBeatmapSet.osb?.IsUsed() ?? false))
                                return aBeatmap.generalSettings.storyInFrontOfFire.ToString();
                            else
                                return "N/A";
                        }),
                        RenderBeatmapContent(aBeatmapSet, "Use Skin Sprites", aBeatmap =>
                        {
                            if (aBeatmap.HasDifficultySpecificStoryboard() || (aBeatmapSet.osb?.IsUsed() ?? false))
                                return aBeatmap.generalSettings.useSkinSprites.ToString();
                            else
                                return "N/A";
                        })
                    ),
                    RenderBeatmapContent(aBeatmapSet, "Preview Time", aBeatmap =>
                        aBeatmap.generalSettings.previewTime >= 0 ?
                            Timestamp.Get(aBeatmap.generalSettings.previewTime) :
                            ""),
                    RenderBeatmapContent(aBeatmapSet, "Skin Preference", aBeatmap =>
                        aBeatmap.generalSettings.skinPreference?.ToString())

                    // Special N+1 Style is apparently not used by any mode, was meant for mania but was later overriden by user settings.
                );
        }

        private static string RenderDifficultySettings(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("Difficulty Settings",
                    RenderBeatmapContent(aBeatmapSet, "HP Drain", aBeatmap =>
                        aBeatmap.difficultySettings.hpDrain.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Circle Size", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Taiko)
                            return aBeatmap.difficultySettings.circleSize.ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Overall Difficulty", aBeatmap =>
                        aBeatmap.difficultySettings.overallDifficulty.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Approach Rate", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Mania)
                            return aBeatmap.difficultySettings.approachRate.ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Slider Tick Rate", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Mania)
                            return aBeatmap.difficultySettings.sliderTickRate.ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "SV Multiplier", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Mania)
                            return aBeatmap.difficultySettings.sliderMultiplier.ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    })
                );
        }

        private static string RenderStatistics(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("Statistics",
                    RenderBeatmapContent(aBeatmapSet, "Old Star Rating", aBeatmap =>
                    {
                        // Current star rating calc only supports standard.
                        if (aBeatmap.generalSettings.mode == Beatmap.Mode.Standard)
                            return $"{aBeatmap.starRating:0.##}";
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Circle Count", aBeatmap =>
                        aBeatmap.hitObjects.OfType<Circle>().Count().ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Slider Count", aBeatmap =>
                    {
                        // Sliders don't exist in mania.
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Mania)
                            return aBeatmap.hitObjects.OfType<Slider>().Count().ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Spinner Count", aBeatmap =>
                    {
                        // Spinners don't exist in mania.
                        if (aBeatmap.generalSettings.mode != Beatmap.Mode.Mania)
                            return aBeatmap.hitObjects.OfType<Spinner>().Count().ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Hold Note Count", aBeatmap =>
                    {
                        // Hold notes only exist in mania.
                        if (aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                            return aBeatmap.hitObjects.OfType<HoldNote>().Count().ToString(CultureInfo.InvariantCulture);
                        else
                            return "N/A";
                    }),
                    RenderBeatmapContent(aBeatmapSet, "Break Count", aBeatmap =>
                        aBeatmap.breaks.Count().ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Drain Time", aBeatmap =>
                        Timestamp.Get(aBeatmap.GetDrainTime()).ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Play Time", aBeatmap =>
                        Timestamp.Get(aBeatmap.GetPlayTime()).ToString(CultureInfo.InvariantCulture))
                );
        }

        private static string RenderDataSize(long aSize)
        {
            if (aSize / Math.Pow(1024, 3) >= 1)
                return $"{aSize / Math.Pow(1024, 3):0.##} GB";
            else if (aSize / Math.Pow(1024, 2) >= 1 && aSize / Math.Pow(1024, 3) < 1)
                return $"{aSize / Math.Pow(1024, 2):0.##} MB";
            else if (aSize / Math.Pow(1024, 1) >= 1 && aSize / Math.Pow(1024, 2) < 1)
                return $"{aSize / Math.Pow(1024, 1):0.##} KB";
            else
                return $"{aSize / Math.Pow(1024, 0):0.##} B";
        }

        private static string RenderFileSize(string aFullPath)
        {
            if (!File.Exists(aFullPath))
                return "";

            FileInfo fileInfo = new FileInfo(aFullPath);

            return RenderDataSize(fileInfo.Length);
        }

        private static long GetDirectorySize(DirectoryInfo aDirectoryInfo)
        {
            long totalSize = 0;

            FileInfo[] fileInfos = aDirectoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
                totalSize += fileInfo.Length;

            DirectoryInfo[] directoryInfos = aDirectoryInfo.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directoryInfos)
                totalSize += GetDirectorySize(directoryInfo);

            return totalSize;
        }

        private static string RenderDirectorySize(string aFullPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(aFullPath);

            return RenderDataSize(GetDirectorySize(directoryInfo));
        }

        private static string RenderResources(BeatmapSet aBeatmapSet)
        {
            string RenderFloat(List<string> aFiles, Func<string, string> aFunc)
            {
                string content =
                    String.Join("<br>",
                        aFiles.Select(aFile =>
                        {
                            string path =
                                aBeatmapSet.hitSoundFiles.FirstOrDefault(anOtherFile =>
                                    anOtherFile.StartsWith(aFile + "."));
                            if (path == null)
                                return null;

                            return aFunc(path);
                        }).Where(aValue => aValue != null)
                    );

                if (content.Length == 0)
                    return "";

                return Div("overview-float", content);
            }

            Dictionary<string, int> hsUsedCount = new Dictionary<string, int>();

            return
                RenderContainer("Resources",
                    RenderBeatmapContent(aBeatmapSet, "Used Hit Sound File(s)", aBeatmap =>
                    {
                        List<string> usedHitSoundFiles =
                            aBeatmap.hitObjects.SelectMany(anObject => anObject.GetUsedHitSoundFileNames()).ToList();

                        List<string> distinctSortedFiles =
                            usedHitSoundFiles.Distinct().OrderByDescending(aFile => aFile).ToList();

                        return
                            RenderFloat(distinctSortedFiles, aPath => Encode(aPath)) +
                            RenderFloat(distinctSortedFiles, aPath =>
                            {
                                int count = usedHitSoundFiles.Where(anOtherFile => aPath.StartsWith(anOtherFile + ".")).Count();

                                // Used for total hit sound usage overview
                                if (hsUsedCount.ContainsKey(aPath))
                                    hsUsedCount[aPath] += count;
                                else
                                    hsUsedCount[aPath] = count;

                                return $"× {count}";
                            });
                    }, false),
                    RenderField("Total Used Hit Sound File(s)",
                        (hsUsedCount.Any() ?
                            Div("overview-float",
                                String.Join("<br>",
                                    hsUsedCount.Select(aPair => aPair.Key)
                                )
                            ) +
                            Div("overview-float",
                                String.Join("<br>",
                                    hsUsedCount.Select(aPair =>
                                    {
                                        string fullPath = Path.Combine(aBeatmapSet.songPath, aPair.Key);

                                        return Encode(RenderFileSize(fullPath));
                                    })
                                )
                            ) +
                            Div("overview-float",
                                String.Join("<br>",
                                    hsUsedCount.Select(aPair =>
                                    {
                                        string fullPath = Path.Combine(aBeatmapSet.songPath, aPair.Key);
                                        double duration = Audio.GetDuration(fullPath);

                                        if (duration < 0)
                                            return "0 ms";

                                        return $"{duration:0.##} ms";
                                    })
                                )
                            ) +
                            Div("overview-float",
                                String.Join("<br>",
                                    hsUsedCount.Select(aPair =>
                                    {
                                        string fullPath = Path.Combine(aBeatmapSet.songPath, aPair.Key);

                                        return Encode(Audio.EnumToString(Audio.GetFormat(fullPath)));
                                    })
                                )
                            ) +
                            Div("overview-float",
                                String.Join("<br>",
                                    hsUsedCount.Select(aPair => "× " + aPair.Value)
                                )
                            )
                        : "")
                    ),
                    RenderBeatmapContent(aBeatmapSet, "Background File(s)", aBeatmap =>
                    {
                        if (aBeatmap.backgrounds.Any())
                        {
                            string fullPath = Path.Combine(aBeatmap.songPath, aBeatmap.backgrounds.First().path);
                            if (!File.Exists(fullPath))
                                return "";

                            string error = null;
                            TagLib.File tagFile = null;
                            try
                            { tagFile = new FileAbstraction(fullPath).GetTagFile(); }
                            catch (Exception exception)
                            {
                                error = exception.Message;
                            }

                            if (error != null || tagFile == null)
                                return
                                    Div("overview-float",
                                        Encode(aBeatmap.backgrounds.First().path)
                                    ) +
                                    Div("overview-float",
                                        Encode(RenderFileSize(fullPath))
                                    ) +
                                    Div("overview-float",
                                        Encode($"(failed getting proprties; {error})")
                                    );

                            return
                                Div("overview-float",
                                    Encode(aBeatmap.backgrounds.First().path)
                                ) +
                                Div("overview-float",
                                    Encode(RenderFileSize(fullPath))
                                ) +
                                Div("overview-float",
                                    Encode(tagFile.Properties.PhotoWidth + " x " + tagFile.Properties.PhotoHeight)
                                );
                        }
                        else
                            return "";
                    }, false),
                    RenderBeatmapContent(aBeatmapSet, "Video File(s)", aBeatmap =>
                    {
                        if (aBeatmap.videos.Any() || (aBeatmapSet.osb?.videos.Any() ?? false))
                        {
                            string fullPath = Path.Combine(aBeatmap.songPath, aBeatmap.videos.First().path);
                            if (!File.Exists(fullPath))
                                return "";

                            string error = null;
                            TagLib.File tagFile = null;
                            try
                            { tagFile = new FileAbstraction(fullPath).GetTagFile(); }
                            catch (Exception exception)
                            {
                                error = exception.Message;
                            }

                            if (error != null || tagFile == null)
                                return
                                    Div("overview-float",
                                        Encode(aBeatmap.videos.First().path)
                                    ) +
                                    Div("overview-float",
                                        Encode(RenderFileSize(fullPath))
                                    ) +
                                    Div("overview-float",
                                        Encode($"(failed getting proprties; {error})")
                                    );

                            return
                                Div("overview-float",
                                    Encode(aBeatmap.videos.First().path)
                                ) +
                                Div("overview-float",
                                    Encode(RenderFileSize(fullPath))
                                ) +
                                Div("overview-float",
                                    FormatTimestamps(Encode(Timestamp.Get(tagFile.Properties.Duration.TotalMilliseconds)))
                                ) +
                                Div("overview-float",
                                    Encode(tagFile.Properties.VideoWidth + " x " + tagFile.Properties.VideoHeight)
                                );
                        }
                        else
                            return "";
                    }, false),
                    RenderBeatmapContent(aBeatmapSet, "Audio File(s)", aBeatmap =>
                    {
                        string path = aBeatmap.GetAudioFilePath();
                        if (path == null)
                            return "";

                        FileInfo fileInfo = new FileInfo(path);
                        ManagedBass.ChannelType format = Audio.GetFormat(path);
                        double duration = Audio.GetDuration(path);

                        return
                            Div("overview-float",
                                Encode(PathStatic.RelativePath(path, aBeatmap.songPath))
                            ) +
                            Div("overview-float",
                                Encode(RenderFileSize(path))
                            ) +
                            Div("overview-float",
                                FormatTimestamps(Encode(Timestamp.Get(duration)))
                            ) +
                            Div("overview-float",
                                Encode(Audio.EnumToString(format))
                            );
                    }, false),
                    RenderBeatmapContent(aBeatmapSet, "Audio Bitrate", aBeatmap =>
                    {
                        string path = aBeatmap.GetAudioFilePath();
                        if (path == null)
                            return "N/A";

                        AudioFile audioFile = new AudioFile(path);

                        return
                            Div("overview-float",
                                (audioFile.GetLowestBitrate() == audioFile.GetHighestBitrate() ?
                                    $"CBR, {audioFile.GetLowestBitrate() / 1000:0.##} kbps" :
                                    $"VBR, {audioFile.GetLowestBitrate() / 1000:0.##} kbps to {audioFile.GetHighestBitrate() / 1000:0.##} kbps, " +
                                    $"average {audioFile.GetAverageBitrate() / 1000:0.##} kbps")
                            );
                    }, false),
                    RenderField("Has .osb",
                         Encode((aBeatmapSet.osb?.IsUsed() ?? false).ToString())
                    ),
                    RenderBeatmapContent(aBeatmapSet, "Has .osu Specific Storyboard", aBeatmap =>
                        aBeatmap.HasDifficultySpecificStoryboard().ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Song Folder Size", aBeatmap =>
                        RenderDirectorySize(aBeatmap.songPath))
                );
        }

        private static float GetLuminosity(Vector3 aColour)
        {
            // HSP colour model http://alienryderflex.com/hsp.html
            return
                (float)Math.Sqrt(
                    aColour.X * aColour.X * 0.299f +
                    aColour.Y * aColour.Y * 0.587f +
                    aColour.Z * aColour.Z * 0.114f);
        }

        private static string RenderColourSettings(BeatmapSet aBeatmapSet)
        {
            int maxComboAmount = aBeatmapSet.beatmaps.Max(aBeatmap => aBeatmap.colourSettings.combos.Count());

            StringBuilder content = new StringBuilder();

            // Combo Colours
            for (int i = 0; i < maxComboAmount; ++i)
            {
                content.Append(
                    RenderBeatmapContent(aBeatmapSet, $"Combo {i + 1}", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
                            aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                            return "N/A";

                        Vector3? comboColour = null;

                        // Need to account for that the colour at index 0 is actually the last displayed index in-game.
                        if (aBeatmap.colourSettings.combos.Count() > i + 1)
                            comboColour = aBeatmap.colourSettings.combos[i + 1];
                        else if (aBeatmap.colourSettings.combos.Count() == i + 1)
                            comboColour = aBeatmap.colourSettings.combos[0];

                        if (comboColour == null)
                            return
                                DivAttr("overview-colour",
                                    DataAttr("colour", "")
                                );
                        else
                            return
                                DivAttr("overview-colour",
                                    DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) +
                                    Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}, less than 43 or greater than 250 in kiai is bad.")
                                );
                    }, false));
            }

            // Border + Track
            content.Append(
                RenderBeatmapContent(aBeatmapSet, $"Slider Border", aBeatmap =>
                {
                    if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
                        aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                        return "N/A";

                    Vector3? comboColour = aBeatmap.colourSettings.sliderBorder;

                    if (comboColour == null)
                        return
                            DivAttr("overview-colour",
                                DataAttr("colour", "")
                            );
                    else
                        return
                            DivAttr("overview-colour",
                                DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) +
                                    Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}, less than 43 is bad.")
                            );
                }, false) +
                RenderBeatmapContent(aBeatmapSet, $"Slider Track", aBeatmap =>
                {
                    if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
                        aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                        return "N/A";

                    Vector3? comboColour = aBeatmap.colourSettings.sliderTrackOverride;

                    if (comboColour == null)
                        return
                            DivAttr("overview-colour",
                                DataAttr("colour", "")
                            );
                    else
                        return
                            DivAttr("overview-colour",
                                DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) +
                                    Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}")
                            );
                }, false));

            return
                RenderContainer("Colour Settings",
                    content.ToString()
                );
        }

        // Returns a single field if all values are equal, otherwise multiple.
        private static string RenderBeatmapContent(BeatmapSet aBeatmapSet, string aTitle, Func<Beatmap, string> aFunc, bool aEncode = true)
        {
            Dictionary<Beatmap, string> beatmapContent = new Dictionary<Beatmap, string>();
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                beatmapContent[beatmap] = aFunc(beatmap);

            if(beatmapContent.Any(aPair => aPair.Value != beatmapContent.First().Value))
                return
                    RenderField(aTitle,
                        String.Concat(
                        aBeatmapSet.beatmaps.Select(aBeatmap =>
                        {
                            return
                                RenderField(aBeatmap.metadataSettings.version,
                                    aEncode ?
                                        FormatTimestamps(Encode(beatmapContent[aBeatmap])) :
                                        beatmapContent[aBeatmap]
                                );
                        }))
                    );
            else
                return
                    RenderField(aTitle,
                        aEncode ?
                            FormatTimestamps(Encode(beatmapContent.First().Value)) :
                            beatmapContent.First().Value
                    );
        }

        private static string RenderContainer(string aTitle, params string[] aContents)
        {
            return
                Div("overview-container",
                    Div("overview-container-title",
                        Encode(aTitle)
                    ),
                    Div("overview-fields",
                        aContents
                    )
                );
        }

        private static string RenderField(string aTitle, params string[] aContents)
        {
            return
                Div("overview-field",
                    Div("overview-field-title",
                        Encode(aTitle)
                    ),
                    Div("overview-field-content",
                        aContents
                    )
                );
        }
    }
}
