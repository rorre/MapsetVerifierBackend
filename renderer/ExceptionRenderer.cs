using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierBackend.renderer
{
    public class ExceptionRenderer : Renderer
    {
        public static string Render(Exception anException)
        {
            // Only the innermost exception is important, MapsetVerifier runs a lot of things in
            // parallel so many exceptions will be aggregates and not provide any useful information.
            Exception printedException = anException;
            while (printedException.InnerException != null)
                printedException = printedException.InnerException;

            string printedCheckBox =
                printedException.Data["Check"] != null ?
                    DocumentationRenderer.RenderCheckBox(printedException.Data["Check"] as Check) :
                    null;

            return
                Div("exception",
                    Div("exception-message",
                        Encode(printedException.Message)
                    ),
                    Div("exception-check",
                        printedCheckBox
                    ),
                    Div("exception-trace",
                        Encode(printedException.StackTrace).Replace("\r\n", "<br>")
                    )
                );
        }
    }
}
