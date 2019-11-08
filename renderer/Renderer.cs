using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MapsetVerifierBackend.renderer
{
    public class Renderer
    {
        /// <summary> Surrounds the content with a div tag using the given class(es), as well as any other attributes in the tag, like id or data. </summary>
        protected static string DivAttr(string aClass, string anAttr, params object[] aContents)
        {
            return String.Concat("<div", (aClass.Length > 0 ? " class=\"" + aClass + "\"" : ""), anAttr, ">", String.Join("", aContents), "</div>");
        }

        /// <summary> Surrounds the content with a div tag using the given class(es). </summary>
        protected static string Div(string aClass, params object[] aContents)
        {
            return DivAttr(aClass, "", aContents);
        }

        /// <summary> Surrounds the content (or reference if none exists) with an a tag using the given reference.
        /// Does not need to target _blank, as this is done client-side. </summary>
        protected static string Link(string aRef, object aContent = null)
        {
            return "<a href=\"" + aRef + "\">" + (aContent ?? aRef) + "</a>";
        }

        /// <summary> Creates a link to the given username with the same username as content. </summary>
        protected static string UserLink(object aUsername)
        {
            return Link("https://osu.ppy.sh/users/" + aUsername, aUsername);
        }

        /// <summary> Creates a link to the given username with the same username as content. </summary>
        protected static string TimestampLink(object aTimestamp)
        {
            return Link("osu://edit/" + aTimestamp + "\" class=\"card-instance-timestamp", aTimestamp);
        }

        /// <summary> Surrounds the content in a data attribute of a given type. </summary>
        protected static string DataAttr(string aType, object aContent)
        {
            return " data-" + aType + "=\"" + Encode(aContent.ToString()) + "\"";
        }

        /// <summary> Surrounds the content in a tooltip data attribute. </summary>
        protected static string Tooltip(object aContent)
        {
            return DataAttr("tooltip", aContent);
        }

        /// <summary> Combines all difficulties this issue applies to into a condition attribute
        /// (data-condition="difficulty=1,2,3"), which is then returned. </summary>
        protected static string DifficultiesDataAttr(Issue anIssue)
        {
            BeatmapCheckMetadata metadata = anIssue.CheckOrigin.GetMetadata() as BeatmapCheckMetadata;

            List<Beatmap.Difficulty> difficulties = new List<Beatmap.Difficulty>();
            foreach (Beatmap.Difficulty difficulty in Enum.GetValues(typeof(Beatmap.Difficulty)))
                if ((metadata?.Difficulties.Contains(difficulty) ?? true) && (
                        !anIssue.InterpretationPairs.Any(aPair => aPair.Key == "difficulty") ||
                        anIssue.InterpretationPairs.Any(aPair => aPair.Key == "difficulty" && aPair.Value == (int)difficulty)))
                    difficulties.Add(difficulty);

            return DifficultiesDataAttr(difficulties.ToArray());
        }

        /// <summary> Combines all difficulties into a condition attribute (data-condition="difficulty=1,2,3"), which is then returned. </summary>
        protected static string DifficultiesDataAttr(Beatmap.Difficulty[] aDifficulties)
        {
            // With the condition being any difficulty, we might as well not have a condition at all.
            if (aDifficulties.Count() == Enum.GetValues(typeof(Beatmap.Difficulty)).Length)
                return "";

            return
                DataAttr("condition",
                    "difficulty=" + String.Join(",", aDifficulties.Select(aDifficulty => (int)aDifficulty))
                );
        }

        /// <summary> Combines all interpretation pairs into a condition attribute (data-condition="key=1,2,3"), which is then returned. </summary>
        protected static string InterpretDataAttr(List<KeyValuePair<string, int>> anInterpretPairs)
        {
            return
                String.Concat(
                anInterpretPairs.Select(aPair => aPair.Key).Distinct().Select(aKey =>
                {
                    return
                        DataAttr("condition",
                            aKey + "=" + String.Join(",",
                                anInterpretPairs.Where(aPair => aPair.Key == aKey).Select(aPair => aPair.Value))
                        );
                }));
        }

        /// <summary> Returns the same string but HTML encoded, meaning greater and less than signs no longer form actual tags. </summary>
        public static string Encode(string aString)
        {
            if (aString == null)
                return null;

            return WebUtility.HtmlEncode(aString);
        }

        /// <summary> Returns the icon of the greatest issue level of all issues given. </summary>
        protected static string GetIcon(IEnumerable<Issue> anIssues)
        {
            return
                anIssues.Any() ?
                    GetIcon(anIssues.Max(anIssue => anIssue.level)) :
                    GetIcon(Issue.Level.Check);
        }

        /// <summary> Returns the icon of the given issue level. </summary>
        protected static string GetIcon(Issue.Level aLevel)
        {
            return
                aLevel == Issue.Level.Problem ? "cross" :
                aLevel == Issue.Level.Warning ? "exclamation" :
                aLevel == Issue.Level.Minor   ? "minor" :
                aLevel == Issue.Level.Error   ? "error" :
                aLevel == Issue.Level.Check   ? "check" :
                                                "info";
        }

        /// <summary> Wraps all timestamps outside of html tags into proper timestamp hyperlinks. </summary>
        protected static string FormatTimestamps(string aContent)
        {
            return
                Regex.Replace(
                    aContent,
                    @"(\d\d:\d\d:\d\d\d( \([\d|,]+\))?)(?![^<]*>|[^<>]*<\/)",
                    (evaluator) => TimestampLink(evaluator.Value));
        }

        /// <summary> Wraps all links outside of html tags into proper hyperlinks. </summary>
        protected static string FormatLinks(string aContent)
        {
            return
                Regex.Replace(
                    aContent,
                    @"(http(s)?:\/\/([a-zA-Z0-9]{1,6}\.)?[a-zA-Z0-9]{1,256}\.[a-zA-Z0-9]{1,6}(\/[a-zA-Z0-9\/\?=&@\+]+)?)(?![^<]*>|[^<>]*<\/)",
                    (evaluator) => Link(evaluator.Value));
        }

        /// <summary> Replaces all pseudo note tags into proper html tags. </summary>
        protected static string FormatNotes(string aContent)
        {
            return
                aContent
                    .Replace("<note>", "<div class=\"note\"><div class=\"note-text\">")
                    .Replace("</note>", "</div></div>");
        }

        protected static string FormatCode(string aContent)
        {
            return
                Regex.Replace(
                    aContent,
                    @"`.*?`",
                    (evaluator) => Div("code", evaluator.Value.Replace("`", "")));
        }

        /// <summary> Replaces all pseudo image tags into proper html tags and moves them if needed. </summary>
        protected static string FormatImages(string aContent)
        {
            string result = aContent;
            result = FormatCenteredImages(result);
            result = FormatRightImages(result);

            return result;
        }

        /// <summary> Replaces all center-aligned pseudo image tags into proper html tags. </summary>
        private static string FormatCenteredImages(string aContent)
        {
            Regex regex = new Regex(
                @"<image>[\ (\r\n|\r|\n)]+([A-Za-z0-9\/:\.]+(\.jpg|\.png))[\ (\r\n|\r|\n)]+(.*?)[\ (\r\n|\r|\n)]+<\/image>",
                RegexOptions.Singleline);

            string result = aContent;
            foreach (Match match in regex.Matches(result))
            {
                string src = match.Groups[1].Value;
                string text = match.Groups[3].Value;

                result = regex.Replace(result,
                    "<div class=\"image image-center\" data-text=\"" + Encode(text) + "\"><img src=\"" + src + "\"></img></div>", 1);
            }

            return result;
        }

        /// <summary> Replaces all right-aligned pseudo image tags into proper html tags and prepends them to the content. </summary>
        protected static string FormatRightImages(string aContent)
        {
            Regex regex = new Regex(
                @"<image-right>[\ (\r\n|\r|\n)]+([A-Za-z0-9\/:\.]+(\.jpg|\.png))[\ (\r\n|\r|\n)]+(.*?)[\ (\r\n|\r|\n)]+<\/image>",
                RegexOptions.Singleline);

            string result = aContent;
            StringBuilder extractedStr = new StringBuilder();
            foreach (Match match in regex.Matches(result))
            {
                string src = match.Groups[1].Value;
                string text = match.Groups[3].Value;

                extractedStr.Append("<div class=\"image image-right\" data-text=\"" + Encode(text) + "\"><img src=\"" + src + "\"></img></div>");
                result = regex.Replace(result, "", 1);
            }

            return extractedStr.ToString() + result;
        }

        /// <summary> Applies all formatting (code, links, timestamps, notes, images) to the given string. </summary>
        protected static string Format(string aContent)
        {
            string result = aContent;
            result = FormatCode(result);
            result = FormatLinks(result);
            result = FormatTimestamps(result);
            result = FormatNotes(result);
            result = FormatImages(result);

            return result;
        }
    }
}
