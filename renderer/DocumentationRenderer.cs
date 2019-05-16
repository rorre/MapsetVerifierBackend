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
    public class DocumentationRenderer : Renderer
    {
        public static string Render()
        {
            return String.Concat(
                    RenderIcons(),
                    RenderChecks()
                );
        }

        private static string RenderChecks()
        {
            List<Check> checks = CheckerRegistry.GetChecks();

            IEnumerable<Check> generalChecks = checks.Where(aCheck => aCheck is GeneralCheck);
            
            IEnumerable<Check> allModeChecks = checks.Where(aCheck =>
            {
                BeatmapCheckMetadata metadata = aCheck.GetMetadata() as BeatmapCheckMetadata;

                if (metadata == null)
                    return false;

                return
                    metadata.Modes.Contains(Beatmap.Mode.Standard) &&
                    metadata.Modes.Contains(Beatmap.Mode.Taiko) &&
                    metadata.Modes.Contains(Beatmap.Mode.Catch) &&
                    metadata.Modes.Contains(Beatmap.Mode.Mania);
            });

            bool hasMode(Check aCheck, Beatmap.Mode aMode) => (aCheck.GetMetadata() as BeatmapCheckMetadata)?.Modes.Contains(aMode) ?? false;

            IEnumerable<Check> standardChecks   = checks.Where(aCheck => hasMode(aCheck, Beatmap.Mode.Standard)).Except(allModeChecks).Except(generalChecks);
            IEnumerable<Check> catchChecks      = checks.Where(aCheck => hasMode(aCheck, Beatmap.Mode.Taiko))   .Except(allModeChecks).Except(generalChecks);
            IEnumerable<Check> taikoChecks      = checks.Where(aCheck => hasMode(aCheck, Beatmap.Mode.Catch))   .Except(allModeChecks).Except(generalChecks);
            IEnumerable<Check> maniaChecks      = checks.Where(aCheck => hasMode(aCheck, Beatmap.Mode.Mania))   .Except(allModeChecks).Except(generalChecks);

            return
                RenderModeCategory("General",   generalChecks) +
                RenderModeCategory("All Modes", allModeChecks) +
                RenderModeCategory("Standard",  standardChecks) +
                RenderModeCategory("Taiko",     catchChecks) +
                RenderModeCategory("Catch",     taikoChecks) +
                RenderModeCategory("Mania",     maniaChecks);
        }

        private static string RenderModeCategory(string title, IEnumerable<Check> aChecks)
        {
            if (!aChecks.Any())
                return "";

            return
                Div("doc-mode-title", title) +
                Div("doc-mode-content",
                    Div("doc-box-container",
                        aChecks.OrderByDescending(aCheck => aCheck.GetMetadata().Category).Select(RenderCheckBox).ToArray()
                    )
                );
        }

        private static string RenderCheckBox(Check aCheck)
        {
            return
                RenderDocBox(
                    "check",
                    Encode(aCheck.GetMetadata().Message),

                    String.Concat(
                    aCheck.GetTemplates().Select(aPair => aPair.Value).Select(aTemplate =>
                    {
                        return
                            Div("card-detail-icon " + GetIcon(aTemplate.Level) + "-icon") +
                            Div("doc-box-issue",
                                aTemplate.ToString()
                            );
                    })),

                    Div("doc-box-category",
                        Encode(aCheck.GetMetadata().GetMode() + " > " + aCheck.GetMetadata().Category)
                    ) +
                    Div("doc-box-author",
                        Encode(aCheck.GetMetadata().Author)
                    )
                );
        }

        private static string RenderIcons()
        {
            return
                Div("doc-mode-title",
                    "Icons"
                ) +
                Div("doc-box-container",
                    RenderDocBox("check",          "Check",    "No issues were found.",                                "Checks", true),
                    RenderDocBox("error",          "Error",    "An error occurred preventing a complete check.",       "Checks", true),
                    RenderDocBox("minor",          "Minor",    "One or more negligible issues may have been found.",   "Checks", true),
                    RenderDocBox("exclamation",    "Warning",  "One or more issues may have been found.",              "Checks", true),
                    RenderDocBox("cross",          "Problem",  "One or more issues were found.",                       "Checks", true),

                    RenderDocBox("gear-gray",  "None",     "No changes were made.",            "Snapshots", true),
                    RenderDocBox("minus",      "Removal",  "One or more lines were removed.",  "Snapshots", true),
                    RenderDocBox("plus",       "Addition", "One or more lines were added.",    "Snapshots", true),
                    RenderDocBox("gear-blue",  "Change",   "One or more lines were changed.",  "Snapshots", true)
                );
        }

        public static string RenderDocBox(string anIcon, string aTitle, string aDesc, string aCategory, bool aIconDoc = false)
        {
            return
                Div((aIconDoc ? "doc-icon-box" : "doc-box"),
                    Div("doc-box-inner",
                        Div("doc-box-content",
                            Div("doc-box-title",
                                Div("doc-box-icon " + anIcon + "-icon"),
                                aTitle
                            ),
                            Div("doc-box-desc",
                                aDesc
                            )
                        ),
                        Div("doc-box-footer",
                            aCategory
                        )
                    )
                );
        }
    }
}
