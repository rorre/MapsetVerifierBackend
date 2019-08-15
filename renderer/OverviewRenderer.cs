using MapsetParser.objects;
using MapsetParser.settings;
using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                    RenderMetadata(aBeatmapSet),
                    RenderGeneralSettings(aBeatmapSet),
                    RenderDifficultySettings(aBeatmapSet)
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
                        aBeatmap.generalSettings.stackLeniency.ToString()),
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
                        Timestamp.Get(aBeatmap.generalSettings.previewTime)),
                    RenderBeatmapContent(aBeatmapSet, "Skin Preference", aBeatmap =>
                        aBeatmap.generalSettings.skinPreference?.ToString()),
                    RenderBeatmapContent(aBeatmapSet, "Special N1 Style", aBeatmap =>
                    {
                        if (aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                            return aBeatmap.generalSettings.specialN1Style.ToString();
                        else
                            return "N/A";
                    })
                );
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

        private static string RenderDifficultySettings(BeatmapSet aBeatmapSet)
        {
            return
                RenderContainer("Difficulty Settings",
                    RenderBeatmapContent(aBeatmapSet, "HP Drain", aBeatmap =>
                        aBeatmap.difficultySettings.hpDrain.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Circle Size", aBeatmap =>
                        aBeatmap.difficultySettings.circleSize.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Overall Difficulty", aBeatmap =>
                        aBeatmap.difficultySettings.overallDifficulty.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Approach Rate", aBeatmap =>
                        aBeatmap.difficultySettings.approachRate.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "Slider Tick Rate", aBeatmap =>
                        aBeatmap.difficultySettings.sliderTickRate.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(aBeatmapSet, "SV Multiplier", aBeatmap =>
                        aBeatmap.difficultySettings.sliderMultiplier.ToString(CultureInfo.InvariantCulture))
                );
        }

        // Returns a single field if all values are equal, otherwise multiple.
        private static string RenderBeatmapContent(BeatmapSet aBeatmapSet, string aTitle, Func<Beatmap, string> aFunc)
        {
            string refContent = aFunc(aBeatmapSet.beatmaps.FirstOrDefault());
            if(aBeatmapSet.beatmaps.Any(aBeatmap => aFunc(aBeatmap) != refContent))
                return
                    RenderField(aTitle,
                        String.Concat(
                        aBeatmapSet.beatmaps.Select(aBeatmap =>
                        {
                            return
                                RenderField(aBeatmap.metadataSettings.version,
                                    FormatTimestamps(Encode(aFunc(aBeatmap)))
                                );
                        }))
                    );
            else
                return
                    RenderField(aTitle,
                        FormatTimestamps(Encode(refContent))
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
