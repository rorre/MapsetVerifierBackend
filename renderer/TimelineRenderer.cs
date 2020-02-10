using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierBackend.renderer
{
    class TimelineRenderer : OverviewRenderer
    {
        private const int ZOOM_FACTOR = 8;
        private const int MILLISECOND_MARGIN = 2000;

        public new static string Render(BeatmapSet aBeatmapSet)
        {
            return RenderTimelineComparison(aBeatmapSet);
        }

        private static string RenderTimelineComparison(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("Timeline Comparison (Prototype)",
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
                        RenderTop(aBeatmapSet)
                    ),
                    Div("overview-timeline-content",
                        RenderContent(aBeatmapSet)
                    ),
                    Div("overview-timeline-footer",
                        RenderFooter(aBeatmapSet)
                    )
                );
        }

        private static string RenderTop(BeatmapSet aBeatmapSet)
        {
            return
                Div("overview-timeline-difficulties",
                    String.Concat(
                    aBeatmapSet.beatmaps.Select(aBeatmap =>
                    {
                        return
                            Div("overview-timeline-difficulty noselect",
                                aBeatmap.metadataSettings.version
                            );
                    }))
                );
        }

        private static string RenderContent(BeatmapSet aBeatmapSet)
        {
            StringBuilder contentHTML = new StringBuilder();

            double startTime = GetStartTime(aBeatmapSet);

            double endTime = GetEndTime(aBeatmapSet);

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                // Mania can have multiple notes at the same time so we'll need to do that differently.
                if (beatmap.generalSettings.mode == Beatmap.Mode.Mania)
                    continue;

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
                            RenderTicks(beatmap, startTime, endTime)
                        ),
                        RenderObjects(beatmap, startTime)
                    )
                );
            }

            return contentHTML.ToString();
        }

        private static double GetStartTime(BeatmapSet aBeatmapSet)
        {
            double startTimeObjects =
                    aBeatmapSet.beatmaps.Min(aBeatmap =>
                        aBeatmap.hitObjects.FirstOrDefault()?.time ?? 0);
            double startTimeLines =
                aBeatmapSet.beatmaps.Min(aBeatmap =>
                    aBeatmap.timingLines.FirstOrDefault()?.offset ?? 0);

            return
                (startTimeObjects < startTimeLines ?
                    startTimeObjects :
                    startTimeLines)
                - MILLISECOND_MARGIN;
        }

        private static double GetEndTime(BeatmapSet aBeatmapSet)
        {
            double endTimeObjects =
                    aBeatmapSet.beatmaps.Max(aBeatmap =>
                        aBeatmap.hitObjects.LastOrDefault()?.GetEndTime() ?? 0);
            double endTimeLines =
                aBeatmapSet.beatmaps.Max(aBeatmap =>
                    aBeatmap.timingLines.LastOrDefault()?.offset ?? 0);

            return
                (endTimeObjects < endTimeLines ?
                    endTimeObjects :
                    endTimeLines)
                + MILLISECOND_MARGIN;
        }

        private static string RenderFooter(BeatmapSet aBeatmapSet)
        {
            return
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
                );
        }

        private static string RenderTicks(Beatmap aBeatmap, double aStartTime, double anEndTime)
        {
            double sampleTime = aStartTime;
            double prevSampleTime = aStartTime;

            return
                String.Concat(
                aBeatmap.timingLines.OfType<UninheritedLine>().Select(aLine =>
                {
                    UninheritedLine nextLine = aBeatmap.GetNextTimingLine<UninheritedLine>(aLine.offset);
                    double nextSwap = nextLine?.offset ?? anEndTime;

                    StringBuilder tickDivs = new StringBuilder();

                    // To get precision down to both 1/16th and 1/12th of a beat we need to sample...
                    // 16 = 2^4, 12 = 2^2*3, 2^4*3 = 48 times per beat.
                    int samplesPerBeat = 48;
                    for (int i = 0; i < (nextSwap - aLine.offset) / aLine.msPerBeat * samplesPerBeat; ++i)
                    {
                        // Add the practical unsnap to avoid things getting unsnapped the further into the map you go.
                        sampleTime =
                            aLine.offset + i * aLine.msPerBeat / samplesPerBeat +
                            aBeatmap.GetPracticalUnsnap(aLine.offset + i * aLine.msPerBeat / samplesPerBeat);

                        bool hasEdge =
                            aBeatmap.GetHitObject(sampleTime)?.GetEdgeTimes().Any(anEdgeTime =>
                                Math.Abs(anEdgeTime - sampleTime) < 2) ?? false;

                        if (i % (samplesPerBeat / 4) == 0 ||
                            hasEdge && (
                                i % (samplesPerBeat / 12) == 0 ||
                                i % (samplesPerBeat / 16) == 0))
                        {
                            tickDivs.Append(
                                DivAttr("overview-timeline-tick",
                                    " style=\"margin-left:" + ((sampleTime - prevSampleTime) / ZOOM_FACTOR) + "px\"",
                                    Div("overview-timeline-ticks-base " + (hasEdge ? " hasobject " : "") + (
                                        i % (samplesPerBeat * aLine.meter) == 0 ? "overview-timeline-ticks-largewhite" :
                                        i % (samplesPerBeat * 1) == 0 ?           "overview-timeline-ticks-white" :
                                        i % (samplesPerBeat / 2) == 0 ?           "overview-timeline-ticks-red" :
                                        i % (samplesPerBeat / 3) == 0 ?           "overview-timeline-ticks-magenta" :
                                        i % (samplesPerBeat / 4) == 0 ?           "overview-timeline-ticks-blue" :
                                        i % (samplesPerBeat / 6) == 0 ?           "overview-timeline-ticks-purple" :
                                        i % (samplesPerBeat / 8) == 0 ?           "overview-timeline-ticks-yellow" :
                                        i % (samplesPerBeat / 12) == 0 ?          "overview-timeline-ticks-gray" :
                                        i % (samplesPerBeat / 16) == 0 ?          "overview-timeline-ticks-gray" :
                                                                                  "overview-timeline-ticks-unsnapped") // these last ones shouldn't appear
                                    )
                                ));

                            prevSampleTime = sampleTime;
                        }
                    }

                    return tickDivs.ToString();
                }));
        }

        private static string RenderObjects(Beatmap aBeatmap, double aStartTime)
        {
            double objectTime = aStartTime;

            return
                String.Concat(
                    aBeatmap.hitObjects.Select(aHitObject =>
                    {
                        double prevTime = objectTime;
                        objectTime = aHitObject.time;

                        if (aHitObject is Circle circle)
                            return
                                DivAttr("overview-timeline-object",
                                    " style=\"margin-left:" + ((circle.time - prevTime) / ZOOM_FACTOR) + "px;\"",
                                    DivAttr("overview-timeline-circle",
                                        DataAttr("timestamp", Timestamp.Get(circle.time)) +
                                        " style=\"" +
                                            RenderHitObjectBackgroundStyle(circle, aBeatmap) +
                                            RenderHitObjectSizeStyle(circle, aBeatmap) +
                                        "\"")
                                );
                        else if (aHitObject is Slider slider)
                        {
                            // Big drumrolls need additional length due to the increased radius.
                            // In taiko regular notes are smaller so it needs a reduced radius otherwise.
                            int addedWidth =
                                aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ?
                                    slider.HasHitSound(HitObject.HitSound.Finish) ?
                                        32 :
                                        22 :
                                    23;
                            return
                                DivAttr("overview-timeline-object",
                                    DataAttr("timestamp", Timestamp.Get(slider.time)) +
                                    " style=\"margin-left:" + ((slider.time - prevTime) / ZOOM_FACTOR) + "px;\"",
                                    String.Concat(
                                    slider.GetEdgeTimes().Select((anEdgeTime, anIndex) =>
                                    {
                                        return
                                            DivAttr("overview-timeline-object edge",
                                                "style=\"margin-left:" + ((anEdgeTime - slider.time) / ZOOM_FACTOR) + "px;\"",
                                                DivAttr("overview-timeline-edge" +
                                                    (anIndex > 0 && anIndex < slider.edgeAmount ?
                                                        " overview-timeline-edge-reverse" : ""),
                                                    " style=\"" +
                                                        RenderHitObjectSizeStyle(slider, aBeatmap) +
                                                    "\"")
                                            );
                                    })),
                                    DivAttr("overview-timeline-path",
                                        " style=\"" +
                                            "width:" + ((slider.endTime - slider.time) / ZOOM_FACTOR) + "px;" +
                                            "padding-right:" + addedWidth + "px;" +
                                            RenderHitObjectBackgroundStyle(slider, aBeatmap) +
                                            RenderHitObjectSizeStyle(slider, aBeatmap, true) + "\"")
                                );
                        }
                        else if (aHitObject is Spinner spinner)
                        {
                            int addedWidth = aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ? 22 : 23;
                            return
                                DivAttr("overview-timeline-object",
                                    DataAttr("timestamp", Timestamp.Get(spinner.time)) +
                                    " style=\"margin-left:" + ((spinner.time - prevTime) / ZOOM_FACTOR) + "px;\"",
                                    Div("overview-timeline-object edge",
                                        Div("overview-timeline-edge")
                                    ),
                                    DivAttr("overview-timeline-object edge",
                                        " style=\"margin-left:" + ((spinner.endTime - spinner.time) / ZOOM_FACTOR) + "px;\"",
                                        Div("overview-timeline-edge")
                                    ),
                                    DivAttr("overview-timeline-path",
                                        " style=\"" +
                                            "width:" + ((spinner.endTime - spinner.time) / ZOOM_FACTOR) + "px;" +
                                            "padding-right:" + addedWidth + "px;\"")
                                );
                        }

                        return
                            DivAttr("overview-timeline-object",
                                " style=\"margin-left:" + ((aHitObject.time - prevTime) / ZOOM_FACTOR) + "px;\""
                            );
                    }));
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
            if (aBeatmap.colourSettings.combos.Count() > colourIndex)
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

        private static string RenderHitObjectSizeStyle(HitObject aHitObject, Beatmap aBeatmap, bool aIsSliderPath = false)
        {
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko)
            {
                if (aHitObject.HasHitSound(HitObject.HitSound.Finish))
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
    }
}
