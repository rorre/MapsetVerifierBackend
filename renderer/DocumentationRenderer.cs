using MapsetParser.objects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierBackend.renderer
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
                    Div("doc-mode-inner",
                        aChecks.OrderByDescending(aCheck => aCheck.GetMetadata().Category).Select(RenderCheckBox).ToArray()
                    )
                );
        }

        /// <summary> Returns the html of a check as shown in the documentation tab. </summary>
        public static string RenderCheckBox(Check aCheck)
        {
            return
                RenderDocBox(
                    "check",
                    Encode(aCheck.GetMetadata().Message),
                    Encode(aCheck.GetMetadata().GetMode() + " > " + aCheck.GetMetadata().Category),

                    String.Concat(
                        aCheck.GetTemplates().Select(aPair => aPair.Value).Select(aTemplate =>
                            Div("card-detail-icon " + GetIcon(aTemplate.Level) + "-icon"))),
                    
                    Encode(aCheck.GetMetadata().Author)
                );
        }

        private static string RenderIcons()
        {
            return
                Div("doc-mode-title",
                    "Icons"
                ) +
                Div("doc-mode-inner doc-mode-icons",
                    RenderIconsDocBox("check",          "Check",    "Checks", "No issues were found."),
                    RenderIconsDocBox("error",          "Error",    "Checks", "An error occurred preventing a complete check."),
                    RenderIconsDocBox("minor",          "Minor",    "Checks", "One or more negligible issues may have been found."),
                    RenderIconsDocBox("exclamation",    "Warning",  "Checks", "One or more issues may have been found."),
                    RenderIconsDocBox("cross",          "Problem",  "Checks", "One or more issues were found."),

                    RenderIconsDocBox("gear-gray",  "None",     "Snapshots", "No changes were made."),
                    RenderIconsDocBox("minus",      "Removal",  "Snapshots", "One or more lines were removed."),
                    RenderIconsDocBox("plus",       "Addition", "Snapshots", "One or more lines were added."),
                    RenderIconsDocBox("gear-blue",  "Change",   "Snapshots", "One or more lines were changed.")
                );
        }

        public static string RenderDocBox(string anIcon, string aTitle, string aSubtitle, string anIssueOverview = null, string anAuthor = null)
        {
            return
                Div("doc-box-container",
                    Div("doc-box-left",
                        Div("doc-box-icon-container",
                            Div("doc-box-icon " + anIcon + "-icon")
                        ),
                        Div("doc-box-content",
                            Div("doc-box-title",
                                aTitle
                            ),
                            Div("doc-box-subtitle",
                                aSubtitle
                            )
                        )
                    ),
                    Div("doc-box-right",
                        Div("doc-box-right-upper",
                            anIssueOverview
                        ),
                        Div("doc-box-right-lower",
                            anAuthor
                        )
                    )
                );
        }

        public static string RenderIconsDocBox(string anIcon, string aTitle, string aSubtitle, string aDescription)
        {
            return
                Div("doc-box-container",
                    Div("doc-box-left",
                        Div("doc-box-icon-container",
                            Div("doc-box-icon " + anIcon + "-icon")
                        ),
                        Div("doc-box-content",
                            Div("doc-box-title",
                                aTitle
                            ),
                            Div("doc-box-subtitle",
                                aSubtitle
                            )
                        )
                    ),
                    Div("doc-box-right",
                        aDescription
                    )
                );
        }
    }
}
