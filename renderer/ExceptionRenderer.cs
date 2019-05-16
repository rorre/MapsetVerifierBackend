using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetVerifierApp.renderer
{
    public class ExceptionRenderer : Renderer
    {
        public static string Render(Exception anException)
        {
            // If the exception wasn't thrown by the application itself, we only care about whatever component caused it.
            Exception printedException = anException.InnerException ?? anException;

            return
                Div("exception",
                    Div("exception-message",
                        Encode(printedException.Message)
                    ),
                    Div("exception-trace",
                        Encode(printedException.StackTrace).Replace("\r\n", "<br>")
                    )
                );
        }
    }
}
