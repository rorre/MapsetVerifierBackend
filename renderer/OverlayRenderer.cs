using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierBackend.renderer
{
    public class OverlayRenderer : Renderer
    {
        public static string Render(string aCheckMessage)
        {
            Check check =
                CheckerRegistry.GetChecks()
                    .FirstOrDefault(aCheck => aCheck.GetMetadata().Message == aCheckMessage);

            if (check?.GetMetadata() == null)
                return "No documentation found for check with message \"" + aCheckMessage + "\".";

            return String.Concat(
                RenderOverlayTop(check.GetMetadata()),
                RenderOverlayContent(check));
        }

        public static string RenderOverlayTop(CheckMetadata aMetadata)
        {
            return
                Div("\" id=\"overlay-top",
                    Div("check-icon\" id=\"overlay-top-icon"),
                    Div("\" id=\"overlay-top-title",
                        Encode(aMetadata.Message)
                    )
                ) +
                Div("\" id=\"overlay-top-subfields",
                    Div("\" id=\"overlay-top-category",
                        Encode(aMetadata.GetMode() + " > " + aMetadata.Category)
                    ),
                    Div("\" id=\"overlay-top-author",
                        "Created by " + Encode(aMetadata.Author)
                    )
                );
        }

        public static string RenderOverlayContent(Check aCheck)
        {
            return
                Div("paste-separator") +
                Div("\" style=\"clear:both;") +
                Div("\" id=\"overlay-content",
                    String.Concat(
                        RenderOverlayTemplates(aCheck),
                        RenderOverlayDocumentation(aCheck.GetMetadata())
                    )
                );
        }

        public static string RenderOverlayTemplates(Check aCheck)
        {
            return
                (aCheck.GetTemplates().Count > 0 ?
                    String.Concat(
                        aCheck.GetTemplates().Select(aPair => aPair.Value).Select(aTemplate =>
                        {
                            return
                                Div("check",
                                    Div("card-detail-icon " + GetIcon(aTemplate.Level) + "-icon"),
                                    Div("message",
                                        aTemplate.Format(
                                            aTemplate.GetDefaultArguments()
                                                .Select(anArg => "<span>" + anArg + "</span>").ToArray()),
                                        Div("cause",
                                            ApplyMarkdown(aTemplate.GetCause())
                                        )
                                    )
                                );
                        })
                    ) :
                    "No issue templates available.");
        }

        public static string RenderOverlayDocumentation(CheckMetadata aMetadata)
        {
            return
                String.Concat(
                aMetadata.Documentation.Select(aSection =>
                {
                    string value = aSection.Value;
                    return
                        ExtractFloatElements(ref value) +
                        Div("title",
                            Encode(aSection.Key)
                        ) +
                        ApplyMarkdown(value);
                }));
        }
    }
}
