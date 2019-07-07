using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetVerifierBackend.renderer
{
    public class BeatmapInfoRenderer : Renderer
    {
        protected static string RenderBeatmapInfo(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];

            return
                Div("beatmap-container",
                    Div("beatmap-title",
                        Encode(refBeatmap.metadataSettings.artist) + " - " + Encode(refBeatmap.metadataSettings.title)
                    ),
                    Div("beatmap-author-field",
                        "Beatmapset by " + UserLink(Encode(refBeatmap.metadataSettings.creator))
                    ),
                    Div("beatmap-options",
                        DivAttr("beatmap-options-folder beatmap-option beatmap-option-filter folder-icon",
                            DataAttr("folder", Encode(aBeatmapSet.songPath)) +
                            Tooltip("Open song folder")
                        ),
                        (refBeatmap.metadataSettings.beatmapSetId != null ?
                        DivAttr("beatmap-options-web beatmap-option beatmap-option-filter web-icon",
                            DataAttr("setid", Encode(refBeatmap.metadataSettings.beatmapSetId.ToString())) +
                            Tooltip("Open beatmap page")
                        ) :
                        DivAttr("beatmap-option beatmap-option-filter no-click web-unavailable-icon",
                            Tooltip("No beatmap page available")
                        ))
                    )
                );
        }
    }
}
